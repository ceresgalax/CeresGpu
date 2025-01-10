using System;

namespace CeresGpu.Graphics;

public interface IRenderTarget : ISampleable, IDisposable
{
    uint Width { get; }
    uint Height { get; }
    
    // TODO: Use an enum here instead?
    /// <summary>
    /// If true, this is a color format render target. Otherwise, depth-stencil format.
    /// </summary>
    bool IsColor { get; }
    
    ColorFormat ColorFormat { get; }
    DepthStencilFormat DepthStencilFormat { get; }
    
}