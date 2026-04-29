using System;
using System.Numerics;

namespace CeresGpu.Graphics;

public struct ColorAttachment
{
    public ColorFormat Format;
    public LoadAction LoadAction;
}

public struct DepthStencilAttachment
{
    public DepthStencilFormat Format;
    public LoadAction LoadAction;
}

public struct RenderPassDefinition
{
    public ColorAttachment[] ColorAttachments;
    public DepthStencilAttachment? DepthStencilAttachment;
}

// TODO: Rename IRenderPass to IFramebuffer and IFramebuffer to IFramebufferBacking.
public interface IRenderPass : IDisposable
{
    IFramebuffer Framebuffer { get; }
}
