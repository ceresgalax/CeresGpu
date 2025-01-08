using System;
using System.Runtime.InteropServices;
using Silk.NET.Vulkan;

namespace CeresGpu.Graphics.Vulkan;

public sealed class VulkanTexture : IVulkanTexture, ITexture
{
    private readonly VulkanRenderer _renderer;
    private IntPtr _weakHandle;

    private Image _image;
    private ImageView _imageView;

    public VulkanTexture(VulkanRenderer renderer)
    {
        _renderer = renderer;
        _weakHandle = GCHandle.ToIntPtr(GCHandle.Alloc(this, GCHandleType.Weak));
        
    }
    
    private void ReleaseUnmanagedResources()
    {
        // TODO release unmanaged resources here
    }

    public void Dispose()
    {
        ReleaseUnmanagedResources();
        GC.SuppressFinalize(this);
    }

    ~VulkanTexture()
    {
        ReleaseUnmanagedResources();
    }
    
    public uint Width { get; private set; }
    public uint Height { get; private set; }
    public IntPtr WeakHandle => _weakHandle;
    
    public unsafe void Set(ReadOnlySpan<byte> data, uint width, uint height, ColorFormat format)
    {
        Width = width;
        Height = height;
        
        // TODO: CeresGpu needs to ensure that all ITexture impls are static and cannot be written if encoded in pending commands.
        // TODO: Make sure we destroy the previous image and view, or re-use.

        ImageCreateInfo createInfo = new(
            sType: StructureType.ImageCreateInfo,
            pNext: null,
            flags: ImageCreateFlags.None,
            imageType: ImageType.Type2D,
            format: format.ToVkFormat(),
            extent: new(width: width, height: height, depth: 1),
            mipLevels: 1,
            arrayLayers: 1,
            samples: SampleCountFlags.Count1Bit,
            tiling: ImageTiling.Optimal,
            usage: ImageUsageFlags.TransferDstBit | ImageUsageFlags.SampledBit,
            sharingMode: SharingMode.Exclusive,
            // Don't specify queue family since we are SharingMode.Exclusive
            queueFamilyIndexCount: 0,
            pQueueFamilyIndices: null,
            initialLayout: ImageLayout.Undefined // Start as undefined, we'll transition first and then copy into the image from our staging buffer.
            // (Remember: initial layout can only be Undefined or PreInitialized, we can't start out as TransferDstOptimal.)
        );
        _renderer.Vk.CreateImage(_renderer.Device, in createInfo, null, out _image)
            .AssertSuccess("Failed to create image");
        
        // Get some memory to put behind that image

        _renderer.Vk.GetImageMemoryRequirements(_renderer.Device, _image, out MemoryRequirements memoryRequirements);
        if (!_renderer.MemoryHelper.FindMemoryType(memoryRequirements.MemoryTypeBits, MemoryPropertyFlags.DeviceLocalBit, out uint memoryTypeIndex)) {
            throw new InvalidOperationException("Failed to find memory type for image");
        }

        MemoryAllocateInfo allocateInfo = new MemoryAllocateInfo(
            sType: StructureType.MemoryAllocateInfo,
            pNext: null,
            allocationSize: memoryRequirements.Size,
            memoryTypeIndex: memoryTypeIndex
        );
        _renderer.Vk.AllocateMemory(_renderer.Device, in allocateInfo, null, out DeviceMemory memory)
            .AssertSuccess("Failed to allocate memory for image");
        
        _renderer.Vk.BindImageMemory(_renderer.Device, _image, memory, 0)
            .AssertSuccess("Failed to bind memory to image");

        // Make a staging buffer 

        VulkanStaticBuffer<byte> stagingBuffer = new VulkanStaticBuffer<byte>(_renderer);
        stagingBuffer.AllocateWithUsage((uint)data.Length, BufferUsageFlags.TransferSrcBit);
        stagingBuffer.Set(data);
        
        //
        // First, memory barrier to transition the image to be transfered into.
        //

        ImageMemoryBarrier toTransferTransitionBarrier = new(
            sType: StructureType.ImageMemoryBarrier,
            pNext: null,

            // We don't need to wait for any memory to become visible before the barrier, the layout transition
            // doesn't read any memory, since we're transitioning from undefined.
            srcAccessMask: AccessFlags.None,

            // We need to make the layout transition available to the transfer we're about to do.
            dstAccessMask: AccessFlags.TransferWriteBit,

            oldLayout: ImageLayout.Undefined,
            newLayout: ImageLayout.TransferDstOptimal,

            // We're not transfering between queue families.
            srcQueueFamilyIndex: Vk.QueueFamilyIgnored,
            dstQueueFamilyIndex: Vk.QueueFamilyIgnored,

            image: _image,
            subresourceRange: new ImageSubresourceRange(
                aspectMask: ImageAspectFlags.ColorBit,
                baseMipLevel: 0,
                levelCount: 1,
                baseArrayLayer: 0,
                layerCount: 1
            )
        );
        _renderer.Vk.CmdPipelineBarrier(
            commandBuffer: _renderer.PreFrameCommandBuffer,
            srcStageMask: PipelineStageFlags.TopOfPipeBit, // Not waiting for any stages.
            dstStageMask: PipelineStageFlags.TransferBit, // Transfer stages must wait until this barrier is done transitioning the image layout.
            dependencyFlags: DependencyFlags.None,
            memoryBarrierCount: 0,
            pMemoryBarriers: null,
            bufferMemoryBarrierCount: 0,
            pBufferMemoryBarriers: null,
            imageMemoryBarrierCount: 1,
            pImageMemoryBarriers: in toTransferTransitionBarrier
        );
        
        //
        // Second, transfer from the staging buffer into the image
        //
        BufferImageCopy region = new(
            bufferOffset: 0,
            bufferRowLength: 0, // tightly packed, no stride between.
            bufferImageHeight: 0,
            imageSubresource: new ImageSubresourceLayers(
                aspectMask: ImageAspectFlags.ColorBit,
                mipLevel: 0,
                baseArrayLayer: 0,
                layerCount: 1
            ),
            imageOffset: new Offset3D(0, 0, 0),
            imageExtent: new Extent3D(width, height, 1)
        );
        _renderer.Vk.CmdCopyBufferToImage(
            commandBuffer: _renderer.PreFrameCommandBuffer,
            srcBuffer: stagingBuffer.Buffer,
            dstImage: _image,
            dstImageLayout: ImageLayout.TransferDstOptimal, // The layout the image is expected to be in when this command executes.
            regionCount: 1,
            pRegions: in region
        );
        
        //
        // Finally, transition the image into the final layout.
        //
        ImageMemoryBarrier toFinalLayoutTransitionBarrier = new(
            sType: StructureType.ImageMemoryBarrier,
            pNext: null,

            // Make the transfer done before this barrier visible.
            srcAccessMask: AccessFlags.TransferWriteBit,

            // Make the transfer available to shader reads.
            dstAccessMask: AccessFlags.ShaderReadBit,

            oldLayout: ImageLayout.TransferDstOptimal,
            newLayout: ImageLayout.ShaderReadOnlyOptimal,

            // We're not transfering between queue families.
            srcQueueFamilyIndex: Vk.QueueFamilyIgnored,
            dstQueueFamilyIndex: Vk.QueueFamilyIgnored,

            image: _image,
            subresourceRange: new ImageSubresourceRange(
                aspectMask: ImageAspectFlags.ColorBit,
                baseMipLevel: 0,
                levelCount: 1,
                baseArrayLayer: 0,
                layerCount: 1
            )
        );
        _renderer.Vk.CmdPipelineBarrier(
            commandBuffer: _renderer.PreFrameCommandBuffer,
            srcStageMask: PipelineStageFlags.TransferBit, // Barrier blocks until prior transfer commands are finished.
            dstStageMask: PipelineStageFlags.FragmentShaderBit, // Barrier blocks fragment shader execution. 
            dependencyFlags: DependencyFlags.None,
            memoryBarrierCount: 0,
            pMemoryBarriers: null,
            bufferMemoryBarrierCount: 0,
            pBufferMemoryBarriers: null,
            imageMemoryBarrierCount: 1,
            pImageMemoryBarriers: in toFinalLayoutTransitionBarrier
        );
        
        // Schedule staging buffer to be disposed once commands have finished executing.
        // Dispose will schedule a deferred deletion of the underlying buffers for after the command buffer the buffer
        // was encoded in was executed.
        stagingBuffer.Dispose();
        
        //
        // Create an image view to be used by descriptor sets.
        //
        ImageViewCreateInfo imageViewCreateInfo = new(
            sType: StructureType.ImageViewCreateInfo,
            pNext: null,
            flags: ImageViewCreateFlags.None,
            image: _image,
            viewType: ImageViewType.Type2D,
            format: format.ToVkFormat(),
            components: new ComponentMapping(),
            subresourceRange: new ImageSubresourceRange(
                aspectMask: ImageAspectFlags.ColorBit,
                baseMipLevel: 0,
                levelCount: 1,
                baseArrayLayer: 0,
                layerCount: 1
            )
        );
        _renderer.Vk.CreateImageView(_renderer.Device, in imageViewCreateInfo, null, out _imageView);
    }
    
    public ImageView GetImageView()
    {
        return _imageView;
    }
}