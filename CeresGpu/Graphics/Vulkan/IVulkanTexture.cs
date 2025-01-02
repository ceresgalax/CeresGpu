using Silk.NET.Vulkan;

namespace CeresGpu.Graphics.Vulkan;

public interface IVulkanTexture
{
    /// <summary>
    /// Return the image view that can be used access subresources necesary for using this texture's image as a render pass attachment.
    /// </summary>
    ImageView GetFramebufferView();
}