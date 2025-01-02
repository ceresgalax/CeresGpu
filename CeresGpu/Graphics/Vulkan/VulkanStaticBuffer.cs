using System;
using System.Runtime.InteropServices;
using CeresGpu.MetalBinding;
using Silk.NET.Vulkan;
using VkBuffer = Silk.NET.Vulkan.Buffer;

namespace CeresGpu.Graphics.Vulkan;

public class VulkanStaticBuffer<T> : StaticBuffer<T>, IVulkanBuffer where T : unmanaged  
{
    private readonly VulkanRenderer _renderer;

    private VkBuffer _buffer;
    private uint _count;
        
    public override uint Count => _count;
    
    public VulkanStaticBuffer(VulkanRenderer renderer)
    {
        _renderer = renderer;
    }

    protected override unsafe void AllocateImpl(uint elementCount)
    {
        Vk vk = _renderer.Vk;
        
        if (_buffer.Handle != 0) {
            // Should be safe to destroy buffer, since the parent class's Allocate method validates that we haven't
            // encoded this buffer in any commands, meaning we don't need to defer delete the buffer.
            vk.DestroyBuffer(_renderer.Device, _buffer, null);    
            _buffer = default;
        }

        // Regarding buffer usage flags, we currently allow buffers to be used by all things buffers could be used for
        // in CeresGpu, similar to how Metal allows all buffers to be used freely for anything without up-front hints.
        // If we have a need to know the usage up front, we could modify the CeresGpu buffer api to allow for some 
        // usage flags, and validate bufferes are being used as declared, regardless of the graphics api.
        BufferUsageFlags usageFlags = BufferUsageFlags.None
            // | BufferUsageFlags.UniformTexelBufferBit
            // | BufferUsageFlags.StorageTexelBufferBit
            | BufferUsageFlags.UniformBufferBit
            | BufferUsageFlags.StorageBufferBit
            | BufferUsageFlags.IndexBufferBit
            | BufferUsageFlags.VertexBufferBit
            | BufferUsageFlags.IndirectBufferBit;
        
        BufferCreateInfo createInfo = new(
            sType: StructureType.BufferCreateInfo,
            pNext: null,
            flags: BufferCreateFlags.None,
            size: (ulong)(elementCount * sizeof(T)),
            usage: usageFlags,
            sharingMode: SharingMode.Exclusive,
            queueFamilyIndexCount: 0,
            pQueueFamilyIndices: null
        );

        vk.CreateBuffer(_renderer.Device, in createInfo, null, out _buffer)
            .AssertSuccess("Failed to create buffer");
        
        // Now put some actual memory behind that buffer.

        vk.GetBufferMemoryRequirements(_renderer.Device, _buffer, out MemoryRequirements requirements);
        
        
        
        vk.BindBufferMemory(_renderer.Device, _buffer, );
        
        
        
        _count = elementCount;
    }

    protected override void SetImpl(uint offset, Span<T> elements, uint count)
    {
        throw new NotImplementedException();
    }

    protected override void SetDirectImpl(IBuffer<T>.DirectSetter setter)
    {
        throw new NotImplementedException();
    }

    public override void Dispose()
    {
        throw new System.NotImplementedException();
    }
}