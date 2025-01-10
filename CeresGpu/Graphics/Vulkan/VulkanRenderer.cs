using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using CeresGpu.Graphics.Shaders;
using Silk.NET.Core;
using Silk.NET.Vulkan;
using Silk.NET.Vulkan.Extensions.KHR;
using DescriptorType = Silk.NET.Vulkan.DescriptorType;

namespace CeresGpu.Graphics.Vulkan;

public unsafe delegate Result CreateWindowSurfaceDelegate(Instance instance, AllocationCallbacks* allocator, out SurfaceKHR surface);

public sealed class VulkanRenderer : IRenderer
{
    public uint UniqueFrameId { get; }

    public readonly Vk Vk = Vk.GetApi();
    public readonly KhrSurface VkKhrSurface;
    public readonly KhrSwapchain VkKhrSwapchain;
    
    public readonly Instance Instance;
    public readonly PhysicalDevice PhysicalDevice;
    public readonly PhysicalDeviceLimits PhysicalDeviceLimits;
    public readonly Device Device;
    public readonly Queue GraphicsQueue;
    public readonly Queue PresentationQueue;
    public readonly SwapchainKHR Swapchain;
    public readonly CommandPool CommandPool;

    private readonly VulkanSwapchainRenderTarget _swapchainRenderTarget;
        
    private readonly Dictionary<Type, VulkanPassBacking> _passBackings = [];

    public readonly VulkanMemoryHelper MemoryHelper;
    public readonly DescriptorPoolManager DescriptorPoolManager;

    private CommandBuffer _preFrameCommandBuffer;
    
    private readonly List<IDeferredDisposable>[] _deferedDisposableByWorkingFrame;

    /// <summary>
    /// Contains the passes that are to be submitted this frame.
    /// </summary>
    private readonly HashSet<IVulkanCommandEncoder> _passesToSubmit = new();
    
    // NOTE: These are just anchors, and are not to be submitted.
    private readonly VulkanCommandEncoderAnchor _encoderListStart = new();
    private readonly VulkanCommandEncoderAnchor _encoderListEnd = new();
    
    public readonly IVulkanTexture FallbackTexture;
    public readonly VulkanSampler FallbackSampler;

    public int FrameCount { get; private set; }
    public int WorkingFrame { get; private set; }
    
    public bool IsDisposed { get; private set; }

    /// <summary>
    /// Command buffer to encode commands into for the the current frame.
    /// This buffer will be submitted to the queue first.
    /// This is used for certain transfers that need to happen before all other commands.
    /// </summary>
    public CommandBuffer PreFrameCommandBuffer => _preFrameCommandBuffer;
    
