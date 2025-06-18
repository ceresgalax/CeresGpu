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

public sealed class VulkanRenderer : IRenderer
{
    public uint UniqueFrameId { get; private set; }

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

    private readonly Semaphore[] _acquireImageSemaphores;
    private readonly Semaphore[] _presentationSemaphores;
    private readonly Fence[] _workFences;

    private readonly VulkanSwapchainRenderTarget _swapchainRenderTarget;
        
    private readonly Dictionary<Type, VulkanPassBacking> _passBackings = [];

    public readonly VulkanMemoryHelper MemoryHelper;
    public readonly DescriptorPoolManager DescriptorPoolManager;

    private CommandBuffer _preFrameCommandBuffer;
    private CommandBuffer _postFrameCommandBuffer;
    
    private readonly List<IDeferredDisposable>[] _deferedDisposableByWorkingFrame;

    /// <summary>
    /// Contains the passes that are to be submitted this frame.
    /// </summary>
    private readonly HashSet<VulkanCommandEncoder> _passesToSubmit = new();
    
    // NOTE: These are just anchors, and are not to be submitted.
    private readonly VulkanCommandEncoderAnchor _encoderListStart = new();
    private readonly VulkanCommandEncoderAnchor _encoderListEnd = new();
    
    public readonly IVulkanTexture FallbackTexture;
    public readonly VulkanSampler FallbackSampler;

    public int FrameCount { get; private set; }
    public int WorkingFrame { get; private set; }
    
    public int CurrentFrameSwapchainImageIndex { get; private set; }
    
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
        // Create other objects needed for renderer
        //
        SemaphoreCreateInfo semaphoreCreateInfo = new(
            sType: StructureType.SemaphoreCreateInfo,
            pNext: null,
            flags: SemaphoreCreateFlags.None
        );
        FenceCreateInfo workFenceCreateInfo = new(
            sType: StructureType.FenceCreateInfo,
            pNext: null,
            // These work fences start off as signaled, since there's no work that 
            flags: FenceCreateFlags.SignaledBit
        );
        
        _acquireImageSemaphores = new Semaphore[FrameCount];
        _presentationSemaphores = new Semaphore[FrameCount];
        _workFences = new Fence[FrameCount];
        
        for (int i = 0; i < _acquireImageSemaphores.Length; ++i) {
            Vk.CreateSemaphore(Device, in semaphoreCreateInfo, null, out _acquireImageSemaphores[i])
                .AssertSuccess("Failed to create semaphore");
            Vk.CreateSemaphore(Device, in semaphoreCreateInfo, null, out _presentationSemaphores[i])
                .AssertSuccess("Failed to create semaphore");
            Vk.CreateFence(Device, in workFenceCreateInfo, null, out _workFences[i])
                .AssertSuccess("Failed to create fence.");
        }

        //
        // Prepare the first frame.
        //
        NewFrame();
        
