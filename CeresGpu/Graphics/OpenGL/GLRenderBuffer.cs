using System;
using CeresGL;

namespace CeresGpu.Graphics.OpenGL;

public sealed class GLRenderBuffer : IGLRenderTarget, IRenderTarget
{
    private readonly GLRenderer _renderer;

    private readonly uint _renderbufferHandle;

    public bool MatchesSwapchainSize { get; }
    public uint Width { get; private set; }
    public uint Height { get; private set; }
    public bool IsColor { get; }
    public ColorFormat ColorFormat { get; }
    public DepthStencilFormat DepthStencilFormat { get; }

    public GLRenderBuffer(GLRenderer renderer, bool isColorBuffer, ColorFormat colorFormat, DepthStencilFormat depthStencilFormat, bool matchesSwapchainSize, uint width, uint height)
    {
        _renderer = renderer;

        GL gl = renderer.GLProvider.Gl;

        Span<uint> renderbuffers = [0];
        gl.GenRenderbuffers(1, renderbuffers);
        _renderbufferHandle = renderbuffers[0];

        MatchesSwapchainSize = matchesSwapchainSize;
        IsColor = isColorBuffer;
        ColorFormat = colorFormat;
        DepthStencilFormat = depthStencilFormat;

        Resize(width, height);
    }
    
    public void BindToFramebuffer(GL gl, uint framebufferHandle, FramebufferAttachment attachmentPoint)
    {
        gl.FramebufferRenderbuffer(FramebufferTarget.FRAMEBUFFER, attachmentPoint, RenderbufferTarget.RENDERBUFFER, _renderbufferHandle);
    }

    public void Resize(uint width, uint height)
    {
        Width = width;
        Height = height;
        
        InternalFormat internalFormat;
        if (IsColor) {
            internalFormat = ColorFormat.GetGLFormats().Item1;
        } else {
            internalFormat = DepthStencilFormat.ToGLInternalFormat();
        }

        GL gl = _renderer.GLProvider.Gl;
        gl.BindRenderbuffer(RenderbufferTarget.RENDERBUFFER, _renderbufferHandle);
        gl.RenderbufferStorage(RenderbufferTarget.RENDERBUFFER, internalFormat, (int)width, (int)height);
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