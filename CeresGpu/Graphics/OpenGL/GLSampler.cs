using System;
using CeresGL;

namespace CeresGpu.Graphics.OpenGL;

public sealed class GLSampler : ISampler
{
    private readonly IGLProvider _glProvider;
    private uint _handle;

    public uint Handle => _handle;
    
    public GLSampler(IGLProvider glProvider, in SamplerDescription description)
    {
        _glProvider = glProvider;
        GL gl = glProvider.Gl;
        Span<uint> samplers = stackalloc uint[1];
        gl.GenSamplers(1, samplers);
        _handle = samplers[0];
        
        gl.SamplerParameteri(_handle, SamplerParameterI.TEXTURE_MIN_FILTER, (int)TranslateMinFilter(description.MinFilter));
        gl.SamplerParameteri(_handle, SamplerParameterI.TEXTURE_MAG_FILTER, (int)TranslateMagFilter(description.MagFilter));
        gl.SamplerParameteri(_handle, SamplerParameterI.TEXTURE_WRAP_R, (int)TranslateAddressMode(description.DepthAddressMode));
        gl.SamplerParameteri(_handle, SamplerParameterI.TEXTURE_WRAP_S, (int)TranslateAddressMode(description.WidthAddressMode));
        gl.SamplerParameteri(_handle, SamplerParameterI.TEXTURE_WRAP_T, (int)TranslateAddressMode(description.HeightAddressMode));
    }
    
    private static TextureMinFilter TranslateMinFilter(MinMagFilter filter)
    {
        return filter switch {
            MinMagFilter.Nearest => TextureMinFilter.NEAREST
            , MinMagFilter.Linear => TextureMinFilter.LINEAR
            , _ => throw new ArgumentOutOfRangeException(nameof(filter), filter, null)
        };
    }
        
    private static TextureMagFilter TranslateMagFilter(MinMagFilter filter)
    {
        return filter switch {
            MinMagFilter.Nearest => TextureMagFilter.NEAREST
            , MinMagFilter.Linear => TextureMagFilter.LINEAR
            , _ => throw new ArgumentOutOfRangeException(nameof(filter), filter, null)
        };
    }

    private static TextureWrapMode TranslateAddressMode(SamplerAddressMode mode)
    {
        return mode switch {
            SamplerAddressMode.ClampToEdge => TextureWrapMode.CLAMP_TO_EDGE,
            SamplerAddressMode.Repeat => TextureWrapMode.REPEAT,
            SamplerAddressMode.MirrorRepeat => TextureWrapMode.MIRRORED_REPEAT,
            _ => throw new ArgumentOutOfRangeException(nameof(mode), mode, null)
        };
    }
    
    private void ReleaseUnmanagedResources()
    {
        _glProvider.DoOnContextThread(gl => {
            Span<uint> handles = stackalloc uint[1];
            handles[0] = _handle;
            gl.DeleteSamplers(1, handles);
            _handle = 0;
        });
    }

    public void Dispose()
    {
        ReleaseUnmanagedResources();
        GC.SuppressFinalize(this);
    }

    ~GLSampler()
    {
        ReleaseUnmanagedResources();
    }
}