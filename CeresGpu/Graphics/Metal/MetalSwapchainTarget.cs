using System;

namespace CeresGpu.Graphics.Metal;

public sealed class MetalSwapchainTarget : IMetalRenderTarget, IRenderTarget
{
    public IntPtr Drawable { get; set; }

    public bool MatchesSwapchainSize => true;
    public uint Width { get; set; }
    public uint Height { get; set; }
    public bool IsColor => true;
    public ColorFormat ColorFormat { get; set; }
    public DepthStencilFormat DepthStencilFormat => default;
    
    public IntPtr GetCurrentFrameDrawable()
    {
        return Drawable;
    }
    
    public void Dispose()
    {
    }
}