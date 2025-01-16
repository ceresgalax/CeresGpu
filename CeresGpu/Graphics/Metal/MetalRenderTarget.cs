using System;
using CeresGpu.MetalBinding;

namespace CeresGpu.Graphics.Metal;

public sealed class MetalRenderTarget : IMetalRenderTarget, IRenderTarget
{
    private readonly MetalRenderer _renderer;
    
    private readonly IntPtr[] _texturesByWorkingFrame;

    public bool MatchesSwapchainSize { get; }
    public uint Width { get; private set; }
    public uint Height { get; private set; }
    public bool IsColor { get; }
    public ColorFormat ColorFormat { get; }
    public DepthStencilFormat DepthStencilFormat { get; }

    public MetalRenderTarget(MetalRenderer renderer, bool isColor, ColorFormat colorFormat, DepthStencilFormat depthStencilFormat, bool matchesSwapchainSize, uint width, uint height)
    {
        _renderer = renderer;

        IsColor = isColor;
        MatchesSwapchainSize = matchesSwapchainSize;
        Width = width;
        Height = height;
        ColorFormat = colorFormat;
        DepthStencilFormat = depthStencilFormat;

        MetalApi.MTLPixelFormat pixelFormat = isColor ? colorFormat.ToMtlPixelFormat() : depthStencilFormat.ToMtlPixelFormat();

        _texturesByWorkingFrame = new IntPtr[renderer.FrameCount];
        for (int i = 0; i < _texturesByWorkingFrame.Length; i++) {
            _texturesByWorkingFrame[i] = MetalApi.metalbinding_new_texture(renderer.Context, width, height, pixelFormat);    
        }
    }
    
    public void HandleSwapchainResized(uint width, uint height)
    {
        if (!MatchesSwapchainSize) {
            throw new InvalidOperationException();
        }
        
        // TODO: Do we need to defer dispose? Or does metal keep reference counts in command buffers?
        foreach (IntPtr texture in _texturesByWorkingFrame) {
            MetalApi.metalbinding_release_texture(texture);
        }
        
        MetalApi.MTLPixelFormat pixelFormat = IsColor ? ColorFormat.ToMtlPixelFormat() : DepthStencilFormat.ToMtlPixelFormat();
        Width = width;
        Height = height;
        
        for (int i = 0; i < _texturesByWorkingFrame.Length; i++) {
            _texturesByWorkingFrame[i] = MetalApi.metalbinding_new_texture(_renderer.Context, width, height, pixelFormat);    
        }
    }
    
    
    public IntPtr GetCurrentFrameDrawable()
    {
        return _texturesByWorkingFrame[_renderer.WorkingFrame];
    }

    private void ReleaseUnmanagedResources()
    {
        // TODO: Do we need to defer dispose? Or does metal keep reference counts in command buffers?
        foreach (IntPtr texture in _texturesByWorkingFrame) {
            MetalApi.metalbinding_release_texture(texture);
        }   
    }

    public void Dispose()
    {
        ReleaseUnmanagedResources();
        GC.SuppressFinalize(this);
    }

    ~MetalRenderTarget()
    {
        ReleaseUnmanagedResources();
    }
    
}