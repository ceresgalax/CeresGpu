using System.Numerics;
using CeresGpu.Graphics;

namespace CeresGpuTestApp;

public sealed class FramebufferPass : IRenderPass
{
    private readonly IMutableFramebuffer _framebuffer;

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
    
    public FramebufferPass(IRenderer renderer)
    {
        _framebuffer = renderer.CreateFramebuffer<FramebufferPass>();
    }

    public void Setup(IRenderTarget colorTarget, Vector4 clearColor)
    {
        _framebuffer.Setup(colorTarget.Width, colorTarget.Height);
        _framebuffer.SetColorAttachment(0, colorTarget, clearColor);
    }

    public void Dispose()
    {
        _framebuffer.Dispose();
    }
}