    public unsafe VulkanRenderer(IVulkanWindowFactory windowFactory)
    {
        VkKhrSurface = new KhrSurface(Vk.Context);
        VkKhrSwapchain = new KhrSwapchain(Vk.Context);
        
        _deferedDisposableByWorkingFrame = Enumerable.Range(0, 3).Select(_ => new List<IDeferredDisposable>()).ToArray();
        
        //
        // Check which layers we have available.
        //
        uint numProperties = 0;
        Vk.EnumerateInstanceLayerProperties(ref numProperties, null)
            .AssertSuccess("Failed to get number of instance layer properties");
        Span<LayerProperties> instanceLayerProperties = new LayerProperties[numProperties];
        Vk.EnumerateInstanceLayerProperties(&numProperties, instanceLayerProperties);

        HashSet<string> layersToRequest = [];
        Console.WriteLine("-- Instance layer properties --");
        for (int i = 0; i < instanceLayerProperties.Length; i++) {
            ref readonly LayerProperties properties = ref instanceLayerProperties[i];
            fixed (LayerProperties* pProperties = &properties) {
                string name = Marshal.PtrToStringUTF8(new IntPtr(pProperties->LayerName)) ?? "(no name)";
                string description = Marshal.PtrToStringUTF8(new IntPtr(pProperties->Description)) ?? "(no description)";
                uint specVersion = pProperties->SpecVersion;
                uint implementationVersion = pProperties->ImplementationVersion;
                Console.WriteLine($"{name}: {specVersion}, {implementationVersion}, {description}");
                layersToRequest.Add(name);
            }
        }
        
        //
        // Check which extensions the instance has available.
        //
        
        uint numInstanceExtensions = 0;
        Vk.EnumerateInstanceExtensionProperties((byte*)null, ref numInstanceExtensions, null)
            .AssertSuccess("Failed to get number of instance extensions");
        Span<ExtensionProperties> instanceExtensions = new ExtensionProperties[numInstanceExtensions];
        Vk.EnumerateInstanceExtensionProperties((byte*)null, &numInstanceExtensions, instanceExtensions);
        
        HashSet<string> availableInstanceExtensions = [];
        Console.WriteLine("-- Instance extensions --");
        for (int i = 0; i < instanceExtensions.Length; ++i) {
            ref ExtensionProperties properties = ref instanceExtensions[i];
            fixed (ExtensionProperties* pProperties = &properties) {
                string? name = Marshal.PtrToStringUTF8(new IntPtr(pProperties->ExtensionName));
                if (name == null) {
                    continue;
                }
                Console.WriteLine($"{name} {pProperties->SpecVersion}");
                availableInstanceExtensions.Add(name);
            }
        }

        string[] desiredLayers = [
            "VK_LAYER_KHRONOS_validation"
        ];
        layersToRequest.IntersectWith(desiredLayers);

        HashSet<string> requiredInstanceExtensions = [
            "VK_KHR_surface"
        ];
        requiredInstanceExtensions.UnionWith(windowFactory.GetRequiredInstanceExtensions());

        if (!availableInstanceExtensions.IsSupersetOf(requiredInstanceExtensions)) {
            throw new InvalidOperationException("Missing required instance extensions.");
        }
        
        //
        // Create an instance of Vulkan!
        //
        fixed (byte* pEngineName = "CeresGpu"u8) {
            ApplicationInfo applicationInfo = new(
                StructureType.ApplicationInfo, 
                pNext: null,
                pApplicationName: null,
                applicationVersion: 0,
                pEngineName: pEngineName,
                engineVersion: 0,
                apiVersion: Vk.Version13.Value
            );

            byte*[] enabledLayerNames = new byte*[layersToRequest.Count];
            byte*[] enabledExtensionNames = new byte*[requiredInstanceExtensions.Count];
            try {
                int layerIndex = 0;
                foreach (string layerName in layersToRequest) {
                    enabledLayerNames[layerIndex++] = (byte*)Marshal.StringToHGlobalAnsi(layerName);
                }
                int extensionIndex = 0;
                foreach (string extensionName in requiredInstanceExtensions) {
                    enabledExtensionNames[extensionIndex++] = (byte*)Marshal.StringToHGlobalAnsi(extensionName);
                }
                
                fixed (byte** pEnabledLayerNames = enabledLayerNames) 
                fixed (byte** pEnabledExtensionNames = enabledExtensionNames) {
                    InstanceCreateInfo instanceCreateInfo = new(
                        StructureType.InstanceCreateInfo,
                        pNext: null,
                        flags: InstanceCreateFlags.None,
                        pApplicationInfo: &applicationInfo,
                        enabledLayerCount: (uint)enabledLayerNames.Length,
                        ppEnabledLayerNames: pEnabledLayerNames,
                        enabledExtensionCount: (uint)enabledExtensionNames.Length,
                        ppEnabledExtensionNames: pEnabledExtensionNames
                    );
                    if (Vk.CreateInstance(&instanceCreateInfo, null, out Instance) != Result.Success) {
                        throw new InvalidOperationException("Failed to create Vulkan Instance.");
                    }
                }
            } finally {
                foreach (byte* str in enabledLayerNames) {
                    Marshal.FreeHGlobal(new IntPtr(str));
                }
                foreach (byte* str in enabledExtensionNames) {
                    Marshal.FreeHGlobal(new IntPtr(str));
                }
            }
        }
        
        //
        // Create our surface
        //
        windowFactory.CreateSurface(Instance, [], out SurfaceKHR surface)
            .AssertSuccess("Failed to create surface.");
        
        //
        // Find an appropriate physical device to use.
        //
        uint numPhysicalDevices = 0;
        
        // First check how many devices we have, numPhysicalDevices will be updated with number of devices present.
        Vk.EnumeratePhysicalDevices(Instance, ref numPhysicalDevices, null)
            .AssertSuccess("Failed to get count of physical devices");
        
        // Now get the devices.
        PhysicalDevice[] physicalDevices = new PhysicalDevice[numPhysicalDevices];
        fixed (PhysicalDevice* pPhysicalDevices = physicalDevices) {
            Vk.EnumeratePhysicalDevices(Instance, ref numPhysicalDevices, pPhysicalDevices)
                .AssertSuccess("Failed to enumerate physical devices");    
        }
        
        // Choose the first suitable device for now.

        HashSet<string> requiredDeviceExtensions = [
            "VK_KHR_swapchain"
        ];
        
        QueueFlags neededQueues = QueueFlags.GraphicsBit;

        Dictionary<QueueFlags, uint> queueFamilyIndexForQueueType = [];
        uint presentQueueFamilyIndex = 0;
        
        PhysicalDevice chosenPhysicalDevice = default;
        foreach (PhysicalDevice physicalDevice in physicalDevices) {
            queueFamilyIndexForQueueType.Clear();
            
            // Check which extensions this device has.
            uint numExtensions = 0;
            Vk.EnumerateDeviceExtensionProperties(physicalDevice, (byte*)null, &numExtensions, null)
                .AssertSuccess("Failed to get number of device extensions");
            ExtensionProperties[] extensions = new ExtensionProperties[numExtensions];
            fixed (ExtensionProperties* pExtensions = extensions) {
                Vk.EnumerateDeviceExtensionProperties(physicalDevice, (byte*)null, &numExtensions, pExtensions)
                    .AssertSuccess("Failed to enumerate device extensions");
            }

            HashSet<string> supportedExtensions = [];
            
            Console.WriteLine("-- Device extensions ---");
            foreach (ExtensionProperties properties in extensions) {
                string? name = Marshal.PtrToStringUTF8((nint)properties.ExtensionName);
                if (name == null) {
                    continue;
                }
                Console.WriteLine($"{name} {properties.SpecVersion}");
                supportedExtensions.Add(name);
            }
            
            // Does the device have the extensions we need?
            if (!requiredDeviceExtensions.IsSubsetOf(supportedExtensions)) {
                continue;
            }
            
            // Check that the physical device has the required queues.
            
            // Check how many queue families we have first.
            uint numQueueFamilies = 0;
            Vk.GetPhysicalDeviceQueueFamilyProperties(physicalDevice, ref numQueueFamilies, null);
            
            // Now get the queue families
            QueueFamilyProperties[] families = new QueueFamilyProperties[numQueueFamilies];
            fixed (QueueFamilyProperties* pFamilies = families) {
                Vk.GetPhysicalDeviceQueueFamilyProperties(physicalDevice, ref numQueueFamilies, pFamilies);
            }
            
            // Get which types of queues are available
            QueueFlags availableQueueFlags = QueueFlags.None;
            bool hasPresentQueueFamily = false;
                
            for (int queueFamilyIndex = 0; queueFamilyIndex < numQueueFamilies; ++queueFamilyIndex) {
                ref readonly QueueFamilyProperties familyProperties = ref families[queueFamilyIndex];
                if (familyProperties.QueueCount <= 0) {
                    continue;
                }

                // Ignore this queue family if it doesn't support the surface.
                VkKhrSurface.GetPhysicalDeviceSurfaceSupport(physicalDevice, (uint)queueFamilyIndex, surface, out Bool32 supported)
                    .AssertSuccess("Failed to get physical device surface support");
                if (supported == Vk.True) {
                    hasPresentQueueFamily = true;
                    presentQueueFamilyIndex = (uint)queueFamilyIndex;
                }
                
                if (familyProperties.QueueFlags.HasFlag(QueueFlags.GraphicsBit)) {
                    queueFamilyIndexForQueueType[QueueFlags.GraphicsBit] = (uint)queueFamilyIndex;
                }
                
                availableQueueFlags |= familyProperties.QueueFlags;
            }
            
            //
            // Is this device suitible?
            //
            
            // Does it have all the queue types we need?
            if ((availableQueueFlags & neededQueues) == neededQueues && hasPresentQueueFamily) {
                chosenPhysicalDevice = physicalDevice;
                break;
            }
        }

        if (chosenPhysicalDevice.Handle == IntPtr.Zero) {
            throw new InvalidOperationException("Failed to find a suitable physical device.");
        }

        PhysicalDevice = chosenPhysicalDevice;

        PhysicalDeviceProperties physicalDeviceProperties = Vk.GetPhysicalDeviceProperties(chosenPhysicalDevice);
        PhysicalDeviceLimits = physicalDeviceProperties.Limits;
        
        //
        // Create the logical device.
        //

        float graphicsQueuePriority = 1f;
        float presentQueuePriority = 1f;
        
        Span<DeviceQueueCreateInfo> deviceQueueCreateInfos = [
            // Graphics queue
            new DeviceQueueCreateInfo(
                StructureType.DeviceQueueCreateInfo,
                pNext: null,
                flags: DeviceQueueCreateFlags.None,
                queueFamilyIndex: queueFamilyIndexForQueueType[QueueFlags.GraphicsBit],
                queueCount: 1,
                pQueuePriorities: &graphicsQueuePriority
            ),
            // Presentation queue
            new DeviceQueueCreateInfo(
                sType: StructureType.DeviceQueueCreateInfo,
                pNext: null,
                flags: DeviceQueueCreateFlags.None,
                queueFamilyIndex: presentQueueFamilyIndex,
                queueCount: 1,
                pQueuePriorities: &presentQueuePriority
            )
        ];
        
        byte*[] extensionNames = new byte*[requiredDeviceExtensions.Count];
        try {
            int extensionIndex = 0;
            foreach (string extension in requiredDeviceExtensions) {
                extensionNames[extensionIndex++] = (byte*)Marshal.StringToHGlobalAnsi(extension);
            }

            fixed (DeviceQueueCreateInfo* pDeviceQueueCreateInfos = deviceQueueCreateInfos) 
            fixed (byte** pExtensionNames = extensionNames) {
                DeviceCreateInfo deviceCreateInfo = new(
                    StructureType.DeviceCreateInfo,
                    pNext: null,
                    flags: 0,
                    queueCreateInfoCount: (uint)deviceQueueCreateInfos.Length,
                    pQueueCreateInfos: pDeviceQueueCreateInfos,
                    enabledLayerCount: 0,
                    ppEnabledLayerNames: null,
                    enabledExtensionCount: (uint)extensionNames.Length,
                    ppEnabledExtensionNames: pExtensionNames,
                    pEnabledFeatures: null
                );

                Vk.CreateDevice(chosenPhysicalDevice, in deviceCreateInfo, null, out Device)
                    .AssertSuccess("Failed to create logical Vulkan device.");
            }
        } finally {
            foreach (byte* pExtensionName in extensionNames) {
                Marshal.FreeHGlobal(new IntPtr(pExtensionName));
            }
        }

        //
        // Get the queues
        //
        GraphicsQueue = Vk.GetDeviceQueue(Device, queueFamilyIndexForQueueType[QueueFlags.GraphicsBit], 0);
        PresentationQueue = Vk.GetDeviceQueue(Device, presentQueueFamilyIndex, 0);
        
        //
        // Set up the swap chain
        //
        VkKhrSurface.GetPhysicalDeviceSurfaceCapabilities(chosenPhysicalDevice, surface, out SurfaceCapabilitiesKHR surfaceCapabilities)
            .AssertSuccess("Failed to get physical device surface capabilities");
        
        uint numSurfaceFormats = 0;
        VkKhrSurface.GetPhysicalDeviceSurfaceFormats(chosenPhysicalDevice, surface, ref numSurfaceFormats, null)
            .AssertSuccess("Failed to get number of physical device surface formats");
        SurfaceFormatKHR[] surfaceFormats = new SurfaceFormatKHR[numSurfaceFormats];
        fixed (SurfaceFormatKHR* pSurfaceFormats = surfaceFormats) {
            VkKhrSurface.GetPhysicalDeviceSurfaceFormats(chosenPhysicalDevice, surface, ref numSurfaceFormats, pSurfaceFormats)
                .AssertSuccess("Failed to get phsical device surface formats");
        }

        uint numSurfacePresentModes = 0;
        VkKhrSurface.GetPhysicalDeviceSurfacePresentModes(chosenPhysicalDevice, surface, ref numSurfacePresentModes, null)
            .AssertSuccess("Failed to get number of physical device surface present modes");
        PresentModeKHR[] surfacePresentModes = new PresentModeKHR[numSurfacePresentModes];
        fixed (PresentModeKHR* pSurfacePresentModes = surfacePresentModes) {
            VkKhrSurface.GetPhysicalDeviceSurfacePresentModes(chosenPhysicalDevice, surface, ref numSurfacePresentModes, pSurfacePresentModes)
                .AssertSuccess("Failed to get physical device surface present modes");
        }

        if (numSurfaceFormats == 0 || numSurfacePresentModes == 0) {
            throw new InvalidOperationException("Surface is not adequate.");
        }

        SurfaceFormatKHR chosenSurfaceFormat = surfaceFormats[0];
        
        foreach (SurfaceFormatKHR format in surfaceFormats) {
            if (format is { Format: Format.R8G8B8A8Srgb, ColorSpace: ColorSpaceKHR.SpaceSrgbNonlinearKhr }) {
                chosenSurfaceFormat = format;
                break;
            }
        }

        // FIFO is guaranteed to be present.
        PresentModeKHR chosenPresentMode = PresentModeKHR.FifoKhr;

        Extent2D swapExtent;
        if (surfaceCapabilities.CurrentExtent.Width != uint.MaxValue) {
            swapExtent = surfaceCapabilities.CurrentExtent;
        } else {
            // TODO: Get current GLFW framebuffer size.
            swapExtent = new Extent2D(
                Math.Clamp(800, surfaceCapabilities.MinImageExtent.Width, surfaceCapabilities.MaxImageExtent.Width),
                Math.Clamp(600, surfaceCapabilities.MinImageExtent.Height, surfaceCapabilities.MaxImageExtent.Height)
            );
        }

        // TODO: Tutorial specifies that we should acquire 1 more than the minimum. Verify this..
        uint swapchainImageCount = surfaceCapabilities.MinImageCount + 1;
        if (surfaceCapabilities.MaxImageCount > 0 && swapchainImageCount > surfaceCapabilities.MaxImageCount) {
            swapchainImageCount = surfaceCapabilities.MaxImageCount;
        }
        
        FrameCount = (int)swapchainImageCount;
        
        Span<uint> swapchainSharedQueueFamilyIndices =
            [queueFamilyIndexForQueueType[QueueFlags.GraphicsBit], presentQueueFamilyIndex];
        bool swapchainIsSharingQueueFamilies =
            swapchainSharedQueueFamilyIndices[0] != swapchainSharedQueueFamilyIndices[1];

        fixed (uint* pSharedQueueFamilyIndices = swapchainSharedQueueFamilyIndices) {
            SwapchainCreateInfoKHR swapchainCreateInfo = new(
                sType: StructureType.SwapchainCreateInfoKhr,
                pNext: null,
                flags: SwapchainCreateFlagsKHR.None,
                surface: surface,
                minImageCount: swapchainImageCount,
                imageFormat: chosenSurfaceFormat.Format,
                imageColorSpace: chosenSurfaceFormat.ColorSpace,
                imageExtent: swapExtent,
                imageArrayLayers: 1,
                imageUsage: ImageUsageFlags.ColorAttachmentBit,
                imageSharingMode: SharingMode.Exclusive,
                queueFamilyIndexCount: swapchainIsSharingQueueFamilies
                    ? (uint)swapchainSharedQueueFamilyIndices.Length
                    : 0,
                pQueueFamilyIndices: pSharedQueueFamilyIndices,
                preTransform: surfaceCapabilities.CurrentTransform,
                compositeAlpha: CompositeAlphaFlagsKHR.OpaqueBitKhr,
                presentMode: chosenPresentMode,
                clipped: true,
                oldSwapchain: new SwapchainKHR()
            );
            VkKhrSwapchain.CreateSwapchain(Device, in swapchainCreateInfo, null, out Swapchain)
                .AssertSuccess("Failed to create swapchain");
        }
        
        //
        // Get the swapchain images
        //
        _swapchainRenderTarget = new VulkanSwapchainRenderTarget(this, Swapchain, swapExtent, chosenSurfaceFormat);
        
        //
        // Create a command pool
        // 
        CommandPoolCreateInfo commandPoolCreateInfo = new CommandPoolCreateInfo(
            StructureType.CommandPoolCreateInfo,
            pNext: null,
            flags: CommandPoolCreateFlags.TransientBit | CommandPoolCreateFlags.ResetCommandBufferBit,
            queueFamilyIndex: queueFamilyIndexForQueueType[QueueFlags.GraphicsBit]
        );
        Vk.CreateCommandPool(Device, in commandPoolCreateInfo, null, out CommandPool)
            .AssertSuccess("Failed to create command pool.");
        
        MemoryHelper = new VulkanMemoryHelper(this);
        DescriptorPoolManager = new DescriptorPoolManager(this, [
            DescriptorType.UniformBuffer,
            DescriptorType.StorageBuffer,
            DescriptorType.CombinedImageSampler
            // DescriptorType.SampledImage,
            // DescriptorType.Sampler
        ]);
        
        //
        // Prepare the first pre-frame command buffer.
        //
        PreparePreFrameCommandBuffer();
        _encoderListStart.ResetAsFront(_encoderListEnd);
        
        //
        // Create some fallback objects
        //
        FallbackTexture = (VulkanTexture)RendererUtil.CreateFallbackTexture(this);
        FallbackSampler = (VulkanSampler)CreateSampler(default);
    }

