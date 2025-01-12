using System.Numerics;
using CeresGpu.Graphics;

namespace CeresGpuTestApp;

public sealed class FramebufferPass : IRenderPass
{
    private readonly IFramebuffer _framebuffer;

    public IFramebuffer Framebuffer => _framebuffer;

    public static void RegisterSelf(IRenderer renderer)
    {
        renderer.RegisterPassType<FramebufferPass>(new RenderPassDefinition {
            ColorAttachments = [
                new ColorAttachment {
                    Format = renderer.GetSwapchainColorTarget().ColorFormat, //ColorFormat.R8G8B8A8_UNORM,
                    LoadAction = LoadAction.Clear
                }
            ],
            // DepthStencilAttachment = new DepthStencilAttachment {
            //     Format = DepthStencilFormat.D32_SFLOAT,
            //     LoadAction = LoadAction.Clear
            // }
        });
    }
    
    public FramebufferPass(IRenderer renderer, IRenderTarget colorTarget)
    {
        _framebuffer = renderer.CreateFramebuffer<FramebufferPass>([colorTarget], null);
    }

    public void SetClearColor(Vector4 clearColor)
    {
        _framebuffer.SetColorAttachmentProperties(0, clearColor);
    }

    public void Dispose()
    {
        _framebuffer.Dispose();
    }
}
