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

public struct FramebufferAttachments
{
    
}

public interface IRenderPass : IDisposable
{
    IFramebuffer Framebuffer { get; }
}

public interface IFramebuffer
{
    bool IsSetup { get; }
}

/// <summary>
/// Create an instance from <see cref="IRenderer"/>
/// Will typically only be used inside of an implementation of IRenderPass, as the render pass implementation will know
/// how to where to set up each attachment in the framebuffer.
/// </summary>
public interface IMutableFramebuffer : IFramebuffer, IDisposable
    //where TRenderPass : IRenderPass
{
    void Setup(uint width, uint height);
    void SetColorAttachment(int index, IRenderTarget target, Vector4 clearColor);
    void SetDepthStencilAttachment(IRenderTarget depthStencil, double clearDepth, uint clearStencil);
}