    private unsafe void PreparePreFrameCommandBuffer()
    {
        // TODO: How do we free this buffer?
        
        CommandBufferAllocateInfo allocateInfo = new(
            sType: StructureType.CommandBufferAllocateInfo,
            pNext: null,
            commandPool: CommandPool,
            level: CommandBufferLevel.Primary,
            commandBufferCount: 1
        );
        Vk.AllocateCommandBuffers(Device, in allocateInfo, out _preFrameCommandBuffer)
            .AssertSuccess("Failed to allocate pre-frame command buffer.");

        CommandBufferBeginInfo beginInfo = new(
            sType: StructureType.CommandBufferBeginInfo,
            pNext: null,
            flags: CommandBufferUsageFlags.OneTimeSubmitBit,
            pInheritanceInfo: null // Ignored, not a secondary command buffer.
        );
        Vk.BeginCommandBuffer(_preFrameCommandBuffer, in beginInfo)
            .AssertSuccess("Failed to begin recording pre-frame command buffer.");
    }

    internal void DeferDisposal(IDeferredDisposable disposable)
    {
        // These are disposed at the begining of the associated working frame.
        _deferedDisposableByWorkingFrame[WorkingFrame].Add(disposable);
    }
    
    public IStaticBuffer<T> CreateStaticBuffer<T>(int elementCount) where T : unmanaged
    {
        VulkanStaticBuffer<T> buffer = new VulkanStaticBuffer<T>(this);
        buffer.Allocate((uint)elementCount);
        return buffer;
    }

