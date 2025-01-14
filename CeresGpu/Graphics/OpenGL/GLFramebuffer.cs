using System;
using System.Numerics;
using CeresGL;

namespace CeresGpu.Graphics.OpenGL;

public sealed class GLFramebuffer : IFramebuffer
{
    public record struct ColorAttachment(IGLRenderTarget? RenderTarget, Vector4 ClearColor);

    private readonly GLRenderer _renderer;
    
    private readonly ColorAttachment[] _colorAttachments;
    public readonly IGLRenderTarget? DepthStencilAttachment;
    
    public double DepthClearValue { get; private set; }
    public uint StencilClearValue { get; private set; }
    
    public uint Width { get; }
    public uint Height { get; }
    
    public ReadOnlySpan<ColorAttachment> ColorAttachments => _colorAttachments;

    public readonly uint FramebufferHandle;
    
    public GLFramebuffer(GLRenderer renderer, GLPassBacking passBacking, ReadOnlySpan<IRenderTarget> colorAttachments, IRenderTarget? depthStencilAttachment)
    {
        _renderer = renderer;
        
        FramebufferUtil.ValidateAttachments(in passBacking.Definition, colorAttachments, depthStencilAttachment, out uint width, out uint height);
        
        _colorAttachments = new ColorAttachment[passBacking.Definition.ColorAttachments.Length];
        
        for (int i = 0; i < _colorAttachments.Length; ++i) {
            if (colorAttachments[i] is not IGLRenderTarget vulkanRenderTarget) {
                throw new ArgumentOutOfRangeException(nameof(colorAttachments));
            }
            _colorAttachments[i].RenderTarget = vulkanRenderTarget;
        }

        if (depthStencilAttachment != null) {
            if (depthStencilAttachment is not IGLRenderTarget vulkanRenderTarget) {
                throw new ArgumentException(nameof(depthStencilAttachment));
            }
            DepthStencilAttachment = vulkanRenderTarget;
        }

        GL gl = renderer.GLProvider.Gl;
        Span<uint> framebuffers = stackalloc uint[1];
        gl.GenFramebuffers(1, framebuffers);
        FramebufferHandle = framebuffers[0];
        
        gl.BindFramebuffer(FramebufferTarget.FRAMEBUFFER, FramebufferHandle);

        for (int i = 0; i < _colorAttachments.Length; ++i) {
            if (_colorAttachments[i].RenderTarget is not IGLRenderTarget glTarget) {
                throw new InvalidOperationException();
            }
            glTarget.BindToFramebuffer(gl, FramebufferHandle, FramebufferAttachment.COLOR_ATTACHMENT0 + (uint)i);
        }

        if (DepthStencilAttachment != null) {
            if (DepthStencilAttachment is not IGLRenderTarget glTarget) {
                throw new InvalidOperationException();
            }
            glTarget.BindToFramebuffer(gl, FramebufferHandle, FramebufferAttachment.DEPTH_STENCIL_ATTACHMENT);
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

    private void ReleaseUnmanagedResources()
    {
        _renderer.GLProvider.AddFinalizerAction(gl => {
            Span<uint> framebuffers = [FramebufferHandle];
            gl.DeleteFramebuffers(1, framebuffers);
        });
    }

    private bool _disposed;
    
    public void Dispose()
    {
        if (_disposed) {
            _disposed = true;
            return;
        }
        ReleaseUnmanagedResources();
        GC.SuppressFinalize(this);
    }

    ~GLFramebuffer()
    {
        ReleaseUnmanagedResources();
    }
}