using System;
using Silk.NET.Vulkan;
using VkBuffer = Silk.NET.Vulkan.Buffer;

namespace CeresGpu.Graphics.Vulkan;

sealed class DestroyBufferDeferable(VulkanRenderer renderer) : IDeferredDisposable
{
    public VkBuffer[]? Buffers;
    public DeviceMemory Memory;
    
    public unsafe void DeferredDispose()
    {
        if (Buffers != null) {
            foreach (VkBuffer buffer in Buffers) {
                renderer.Vk.DestroyBuffer(renderer.Device, buffer, null);    
            }
        }

        if (Memory.Handle != 0) {
            renderer.Vk.FreeMemory(renderer.Device, Memory, null);
        }
    }
}

public sealed class VulkanStreamingBuffer<T> : StreamingBuffer<T>, IVulkanBuffer
    where T : unmanaged
{
    private readonly VulkanRenderer _renderer;
    private VkBuffer[] _buffers;
    private DeviceMemory _memory;

    private ulong _perBufferMemoryUsage;

    private uint _elementCount;

    public override uint Count => _elementCount;
    
    protected override IRenderer Renderer => _renderer;
    
    public VulkanStreamingBuffer(VulkanRenderer renderer)
    {
        _renderer = renderer;
        _buffers = new VkBuffer[renderer.FrameCount];
    }

    protected override unsafe void AllocateImpl(uint elementCount)
    {
        _elementCount = elementCount;
        
        Vk vk = _renderer.Vk;
        
        // TODO: Re-use these defered dispose envelopes?
        DestroyBufferDeferable deferable = new DestroyBufferDeferable(_renderer) {
            Buffers = _buffers,
            Memory = _memory
        };
        _renderer.DeferDisposal(deferable);
        
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
        
        if (_elementCount > 0) {
            
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

            _buffers = new VkBuffer[_renderer.FrameCount];
            for (int i = 0; i < _renderer.FrameCount; ++i) {
                vk.CreateBuffer(_renderer.Device, in createInfo, null, out _buffers[i])
                    .AssertSuccess("Failed to create buffer");    
            }
        
            // Allocate memory to be used by the buffers.

            // NOTE: This assumes the buffers will all have the same memory requirements.
            vk.GetBufferMemoryRequirements(_renderer.Device, _buffers[0], out MemoryRequirements requirements);

            // TODO: Should we always require host visible memory?
            MemoryPropertyFlags requiredProperties =
                MemoryPropertyFlags.DeviceLocalBit | MemoryPropertyFlags.HostVisibleBit;

            if (!_renderer.MemoryHelper.FindMemoryType(requirements.MemoryTypeBits, requiredProperties,
                    out uint memoryTypeIndex)) {
                throw new InvalidOperationException("Failed to find a suitable memory type for allocation");
            }

            // Align up to match non-coherent atom size.
            _perBufferMemoryUsage = AlignUtil.AlignUp(requirements.Size, requirements.Alignment);
            ulong allocationSize = AlignUtil.AlignUp(_perBufferMemoryUsage * (uint)_buffers.Length,
                _renderer.PhysicalDeviceLimits.NonCoherentAtomSize);

            // TODO: WE CAN'T MAKE AN ALLOCATION FOR EVERY BUFFER.

            MemoryAllocateInfo allocateInfo = new(
                sType: StructureType.MemoryAllocateInfo,
                pNext: null,
                allocationSize: allocationSize,
                memoryTypeIndex: memoryTypeIndex
            );
            vk.AllocateMemory(_renderer.Device, in allocateInfo, null, out _memory)
                .AssertSuccess("Failed to allocate memory for buffer");

            for (int i = 0; i < _buffers.Length; ++i) {
                vk.BindBufferMemory(_renderer.Device, _buffers[i], _memory, _perBufferMemoryUsage * (uint)i)
                    .AssertSuccess("Failed to bind buffer memory");
            }
        }
    }

    private void ReleaseUnmanagedResources()
    {
        if (!_renderer.IsDisposed) {
            // TODO: Re-use these defered dispose envelopes?
            DestroyBufferDeferable deferable = new DestroyBufferDeferable(_renderer) {
                Buffers = _buffers,
                Memory = _memory
            };
            _renderer.DeferDisposal(deferable);
        }
    }
    
    public override void Dispose()
    {
        ReleaseUnmanagedResources();
        GC.SuppressFinalize(this);   
    }

    ~VulkanStreamingBuffer()
    {
        ReleaseUnmanagedResources();
    }

    public VkBuffer GetBufferForCurrentFrame()
    {
        return _buffers[_renderer.WorkingFrame];
    }

    protected override unsafe void SetImpl(uint offset, ReadOnlySpan<T> elements, uint count)
    {
        Vk vk = _renderer.Vk;

        if (count == 0) {
            return;
        }
        
        ulong atomSize = _renderer.PhysicalDeviceLimits.NonCoherentAtomSize;
        ulong unalignedStart = _perBufferMemoryUsage * (uint)_renderer.WorkingFrame + offset * (uint)sizeof(T);
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
                System.Buffer.MemoryCopy(pElements, (byte*)mapped + mappingOffset, mappingSize - mappingOffset, count * (uint)sizeof(T));    
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

    protected override void SetDirectImpl(IStreamingBuffer<T>.DirectSetter setter, uint count)
    {
        Vk vk = _renderer.Vk;

        unsafe {

            ulong atomSize = _renderer.PhysicalDeviceLimits.NonCoherentAtomSize;
            ulong unalignedEnd = _perBufferMemoryUsage * (uint)_renderer.FrameCount + count * (uint)sizeof(T);
            ulong alignedEnd = AlignUtil.AlignUp(unalignedEnd, atomSize);
            
            void* mapped = null;
            vk.MapMemory(_renderer.Device, _memory, 0, alignedEnd, MemoryMapFlags.None, ref mapped)
                .AssertSuccess("Failed to map memory");
            try {
                // Operate on the mapped memory.
                setter(new Span<T>(mapped, (int)count));
                
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

    void IVulkanBuffer.Commit()
    {
        Commit();
    }

    public void PrepareToUpdateExternally()
    {
        throw new System.NotImplementedException();
    }
}