using System;
using Silk.NET.Vulkan;

namespace CeresGpu.Graphics.Vulkan;

public sealed class VulkanSwapchainRenderTarget : IVulkanRenderTarget, IRenderTarget
{
    public uint Width => _extent.Width;
    public uint Height => _extent.Height;

    public bool IsColor => true;
    public ColorFormat ColorFormat { get; }
    public DepthStencilFormat DepthStencilFormat => default;

    private readonly Extent2D _extent;
    private readonly Image[] _images;
    private readonly ImageView[] _imageViews;
    
    public unsafe VulkanSwapchainRenderTarget(VulkanRenderer renderer, SwapchainKHR swapchain, Extent2D extent, SurfaceFormatKHR surfaceFormat)
    {
        _extent = extent;
        surfaceFormat.Format.ToColorFormat(out ColorFormat colorFormat);
        ColorFormat = colorFormat;
        
        _images = new Image[renderer.FrameCount];
        uint numSwapchainImages = (uint)_images.Length;
        fixed (Image* pImages = _images) {
            renderer.VkKhrSwapchain.GetSwapchainImages(renderer.Device, swapchain, ref numSwapchainImages, pImages)
                .AssertSuccess("Failed to get swapchain images");
        }
        if (numSwapchainImages != (uint)_images.Length) {
            throw new InvalidOperationException("Got unexpected number of swapchain images");
        }
        
        //
        // Create image views
        //
        _imageViews = new ImageView[_images.Length];
        for (int i = 0; i < _images.Length; ++i) {
            ImageViewCreateInfo imageViewCreateInfo = new(
                sType: StructureType.ImageViewCreateInfo,
                pNext: null,
                flags: ImageViewCreateFlags.None,
                image: _images[i],
                ImageViewType.Type2D,
                format: surfaceFormat.Format,
                components: new ComponentMapping(),
                subresourceRange: new ImageSubresourceRange(
                    aspectMask: ImageAspectFlags.ColorBit,
                    baseMipLevel: 0,
                    levelCount: 1,
                    baseArrayLayer: 0,
                    layerCount: 1
                )
            );
            renderer.Vk.CreateImageView(renderer.Device, in imageViewCreateInfo, null, out _imageViews[i])
                .AssertSuccess("Failed to create image view");
        }
        
        // // Encode transitions to our desired initial layout in the pre-frame command buffer, to be executed first thing this frame.
        //
        // ImageMemoryBarrier[] barriers = new ImageMemoryBarrier[_images.Length];
        // for (int i = 0; i < _images.Length; ++i) {
        //     barriers[i] = new ImageMemoryBarrier(
        //         sType: StructureType.ImageMemoryBarrier,
        //         pNext: null,
        //         srcAccessMask: AccessFlags.None, // don't need prior writes to be visible, we don't read any memory in the transition.
        //         dstAccessMask: AccessFlags.ShaderReadBit,
        //         oldLayout: ImageLayout.Undefined,
        //         newLayout: ImageLayout.ColorAttachmentOptimal,
        //         srcQueueFamilyIndex: Vk.QueueFamilyIgnored,
        //         dstQueueFamilyIndex: Vk.QueueFamilyIgnored,
        //         image: _images[i],
        //         subresourceRange: new ImageSubresourceRange(
        //             aspectMask: ImageAspectFlags.ColorBit,
        //             baseMipLevel: 0,
        //             levelCount: 1,
        //             baseArrayLayer: 0,
        //             layerCount: 1
        //         )
        //     );
        // }
        // fixed (ImageMemoryBarrier* pImageBarriers = barriers) {
        //     renderer.Vk.CmdPipelineBarrier(
        //         commandBuffer: renderer.PreFrameCommandBuffer,
        //         srcStageMask: PipelineStageFlags.TopOfPipeBit,
        //         dstStageMask: PipelineStageFlags.FragmentShaderBit,
        //         dependencyFlags: DependencyFlags.None,
        //         memoryBarrierCount: 0,
        //         pMemoryBarriers: null,
        //         bufferMemoryBarrierCount: 0,
        //         pBufferMemoryBarriers: null,
        //         imageMemoryBarrierCount: (uint)barriers.Length,
        //         pImageMemoryBarriers: pImageBarriers
        //     );
        // }
        
    }

    public ImageView GetImageViewForWorkingFrame(int workingFrame)
    {
        return _imageViews[workingFrame];
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

    ~VulkanSwapchainRenderTarget()
    {
        ReleaseUnmanagedResources();
    }
}