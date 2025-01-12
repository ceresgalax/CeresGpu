using Silk.NET.Vulkan;

namespace CeresGpu.Graphics.Vulkan;

public interface IVulkanRenderTarget
{
    bool IsBufferedByWorkingFrame { get; }
    
    int ImageViewIndexForCurrentFrame { get; }
    ImageView GetImageView(int index);
}