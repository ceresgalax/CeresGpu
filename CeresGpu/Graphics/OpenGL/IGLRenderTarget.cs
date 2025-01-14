using CeresGL;

namespace CeresGpu.Graphics.OpenGL;

public interface IGLRenderTarget
{
    void BindToFramebuffer(GL gl, uint framebufferHandle, FramebufferAttachment attachmentPoint);

}