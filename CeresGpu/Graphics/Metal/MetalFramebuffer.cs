using System;
using System.Numerics;

namespace CeresGpu.Graphics.Metal;

public sealed class MetalFramebuffer : IFramebuffer
{
    public record struct ColorAttachment(IMetalRenderTarget RenderTarget, Vector4 ClearColor);
    
    private readonly ColorAttachment[] _colorAttachments;
    public readonly IMetalRenderTarget? DepthStencilAttachment;
    
    public double DepthClearValue { get; private set; }
    public uint StencilClearValue { get; private set; }
    
    public uint Width { get; }
    public uint Height { get; }
    
    public ReadOnlySpan<ColorAttachment> ColorAttachments => _colorAttachments;
    
    public MetalFramebuffer(MetalPassBacking pass, ReadOnlySpan<IRenderTarget> colorAttachments, IRenderTarget? depthStencilAttachment)
    {
        FramebufferUtil.ValidateAttachments(in pass.Definition, colorAttachments, depthStencilAttachment, out uint width, out uint height);
        
        _colorAttachments = new ColorAttachment[pass.Definition.ColorAttachments.Length];
        
        for (int i = 0; i < _colorAttachments.Length; ++i) {
            if (colorAttachments[i] is not IMetalRenderTarget target) {
                throw new ArgumentOutOfRangeException(nameof(colorAttachments));
            }
            _colorAttachments[i].RenderTarget = target;
        }

        if (depthStencilAttachment != null) {
            if (depthStencilAttachment is not IMetalRenderTarget target) {
                throw new ArgumentException(nameof(depthStencilAttachment));
            }
            DepthStencilAttachment = target;
        }
        
        Width = width;
        Height = height;
    }
    
    public void SetColorAttachmentProperties(int index, Vector4 clearColor)
    {
        _colorAttachments[index].ClearColor = clearColor;
    }

    public void SetDepthStencilAttachmentProperties(double clearDepth, uint clearStencil)
    {
        DepthClearValue = clearDepth;
        StencilClearValue = clearStencil;
    }

    public void Dispose()
    {
    }
}