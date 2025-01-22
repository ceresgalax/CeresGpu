using CeresGL;

namespace CeresGpu.Graphics.OpenGL;

public interface IGLRenderTarget
{
    ColorFormat ColorFormat { get; }
    DepthStencilFormat DepthStencilFormat { get; }
    void BindToFramebuffer(GL gl, uint framebufferHandle, FramebufferAttachment attachmentPoint);

}