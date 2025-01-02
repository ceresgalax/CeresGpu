using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using CeresGpu.Graphics.Shaders;
using Silk.NET.Vulkan;

namespace CeresGpu.Graphics.Vulkan;

public sealed class VulkanRenderer : IRenderer
{
    public uint UniqueFrameId { get; }

    public readonly Vk Vk = Vk.GetApi();

    public readonly Instance Instance;
    public readonly PhysicalDevice PhysicalDevice;
    public readonly Device Device;
    public readonly Queue GraphicsQueue;
    public readonly CommandPool CommandPool;

    private Dictionary<Type, VulkanPassBacking> _passBackings = [];

    public int FrameCount => 3; 
    public int WorkingFrame { get; private set; }
    
    public unsafe VulkanRenderer()
    {
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

            byte*[]? enabledLayerNames = null;
            try {
                enabledLayerNames = [];
                fixed (byte** pEnabledLayerNames = enabledLayerNames) {

                    InstanceCreateInfo instanceCreateInfo = new(
                        StructureType.InstanceCreateInfo,
                        pNext: null,
                        flags: InstanceCreateFlags.None,
                        pApplicationInfo: &applicationInfo,
                        enabledLayerCount: (uint)enabledLayerNames.Length,
                        ppEnabledLayerNames: pEnabledLayerNames,
                        enabledExtensionCount: 0,
                        ppEnabledExtensionNames: null
                    );

                    if (Vk.CreateInstance(&instanceCreateInfo, null, out Instance) != Result.Success) {
                        throw new InvalidOperationException("Failed to create Vulkan Instance.");
                    }
                }
            } finally {
                foreach (byte* str in enabledLayerNames ?? []) {
                    Marshal.FreeHGlobal(new IntPtr(str));
                }
            }
        }
        
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

        QueueFlags neededQueues = QueueFlags.GraphicsBit;

        Dictionary<QueueFlags, uint> queueFamilyIndexForQueueType = [];
        
        PhysicalDevice chosenPhysicalDevice = default;
        foreach (PhysicalDevice physicalDevice in physicalDevices) {
            queueFamilyIndexForQueueType.Clear();
            
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
            for (int queueFamilyIndex = 0; queueFamilyIndex < numQueueFamilies; ++queueFamilyIndex) {
                ref readonly QueueFamilyProperties familyProperties = ref families[queueFamilyIndex];
                if (familyProperties.QueueCount <= 0) {
                    continue;
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
            if ((availableQueueFlags & neededQueues) == neededQueues) {
                chosenPhysicalDevice = physicalDevice;
                break;
            }
        }

        if (chosenPhysicalDevice.Handle == IntPtr.Zero) {
            throw new InvalidOperationException("Failed to find a suitable physical device.");
        }

        PhysicalDevice = chosenPhysicalDevice;
        
        //
        // Create the logical device.
        //

        float graphicsQueuePriority = 1f;
        
        Span<DeviceQueueCreateInfo> deviceQueueCreateInfos = stackalloc DeviceQueueCreateInfo[1] {
            // Graphics queue
            new DeviceQueueCreateInfo(
                StructureType.DeviceQueueCreateInfo,
                pNext: null,
                flags: DeviceQueueCreateFlags.None,
                queueFamilyIndex: queueFamilyIndexForQueueType[QueueFlags.GraphicsBit],
                queueCount: 1,
                pQueuePriorities: &graphicsQueuePriority
            )
        };
        
        fixed (DeviceQueueCreateInfo* pDeviceQueueCreateInfos = deviceQueueCreateInfos)
        {
            DeviceCreateInfo deviceCreateInfo = new(
                StructureType.DeviceCreateInfo,
                pNext: null,
                flags: 0,
                queueCreateInfoCount: (uint)deviceQueueCreateInfos.Length,
                pQueueCreateInfos: pDeviceQueueCreateInfos,
                enabledLayerCount: 0,
                ppEnabledLayerNames: null,
                enabledExtensionCount: 0,
                ppEnabledExtensionNames: null,
                pEnabledFeatures: null
            );

            Vk.CreateDevice(chosenPhysicalDevice, in deviceCreateInfo, null, out Device)
                .AssertSuccess("Failed to create logical Vulkan device.");
        }
        
        //
        // Get the queues
        //
        GraphicsQueue = Vk.GetDeviceQueue(Device, queueFamilyIndexForQueueType[QueueFlags.GraphicsBit], 0);
        
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
    }
    
    public IStaticBuffer<T> CreateStaticBuffer<T>(int elementCount) where T : unmanaged
    {
        throw new NotImplementedException();
    }

    public IStreamingBuffer<T> CreateStreamingBuffer<T>(int elementCount) where T : unmanaged
    {
        throw new NotImplementedException();
    }

    public ITexture CreateTexture()
    {
        throw new NotImplementedException();
    }

    public ISampler CreateSampler(in SamplerDescription description)
    {
        throw new NotImplementedException();
    }

    public IShaderBacking CreateShaderBacking(IShader shader)
    {
        return new VulkanShaderBacking(this, shader);
    }

    public IShaderInstanceBacking CreateShaderInstanceBacking(IShader shader)
    {
        throw new NotImplementedException();
    }

    public IDescriptorSet CreateDescriptorSet(IShaderBacking shader, ShaderStage stage, int index,
        in DescriptorSetCreationHints hints)
    {
        throw new NotImplementedException();
    }

    public void RegisterPassType<TRenderPass>(RenderPassDefinition definition) where TRenderPass : IRenderPass
    {
        _passBackings.Add(typeof(TRenderPass), new VulkanPassBacking(this, definition));
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
        if (!_passBackings.TryGetValue(typeof(TRenderPass), out VulkanPassBacking? passBacking)) {
            throw new InvalidOperationException(
                $"Pass of type {typeof(TRenderPass)} has not been registered. You must call RegisterPassType first.");
        }

        return new VulkanPipeline<TRenderPass, TShader, TVertexBufferLayout>(this, definition, passBacking, shader, vertexBufferLayout);
    }

    public IPass<TRenderPass> CreatePass<TRenderPass>(ReadOnlySpan<IPass> dependentPasses, TRenderPass pass)
        where TRenderPass : IRenderPass
    {
        throw new NotImplementedException();
    }

    public void Present(float minimumElapsedSeocnds)
    {
        throw new NotImplementedException();
    }

    public void GetDiagnosticInfo(IList<(string key, object value)> entries)
    {
        throw new NotImplementedException();
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

    private bool _disposed;
    
    public void Dispose()
    {
        if (_disposed) {
            throw new ObjectDisposedException("this");
        }
        _disposed = true;
        
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