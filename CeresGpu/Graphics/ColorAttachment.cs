using System.Numerics;

namespace CeresGpu.Graphics;

// TODO: This is probably going away in favor of IRenderPass!?!?!
public struct ColorAttachment_X
{
    /// <summary>
    /// If true, the swapchain framebuffer will be used instead of <see cref="Texture"/>.
    /// </summary>
    public bool UseSwapchainFramebuffer;
    public ITexture? Texture;
    public LoadAction LoadAction;
    public Vector4 ClearColor;
}