    public IStreamingBuffer<T> CreateStreamingBuffer<T>(int elementCount) where T : unmanaged
    {
        throw new NotImplementedException();
    }

    public ITexture CreateTexture()
    {
        return new VulkanTexture(this);
    }

    public ISampler CreateSampler(in SamplerDescription description)
    {
        return new VulkanSampler(this, in description);
    }

    public IShaderBacking CreateShaderBacking(IShader shader)
    {
        return new VulkanShaderBacking(this, shader);
    }

    public IShaderInstanceBacking CreateShaderInstanceBacking(IShader shader)
    {
        if (shader.Backing is not VulkanShaderBacking backing) {
            throw new ArgumentException("Shader's backing is incompatible with this renderer.", nameof(shader));
        }
        return new VulkanShaderInstanceBacking(backing);
    }

    public IDescriptorSet CreateDescriptorSet(IShaderBacking shader, ShaderStage stage, int index, in DescriptorSetCreationHints hints)
    {
        if (shader is not VulkanShaderBacking vulkanShaderBacking) {
            throw new ArgumentException("Given shader backing is not compatible with this renderer.", nameof(shader));
        }
        return new VulkanDescriptorSet(this, vulkanShaderBacking, index, in hints);
    }

    public void RegisterPassType<TRenderPass>(RenderPassDefinition definition) where TRenderPass : IRenderPass
    {
        _passBackings.Add(typeof(TRenderPass), new VulkanPassBacking(this, definition));
    }

