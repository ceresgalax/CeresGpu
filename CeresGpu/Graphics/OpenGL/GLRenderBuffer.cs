using System;
using CeresGL;

namespace CeresGpu.Graphics.OpenGL;

public sealed class GLRenderBuffer : IGLRenderTarget, IRenderTarget
{
    private readonly GLRenderer _renderer;

    private readonly uint _renderbufferHandle;

    public bool MatchesSwapchainSize => false;
    public uint Width { get; }
    public uint Height { get; }
    public bool IsColor { get; }
    public ColorFormat ColorFormat { get; }
    public DepthStencilFormat DepthStencilFormat { get; }

    public GLRenderBuffer(GLRenderer renderer, bool isColorBuffer, ColorFormat colorFormat, DepthStencilFormat depthStencilFormat, uint width, uint height)
    {
        _renderer = renderer;

        GL gl = renderer.GLProvider.Gl;

        Span<uint> renderbuffers = [0];
        gl.GenRenderbuffers(1, renderbuffers);
        _renderbufferHandle = renderbuffers[0];
        
        gl.BindRenderbuffer(RenderbufferTarget.RENDERBUFFER, _renderbufferHandle);
        
        IsColor = isColorBuffer;
        Width = width;
        Height = height;
        ColorFormat = colorFormat;
        DepthStencilFormat = depthStencilFormat;

        InternalFormat internalFormat;
        if (isColorBuffer) {
            internalFormat = colorFormat.GetGLFormats().Item1;
        } else {
            internalFormat = depthStencilFormat.ToGLInternalFormat();
        }
        
        gl.RenderbufferStorage(RenderbufferTarget.RENDERBUFFER, internalFormat, (int)width, (int)height);
    }
    
    public void BindToFramebuffer(GL gl, uint framebufferHandle, FramebufferAttachment attachmentPoint)
    {
        gl.FramebufferRenderbuffer(FramebufferTarget.FRAMEBUFFER, attachmentPoint, RenderbufferTarget.RENDERBUFFER, _renderbufferHandle);
    }


    private void ReleaseUnmanagedResources()
    {
        _renderer.GLProvider.AddFinalizerAction(gl => {
            Span<uint> renderbuffers = [_renderbufferHandle];
            gl.DeleteRenderbuffers(1, renderbuffers);
        });
    }

    
    private bool _isDisposed;

    public void Dispose()
    {
        if (_isDisposed) {
            return;
        }
        _isDisposed = true;
        
        ReleaseUnmanagedResources();
        GC.SuppressFinalize(this);
    }

    ~GLRenderBuffer()
    {
        ReleaseUnmanagedResources();
    }
}