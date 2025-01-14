using CeresGL;

namespace CeresGpu.Graphics.OpenGL;

public sealed class GLSwapchainTarget : IGLRenderTarget, IRenderTarget
{
    public GLRenderBuffer? InnerBuffer;

    public bool MatchesSwapchainSize => true;
    public uint Width => InnerBuffer!.Width;
    public uint Height => InnerBuffer!.Height;
    public bool IsColor => true;
    public ColorFormat ColorFormat => InnerBuffer!.ColorFormat;
    public DepthStencilFormat DepthStencilFormat => InnerBuffer!.DepthStencilFormat;
    
    public void BindToFramebuffer(GL gl, uint framebufferHandle, FramebufferAttachment attachmentPoint)
    {
        InnerBuffer!.BindToFramebuffer(gl, framebufferHandle, attachmentPoint);
    }
    
    public void Dispose()
    {
        // We don't own the inner buffer, the GL Renderer does.
    }
}