    private VulkanPassBacking GetPassBackingOrThrow<TRenderPass>()
    {
        if (!_passBackings.TryGetValue(typeof(TRenderPass), out VulkanPassBacking? passBacking)) {
            throw new InvalidOperationException(
                $"Pass of type {typeof(TRenderPass)} has not been registered. You must call RegisterPassType first.");
        }
        return passBacking;
    }
    
    public IPipeline<TRenderPass, TShader, TVertexBufferLayout> CreatePipeline<TRenderPass, TShader, TVertexBufferLayout>(
        PipelineDefinition definition, 
        TShader shader,
        TVertexBufferLayout vertexBufferLayout
    )
        where TRenderPass : IRenderPass
        where TShader : IShader
        where TVertexBufferLayout : IVertexBufferLayout<TShader>
    {
        VulkanPassBacking passBacking = GetPassBackingOrThrow<TRenderPass>();
        return new VulkanPipeline<TRenderPass, TShader, TVertexBufferLayout>(this, definition, passBacking, shader, vertexBufferLayout);
    }

    public IMutableFramebuffer CreateFramebuffer<TRenderPass>() where TRenderPass : IRenderPass
    {
        VulkanPassBacking passBacking = GetPassBackingOrThrow<TRenderPass>();
        return new VulkanFramebuffer(this, passBacking);
    }

