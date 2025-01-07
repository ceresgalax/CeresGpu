using Silk.NET.Vulkan;

namespace CeresGpu.Graphics.Vulkan;

public interface IVulkanTexture
{
    
    ImageView GetImageView();
}