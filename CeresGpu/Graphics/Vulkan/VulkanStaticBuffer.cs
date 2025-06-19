using System;
using Silk.NET.Vulkan;
using Buffer = System.Buffer;
using VkBuffer = Silk.NET.Vulkan.Buffer;

namespace CeresGpu.Graphics.Vulkan;

public sealed class VulkanStaticBuffer<T> : StaticBuffer<T>, IVulkanBuffer, IDeferredDisposable where T : unmanaged  
{
    private readonly VulkanRenderer _renderer;

    private VkBuffer _buffer;
    private DeviceMemory _memory;
    
    private uint _count;
        
    public override uint Count => _count;

    public VkBuffer Buffer => _buffer;
    
    public VulkanStaticBuffer(VulkanRenderer renderer)
    {
        _renderer = renderer;
    }

    void IDeferredDisposable.DeferredDispose()
    {
        // Release unmanaged resources here.
        Vk vk = _renderer.Vk;
        unsafe {
            if (_buffer.Handle != 0) {
                vk.DestroyBuffer(_renderer.Device, _buffer, null);
                _buffer = default;
            }
            
            if (_memory.Handle != 0) {
                vk.FreeMemory(_renderer.Device, _memory, null);
                _memory = default;
            }
        }
    }
    
    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);

        if (disposing) {
            // Dispose owned IDisposable objects here.
        }

        _renderer.DeferDisposal(this);
    }

    protected override void AllocateImpl(uint elementCount)
    {
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
        
        AllocateWithUsage(elementCount, usageFlags);
    }
    
    public unsafe void AllocateWithUsage(uint elementCount, BufferUsageFlags usageFlags)
    {
        Vk vk = _renderer.Vk;
        
        if (_buffer.Handle != 0) {
            // Should be safe to destroy buffer, since the parent class's Allocate method validates that we haven't
            // encoded this buffer in any commands, meaning we don't need to defer delete the buffer.
            vk.DestroyBuffer(_renderer.Device, _buffer, null);    
            _buffer = default;
        }

        if (elementCount < 0) {
            return;
        }

        BufferCreateInfo createInfo = new(
            sType: StructureType.BufferCreateInfo,
            pNext: null,
            flags: BufferCreateFlags.None,
            size: (ulong)(elementCount * sizeof(T)),
            usage: usageFlags,
            sharingMode: SharingMode.Exclusive,
            // Ignored when SharingMode is Exclusive:
            queueFamilyIndexCount: 0,
            pQueueFamilyIndices: null
        );

        vk.CreateBuffer(_renderer.Device, in createInfo, null, out _buffer)
            .AssertSuccess("Failed to create buffer");
        
        // Now put some actual memory behind that buffer.

        vk.GetBufferMemoryRequirements(_renderer.Device, _buffer, out MemoryRequirements requirements);

        // TODO: Should we always required host visible memory?
        MemoryPropertyFlags requiredProperties = MemoryPropertyFlags.DeviceLocalBit | MemoryPropertyFlags.HostVisibleBit;
        
        if (!_renderer.MemoryHelper.FindMemoryType(requirements.MemoryTypeBits, requiredProperties, out uint memoryTypeIndex)) {
            throw new InvalidOperationException("Failed to find a suitable memory type for allocation");
        }
        
        // Align up to match non-coherent atom size.
        // TODO: Does GetBufferMemoryRequirements already take the non-coherent atom size into account for size?
        ulong allocationSize = AlignUtil.AlignUp(requirements.Size, _renderer.PhysicalDeviceLimits.NonCoherentAtomSize);
        
        // TODO: WE CAN'T MAKE AN ALLOCATION FOR EVERY BUFFER.
        
        MemoryAllocateInfo allocateInfo = new(
            sType: StructureType.MemoryAllocateInfo,
            pNext: null,
            allocationSize: allocationSize,
            memoryTypeIndex: memoryTypeIndex
        );
        vk.AllocateMemory(_renderer.Device, in allocateInfo, null, out _memory)
            .AssertSuccess("Failed to allocate memory for buffer");
        
        vk.BindBufferMemory(_renderer.Device, _buffer, _memory, 0)
            .AssertSuccess("Failed to bind buffer memory");
        
        _count = elementCount;
    }

    protected override void SetImpl(uint offset, ReadOnlySpan<T> elements, uint count)
    {
        Vk vk = _renderer.Vk;

        unsafe {

            ulong atomSize = _renderer.PhysicalDeviceLimits.NonCoherentAtomSize;
            ulong unalignedStart = offset * (uint)sizeof(T);
            ulong unalignedEnd = unalignedStart + count * (uint)sizeof(T);
            ulong alignedStart = AlignUtil.AlignDown(unalignedStart, atomSize);
            ulong alignedEnd = AlignUtil.AlignUp(unalignedEnd, atomSize);
            ulong mappingOffset = unalignedStart - alignedStart;
            ulong mappingSize = alignedEnd - alignedStart;
            
            void* mapped = null;
            vk.MapMemory(_renderer.Device, _memory, alignedStart, mappingSize, MemoryMapFlags.None, ref mapped)
                .AssertSuccess("Failed to map memory");
            try {
                // Copy the elements
                fixed (T* pElements = elements) {
                    System.Buffer.MemoryCopy(pElements, (byte*)mapped + mappingOffset, mappingSize - mappingOffset, (uint)elements.Length * (uint)sizeof(T));    
                }
                
                // Flush the writen memory.
                MappedMemoryRange range = new(
                    sType: StructureType.MappedMemoryRange,
                    pNext: null,
                    memory: _memory,
                    offset: alignedStart,
                    size: mappingSize
                );
                vk.FlushMappedMemoryRanges(_renderer.Device, 1, in range)
                    .AssertSuccess("Failed to flush memory");
            } finally {
                vk.UnmapMemory(_renderer.Device, _memory);
            }
        }
        
    }

    protected override void SetDirectImpl(IStaticBuffer<T>.DirectSetter setter)
    {
        Vk vk = _renderer.Vk;

        unsafe {

            ulong atomSize = _renderer.PhysicalDeviceLimits.NonCoherentAtomSize;
            ulong unalignedEnd = _count * (uint)sizeof(T);
            ulong alignedEnd = AlignUtil.AlignUp(unalignedEnd, atomSize);
            
            void* mapped = null;
            vk.MapMemory(_renderer.Device, _memory, 0, alignedEnd, MemoryMapFlags.None, ref mapped)
                .AssertSuccess("Failed to map memory");
            try {
                // Operate on the mapped memory.
                setter(new Span<T>(mapped, (int)_count));
                
                // Flush the writen memory.
                MappedMemoryRange range = new(
                    sType: StructureType.MappedMemoryRange,
                    pNext: null,
                    memory: _memory,
                    offset: 0,
                    size: alignedEnd
                );
                vk.FlushMappedMemoryRanges(_renderer.Device, 1, in range)
                    .AssertSuccess("Failed to flush memory");
            } finally {
                vk.UnmapMemory(_renderer.Device, _memory);
            }
        }
    }

    public VkBuffer GetBufferForCurrentFrame()
    {
        return _buffer;
    }

    public void PrepareToUpdateExternally()
    {
        throw new NotImplementedException();
    }
}