    public IRenderTarget CreateRenderTarget(ColorFormat format, uint width, uint height)
    {
        return new VulkanRenderTarget(this, true, format, default, width, height,
            ImageUsageFlags.ColorAttachmentBit | ImageUsageFlags.SampledBit,
            ImageAspectFlags.ColorBit
        );
    }

    public IRenderTarget CreateRenderTarget(DepthStencilFormat format, uint width, uint height)
    {
        return new VulkanRenderTarget(this, false, default, format, width, height, 
            ImageUsageFlags.DepthStencilAttachmentBit | ImageUsageFlags.SampledBit,
            ImageAspectFlags.DepthBit | ImageAspectFlags.StencilBit
        );
    }

    public IRenderTarget GetSwapchainColorTarget()
    {
        return _swapchainRenderTarget;
    }

    public IPass<TRenderPass> CreatePassEncoder<TRenderPass>(
        TRenderPass pass,
        IPass? occursBefore
    )
        where TRenderPass : IRenderPass
    {
        // TODO: Move this check to shared CeresGPU renderer checkes.
        if (!pass.Framebuffer.IsSetup) {
            throw new InvalidOperationException(
                "Framebuffer has not been set up. Make sure your render pass impl sets up the framebuffer.");
        }
        
        if (pass.Framebuffer is not VulkanFramebuffer vkFramebuffer) {
            throw new ArgumentException("Backend type of pass is not compatible with this renderer.", nameof(pass));
        }
        
        VulkanPassBacking passBacking = GetPassBackingOrThrow<TRenderPass>();
        
        VulkanCommandEncoder<TRenderPass> encoder = new(this, passBacking, vkFramebuffer);
        
        if (occursBefore == null) {
            encoder.InsertAfter(_encoderListEnd.Prev!);
        } else {
            encoder.InsertBefore((VulkanCommandEncoder)occursBefore);
        }

        _passesToSubmit.Add(encoder);
        return encoder;
    }