        //
        // Create some fallback objects after all initialization is complete, and we are in a frame.
        //
        FallbackTexture = (VulkanTexture)RendererUtil.CreateFallbackTexture(this);
        FallbackSampler = (VulkanSampler)CreateSampler(default);
    }

    private unsafe void PreparePreAndPostFrameCommandBuffer()
    {
        // TODO: How do we free this buffer?
        // Ideally we could just release the buffer after each frame and re-use it?

        Span<CommandBuffer> createdCommandBuffers = stackalloc CommandBuffer[2];
        
        CommandBufferAllocateInfo allocateInfo = new(
            sType: StructureType.CommandBufferAllocateInfo,
            pNext: null,
            commandPool: CommandPool,
            level: CommandBufferLevel.Primary,
            commandBufferCount: (uint)createdCommandBuffers.Length
        );
        fixed (CommandBuffer* pCommandBuffers = createdCommandBuffers) {
            Vk.AllocateCommandBuffers(Device, in allocateInfo, pCommandBuffers)
                .AssertSuccess("Failed to allocate pre-frame command buffer.");    
        }

        _preFrameCommandBuffer = createdCommandBuffers[0];
        _postFrameCommandBuffer = createdCommandBuffers[1];

        CommandBufferBeginInfo beginInfo = new(
            sType: StructureType.CommandBufferBeginInfo,
            pNext: null,
            flags: CommandBufferUsageFlags.OneTimeSubmitBit,
            pInheritanceInfo: null // Ignored, not a secondary command buffer.
        );
        Vk.BeginCommandBuffer(_preFrameCommandBuffer, in beginInfo)
            .AssertSuccess("Failed to begin recording pre-frame command buffer.");
        Vk.BeginCommandBuffer(_postFrameCommandBuffer, in beginInfo)
            .AssertSuccess("Failed to begin recording post-frame command buffer.");
    }

    internal void DeferDisposal(IDeferredDisposable disposable)
    {
        // These are disposed at the beginning of the associated working frame.
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
        VulkanStreamingBuffer<T> buffer = new VulkanStreamingBuffer<T>(this);
        buffer.Allocate((uint)elementCount);
        return buffer;
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
        return new VulkanShaderInstanceBacking(this, backing);
    }

    public bool IsPassRegistered<TRenderPass>() where TRenderPass : IRenderPass
    {
        return _passBackings.ContainsKey(typeof(TRenderPass));
    }

    public void RegisterPassType<TRenderPass>(RenderPassDefinition definition) where TRenderPass : IRenderPass
    {
        _passBackings.Add(typeof(TRenderPass), new VulkanPassBacking(this, definition));
    }

    private VulkanPassBacking GetPassBackingOrThrow(Type passType)
    {
        if (!_passBackings.TryGetValue(passType, out VulkanPassBacking? passBacking)) {
            throw new InvalidOperationException($"Pass of type {passType} has not been registered. You must call RegisterPassType first.");
        }
        return passBacking;
    }
    
    public IPipeline<TShader, TVertexBufferLayout> CreatePipeline<TShader, TVertexBufferLayout>(
        PipelineDefinition definition,
        ReadOnlySpan<Type> compatiblePasses,
        TShader shader,
        TVertexBufferLayout vertexBufferLayout
    )
        where TShader : IShader
        where TVertexBufferLayout : IVertexBufferLayout<TShader>
    {
        VulkanPassBacking[] compatiblePassBackings = new VulkanPassBacking[compatiblePasses.Length];
        for (int i = 0; i < compatiblePasses.Length; ++i) {
            compatiblePassBackings[i] = GetPassBackingOrThrow(compatiblePasses[i]);
        }
        
        return new VulkanPipeline<TShader, TVertexBufferLayout>(this, definition, compatiblePassBackings, shader, vertexBufferLayout);
    }

    public IFramebuffer CreateFramebuffer<TRenderPass>(ReadOnlySpan<IRenderTarget> colorAttachments, IRenderTarget? depthStencilAttachment) where TRenderPass : IRenderPass
    {
        VulkanPassBacking passBacking = GetPassBackingOrThrow(typeof(TRenderPass));
        return new VulkanFramebuffer(this, passBacking, colorAttachments, depthStencilAttachment);
    }

    public IRenderTarget CreateRenderTarget(ColorFormat format, bool matchesSwapchainSize, uint width, uint height)
    {
        return new VulkanRenderTarget(this, true, format, default, matchesSwapchainSize, width, height,
            ImageUsageFlags.ColorAttachmentBit | ImageUsageFlags.SampledBit,
            ImageAspectFlags.ColorBit
        );
    }

    public IRenderTarget CreateRenderTarget(DepthStencilFormat format, bool matchesSwapchainSize, uint width, uint height)
    {
        return new VulkanRenderTarget(this, false, default, format, matchesSwapchainSize, width, height, 
            ImageUsageFlags.DepthStencilAttachmentBit | ImageUsageFlags.SampledBit,
            ImageAspectFlags.DepthBit | ImageAspectFlags.StencilBit
        );
    }

    public IRenderTarget GetSwapchainColorTarget()
    {
        return _swapchainRenderTarget;
    }

    public IPass CreatePassEncoder<TRenderPass>(
        TRenderPass pass,
        IPass? occursBefore
    )
        where TRenderPass : IRenderPass
    {
        if (pass.Framebuffer is not VulkanFramebuffer vkFramebuffer) {
            throw new ArgumentException("Backend type of pass is not compatible with this renderer.", nameof(pass));
        }
        
        VulkanPassBacking passBacking = GetPassBackingOrThrow(typeof(TRenderPass));
        VulkanCommandEncoder encoder = new(this, passBacking, vkFramebuffer);
        
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
        Vk.EndCommandBuffer(_preFrameCommandBuffer);
        Vk.EndCommandBuffer(_postFrameCommandBuffer);

        int numCommandBuffersToSubmit = _passesToSubmit.Count + 2;
        
        // Gather the command buffers in order of submission
        
        if (_reusedCommandBufferList.Length < numCommandBuffersToSubmit) {
            // 64 is arbitrary. Seems like a nice size to grow by.
            _reusedCommandBufferList = new CommandBuffer[AlignUtil.AlignUp((ulong)numCommandBuffersToSubmit, 64)];  
        }
        
        _reusedCommandBufferList[0] = _preFrameCommandBuffer;
        _reusedCommandBufferList[numCommandBuffersToSubmit - 1] = _postFrameCommandBuffer;
        
        IVulkanCommandEncoder? currentEncoder = _encoderListStart.Next;
        for (int i = 0, ilen = _passesToSubmit.Count; i < ilen; ++i) {
            if (currentEncoder == null) {
                throw new InvalidOperationException("Unexpected end of command buffer list. (Likely a bug in CeresGpu)");
            }

            // Finish recording if it wasn't finished already.
            currentEncoder.Finish();

            _reusedCommandBufferList[i + 1] = currentEncoder.CommandBuffer;
            currentEncoder = currentEncoder.Next;
        }
        
        // Submit the passes
        Semaphore acquireImageSemaphore = _acquireImageSemaphores[WorkingFrame];
        PipelineStageFlags acquireImageWaitStage = PipelineStageFlags.ColorAttachmentOutputBit;
        Semaphore presentSemaphore = _presentationSemaphores[WorkingFrame];
        fixed (CommandBuffer* pCommandBuffers = _reusedCommandBufferList) {
            SubmitInfo submitInfo = new SubmitInfo(
                sType: StructureType.SubmitInfo,
                pNext: null,
                waitSemaphoreCount: 1,
                pWaitSemaphores: &acquireImageSemaphore,
                pWaitDstStageMask: &acquireImageWaitStage,
                commandBufferCount: (uint)numCommandBuffersToSubmit,
                pCommandBuffers: pCommandBuffers,
                signalSemaphoreCount: 1,
                pSignalSemaphores: &presentSemaphore
            );
            Vk.QueueSubmit(GraphicsQueue, 1, in submitInfo, _workFences[WorkingFrame])
                .AssertSuccess("Failed to submit command buffers to graphics queue.");
        }
        
        // Dispose all command encoders -- this will defer deletion of the underlying command buffers appropriately.
        foreach (VulkanCommandEncoder encoder in _passesToSubmit) {
            encoder.Dispose();
        }
        _passesToSubmit.Clear();

        //
        // Present the frame
        //
        SwapchainKHR swapchain = Swapchain;
        uint imageToPresent = (uint)CurrentFrameSwapchainImageIndex;
        PresentInfoKHR presentInfo = new(
            sType: StructureType.PresentInfoKhr,
            pNext: null,
            waitSemaphoreCount: 1,
            pWaitSemaphores: &presentSemaphore,
            swapchainCount: 1,
            pSwapchains: &swapchain,
            pImageIndices: &imageToPresent,
            pResults: null
        );
        VkKhrSwapchain.QueuePresent(PresentationQueue, in presentInfo)
            .AssertSuccess("Failed to present");
        
        //
        // Prepare next frame
        //
        WorkingFrame = (WorkingFrame + 1) % FrameCount;
        ++UniqueFrameId;

        NewFrame();
    }

    private unsafe void NewFrame()
    {
        _encoderListStart.ResetAsFront(_encoderListEnd);
        
        // Wait for the existing work in this working frame to be completed.
        Fence fence = _workFences[WorkingFrame];
        Vk.WaitForFences(Device, 1, in fence, Vk.True, UInt64.MaxValue)
            .AssertSuccess("Failed to wait for fence.");
        Vk.ResetFences(Device, 1, in fence)
            .AssertSuccess("Failed to reset fence.");
        
        // Delete anything that's ready to be disposed now
        List<IDeferredDisposable> deferredDisposables = _deferedDisposableByWorkingFrame[WorkingFrame];
        for (int i = deferredDisposables.Count - 1; i >= 0; --i) {
            deferredDisposables[i].DeferredDispose();
        }
        deferredDisposables.Clear();
        
        PreparePreAndPostFrameCommandBuffer();
        
        // Acquire the next swapchain image
        uint swapchainImageIndex = 0;
        VkKhrSwapchain.AcquireNextImage(Device, Swapchain, UInt64.MaxValue, _acquireImageSemaphores[WorkingFrame], default, ref swapchainImageIndex)
            .AssertSuccess("Failed to acquire next image");
        CurrentFrameSwapchainImageIndex = (int)swapchainImageIndex;

        // Add transition the current frame swapchain image in our pre-frame buffer.
        ImageMemoryBarrier beginingImageBarrier = new(
            sType: StructureType.ImageMemoryBarrier,
            pNext: null,
            srcAccessMask: AccessFlags.None,
            dstAccessMask: AccessFlags.ColorAttachmentWriteBit,
            oldLayout: ImageLayout.Undefined,
            newLayout: ImageLayout.ColorAttachmentOptimal,
            srcQueueFamilyIndex: Vk.QueueFamilyIgnored,
            dstQueueFamilyIndex: Vk.QueueFamilyIgnored,
            image: _swapchainRenderTarget._images[CurrentFrameSwapchainImageIndex],
            subresourceRange: new ImageSubresourceRange(
                ImageAspectFlags.ColorBit,
                baseMipLevel: 0,
                levelCount: 1,
                baseArrayLayer: 0,
                layerCount: 1
            )
        );
        Vk.CmdPipelineBarrier(
            commandBuffer: _preFrameCommandBuffer,
            srcStageMask: PipelineStageFlags.TopOfPipeBit,
            // dstStageMask: PipelineStageFlags.ColorAttachmentOutputBit,
            dstStageMask: PipelineStageFlags.AllGraphicsBit,
            dependencyFlags: DependencyFlags.None,
            memoryBarrierCount: 0,
            pMemoryBarriers: null,
            imageMemoryBarrierCount: 1,
            pImageMemoryBarriers: &beginingImageBarrier,
            bufferMemoryBarrierCount: 0,
            pBufferMemoryBarriers: null
        );
        
        // Schedule the swap chain image to be transitioned to the PresentSrc layout at the very end of this frame before presenting.
        ImageMemoryBarrier endingImageBarrier = new(
            sType: StructureType.ImageMemoryBarrier,
            pNext: null,
            srcAccessMask: AccessFlags.ColorAttachmentWriteBit,
            // I think this is correct.. Presentation doesn't occur until the semaphore passed to submit has been signaled,
            // and I think Vulkan has made all memory related to this image visible by then? 
            // Besides, I don't know of an access mask flag that is appropriate for "presentation read"
            dstAccessMask: AccessFlags.None, 
            oldLayout: ImageLayout.ColorAttachmentOptimal,
            newLayout: ImageLayout.PresentSrcKhr,
            srcQueueFamilyIndex: Vk.QueueFamilyIgnored,
            dstQueueFamilyIndex: Vk.QueueFamilyIgnored,
            image: _swapchainRenderTarget._images[CurrentFrameSwapchainImageIndex],
            subresourceRange: new ImageSubresourceRange(
                ImageAspectFlags.ColorBit,
                baseMipLevel: 0,
                levelCount: 1,
                baseArrayLayer: 0,
                layerCount: 1
            )
        );
        Vk.CmdPipelineBarrier(
            commandBuffer: _postFrameCommandBuffer,
            srcStageMask: PipelineStageFlags.ColorAttachmentOutputBit,
            dstStageMask: PipelineStageFlags.BottomOfPipeBit,
            dependencyFlags: DependencyFlags.None,
            memoryBarrierCount: 0,
            pMemoryBarriers: null,
            imageMemoryBarrierCount: 1,
            pImageMemoryBarriers: &endingImageBarrier,
            bufferMemoryBarrierCount: 0,
            pBufferMemoryBarriers: null
        );
        
        
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