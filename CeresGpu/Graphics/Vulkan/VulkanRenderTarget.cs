using System;
using Silk.NET.Vulkan;

namespace CeresGpu.Graphics.Vulkan;

/// <summary>
/// * Render targets are working-frame buffered so that images can be written to for each working frame.
/// * Render targets assume a SHADER_SAMPLE_OPTIMAL layout.
///     * When used as a pass attachment:
///         * The image is transitioned to ATTACHMENT_OPTIMAL layout at the beginning of the pass.
///         * The image is transitioned to SHADER_SAMPLE_OPTIMAL layout at the end of the pass's buffer, with pipeline
///           barrier to make sure render target can be used as a sample-able texture. 
/// </summary>
public sealed class VulkanRenderTarget : IVulkanRenderTarget, IRenderTarget
{
    private readonly VulkanRenderer _renderer;

    private readonly Image[] ImageByWorkingFrame;
    private readonly ImageView[] ImageViewByWorkingFrame;
    
    public uint Width { get; }
    public uint Height { get; }
    
    public bool IsColor { get; }
    public ColorFormat ColorFormat { get; }
    public DepthStencilFormat DepthStencilFormat { get; }
    
    public unsafe VulkanRenderTarget(VulkanRenderer renderer, bool isColor, ColorFormat colorFormat, DepthStencilFormat depthStencilFormat, uint width, uint height, ImageUsageFlags usage, ImageAspectFlags aspectMask)
    {
        _renderer = renderer;
        ImageByWorkingFrame = new Image[renderer.FrameCount];
        
        Width = width;
        Height = height;
        IsColor = isColor;
        ColorFormat = colorFormat;
        DepthStencilFormat = depthStencilFormat;

        Format format = colorFormat.ToVkFormat();
        
        // Create images for each working frame!

        ImageCreateInfo createInfo = new ImageCreateInfo(
            sType: StructureType.ImageCreateInfo,
            pNext: null,
            flags: ImageCreateFlags.None,
            imageType: ImageType.Type2D,
            format: format,
            extent: new Extent3D(width, height, 1),
            mipLevels: 1,
            arrayLayers: 1,
            samples: SampleCountFlags.Count1Bit,
            tiling: ImageTiling.Optimal,
            usage: usage,
            sharingMode: SharingMode.Exclusive,
            queueFamilyIndexCount: 0,
            pQueueFamilyIndices: null,
            initialLayout: ImageLayout.Undefined // Remember: Can only start as Undefined or PreInitialized.
        );
        // TODO: Instead of creating 3 different Images, can we use the same image with 3 layers for each resource frame?
        for (int i = 0; i < ImageByWorkingFrame.Length; ++i) {
            renderer.Vk.CreateImage(renderer.Device, in createInfo, null, out ImageByWorkingFrame[i])
                .AssertSuccess("Failed to create image");
        }

        // Allocate some memory for all three images
        
        // Note: We assume the images have the same requirements, since we use the same create info for all.
        renderer.Vk.GetImageMemoryRequirements(renderer.Device, ImageByWorkingFrame[0], out MemoryRequirements memoryRequirements);
        if (!renderer.MemoryHelper.FindMemoryType(memoryRequirements.MemoryTypeBits, MemoryPropertyFlags.DeviceLocalBit, out uint memoryTypeIndex)) {
            throw new InvalidOperationException("Failed to find memory type.");
        }
        
        ulong start = 0;
        ulong end = 0;

        for (int i = 0; i < ImageByWorkingFrame.Length; ++i) {
            start = AlignUtil.AlignUp(end, memoryRequirements.Alignment);
            end = start + memoryRequirements.Size;
        }
        
        MemoryAllocateInfo allocateInfo = new(
            sType: StructureType.MemoryAllocateInfo,
            pNext: null,
            allocationSize: end,
            memoryTypeIndex: memoryTypeIndex
        );
        renderer.Vk.AllocateMemory(renderer.Device, in allocateInfo, null, out DeviceMemory memory)
            .AssertSuccess("Failed to allocate memory");
        
        // Bind the images to the memory

        start = 0;
        end = 0;
        for (int i = 0; i < ImageByWorkingFrame.Length; ++i) {
            start = AlignUtil.AlignUp(end, memoryRequirements.Alignment);
            end = start + memoryRequirements.Size;
            renderer.Vk.BindImageMemory(renderer.Device, ImageByWorkingFrame[i], memory, start)
                .AssertSuccess("Failed to bind image memory");
        }

        // Encode transitions to our desired initial layout in the pre-frame command buffer, to be executed first thing this frame.

        ImageMemoryBarrier[] barriers = new ImageMemoryBarrier[ImageByWorkingFrame.Length];
        for (int i = 0; i < ImageByWorkingFrame.Length; ++i) {
            barriers[i] = new ImageMemoryBarrier(
                sType: StructureType.ImageMemoryBarrier,
                pNext: null,
                srcAccessMask: AccessFlags.None, // don't need prior writes to be visible, we don't read any memory in the transition.
                dstAccessMask: AccessFlags.ShaderReadBit, // We need the transition to be visisble to the shaders reading from these images.
                oldLayout: ImageLayout.Undefined,
                newLayout: ImageLayout.ColorAttachmentOptimal,
                srcQueueFamilyIndex: Vk.QueueFamilyIgnored,
                dstQueueFamilyIndex: Vk.QueueFamilyIgnored,
                image: ImageByWorkingFrame[i],
                subresourceRange: new ImageSubresourceRange(
                    aspectMask: aspectMask,
                    baseMipLevel: 0,
                    levelCount: 1,
                    baseArrayLayer: 0,
                    layerCount: 1
                )
            );
        }
        fixed (ImageMemoryBarrier* pImageBarriers = barriers) {
            renderer.Vk.CmdPipelineBarrier(
                commandBuffer: renderer.PreFrameCommandBuffer,
                srcStageMask: PipelineStageFlags.TopOfPipeBit,
                dstStageMask: PipelineStageFlags.FragmentShaderBit,
                dependencyFlags: DependencyFlags.None,
                memoryBarrierCount: 0,
                pMemoryBarriers: null,
                bufferMemoryBarrierCount: 0,
                pBufferMemoryBarriers: null,
                imageMemoryBarrierCount: (uint)barriers.Length,
                pImageMemoryBarriers: pImageBarriers
            );
        }
        
        //
        // Create image views
        //
        ImageViewByWorkingFrame = new ImageView[ImageByWorkingFrame.Length];
        for (int i = 0; i < ImageViewByWorkingFrame.Length; ++i) {
            ImageViewCreateInfo imageViewCreateInfo = new(
                sType: StructureType.ImageViewCreateInfo,
                pNext: null,
                flags: ImageViewCreateFlags.None,
                image: ImageByWorkingFrame[i],
                ImageViewType.Type2D,
                format: format,
                components: new ComponentMapping(),
                subresourceRange: new ImageSubresourceRange(
                    aspectMask: aspectMask,
                    baseMipLevel: 0,
                    levelCount: 1,
                    baseArrayLayer: 0,
                    layerCount: 1
                )
            );
            renderer.Vk.CreateImageView(renderer.Device, in imageViewCreateInfo, null, out ImageViewByWorkingFrame[i])
                .AssertSuccess("Failed to create image view");
        }
    }

    public ImageView GetImageViewForWorkingFrame()
    {
        return ImageViewByWorkingFrame[_renderer.WorkingFrame];
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

    ~VulkanRenderTarget()
    {
        ReleaseUnmanagedResources();
    }
}