    // A buffer of.. buffers
    private CommandBuffer[] _reusedCommandBufferList = [];
    
    public unsafe void Present(float minimumElapsedSeocnds)
    {
        // Submit the pre-frame command buffer first.
        Vk.EndCommandBuffer(_preFrameCommandBuffer);
        CommandBuffer preFrameCommandBuffer = _preFrameCommandBuffer;
        SubmitInfo preFrameCommandsSubmitInfo = new(
            sType: StructureType.SubmitInfo,
            pNext: null,
            waitSemaphoreCount: 0,
            pWaitSemaphores: null,
            pWaitDstStageMask: null,
            commandBufferCount: 1,
            pCommandBuffers: &preFrameCommandBuffer,
            signalSemaphoreCount: 0,
            pSignalSemaphores: null
        );
        Vk.QueueSubmit(GraphicsQueue, 1, in preFrameCommandsSubmitInfo, default);
        
        // Gather the command buffers in order of submission
        if (_reusedCommandBufferList.Length < _passesToSubmit.Count) {
            // 64 is arbitrary. Seems like a nice size to grow by.
            _reusedCommandBufferList = new CommandBuffer[AlignUtil.AlignUp((ulong)_passesToSubmit.Count, 64)];  
        }
        
        VulkanCommandEncoder? currentEncoder = _encoderListStart.Next;
        for (int i = 0, ilen = _passesToSubmit.Count; i < ilen; ++i) {
            if (currentEncoder == null) {
                throw new InvalidOperationException("Unexpected end of command buffer list. (Likely a bug in CeresGpu)");
            }

            // Finish recording if it wasn't finished already.
            currentEncoder.Finish();

            _reusedCommandBufferList[i] = currentEncoder.CommandBuffer;
            currentEncoder = currentEncoder.Next;
        }

        // Submit the passes
        fixed (CommandBuffer* pCommandBuffers = _reusedCommandBufferList) {
            SubmitInfo submitInfo = new SubmitInfo(
                sType: StructureType.SubmitInfo,
                pNext: null,
                waitSemaphoreCount: 0,
                pWaitSemaphores: null,
                pWaitDstStageMask: null,
                commandBufferCount: (uint)_passesToSubmit.Count,
                pCommandBuffers: pCommandBuffers,
                signalSemaphoreCount: 0,
                pSignalSemaphores: null
            );
            Vk.QueueSubmit(GraphicsQueue, 1, in submitInfo, default)
                .AssertSuccess("Failed to submit command buffers to graphics queue.");
        }
        
        // Dispose all command encoders -- this will defer deletion of the underlying command buffers appropriately.
        foreach (IVulkanCommandEncoder encoder in _passesToSubmit) {
            encoder.Dispose();
        }

        // Prepare next frame
        _passesToSubmit.Clear();
        _encoderListStart.ResetAsFront(_encoderListEnd);

        WorkingFrame = (WorkingFrame + 1) % FrameCount;
        
        // Delete anything that's ready to be disposed now
        List<IDeferredDisposable> deferredDisposables = _deferedDisposableByWorkingFrame[WorkingFrame];
        for (int i = deferredDisposables.Count - 1; i >= 0; --i) {
            deferredDisposables[i].DeferredDispose();
        }
        deferredDisposables.Clear();

        PreparePreFrameCommandBuffer();
        
    }

    public void GetDiagnosticInfo(IList<(string key, object value)> entries)
    {
    }

    private unsafe void ReleaseUnmanagedResources()
    {
        if (Device.Handle != IntPtr.Zero) {
            if (CommandPool.Handle != 0) {
                Vk.DestroyCommandPool(Device, CommandPool, null);
            }
            
            Vk.DestroyDevice(Device, null);
        }

        if (Instance.Handle != IntPtr.Zero) {
            Vk.DestroyInstance(Instance, null);
        }
        
        Vk.Dispose();
    }
    
    public void Dispose()
    {
        if (IsDisposed) {
            throw new ObjectDisposedException("this");
        }
        IsDisposed = true;
        
        // Call managed disposed methods here
        foreach (VulkanPassBacking passBacking in _passBackings.Values) {
            passBacking.Dispose();
        }
        
        GC.SuppressFinalize(this);
        ReleaseUnmanagedResources();
    }

    ~VulkanRenderer()
    {
        ReleaseUnmanagedResources();
    }
}