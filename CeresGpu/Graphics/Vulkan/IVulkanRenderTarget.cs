using Silk.NET.Vulkan;

namespace CeresGpu.Graphics.Vulkan;

public interface IVulkanRenderTarget
{
    ImageView GetImageViewForWorkingFrame();
}