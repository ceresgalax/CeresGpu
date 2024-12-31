using System;
using System.Numerics;

namespace CeresGpu.Graphics;

public struct ColorAttachment
{
    public InputFormat Format;
    public LoadAction LoadAction;
    public Vector4 ClearColor;
}

public struct DepthStencilAttachment
{
    public LoadAction LoadAction;
}

public interface IRenderPass
{
    public ReadOnlySpan<ColorAttachment> ColorAttachments { get; }
    public ReadOnlySpan<DepthStencilAttachment> DepthStencilAttachments { get; }
}