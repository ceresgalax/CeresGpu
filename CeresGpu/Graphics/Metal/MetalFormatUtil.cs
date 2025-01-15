using System;
using CeresGpu.MetalBinding;

namespace CeresGpu.Graphics.Metal;

public static class MetalFormatUtil
{
    public static MetalApi.MTLPixelFormat ToMtlPixelFormat(this ColorFormat format)
    {
        return format switch { 
            ColorFormat.R8G8B8A8_UNORM => MetalApi.MTLPixelFormat.RGBA8Unorm,
            ColorFormat.B8G8R8A8_UNORM => MetalApi.MTLPixelFormat.BGRA8Unorm,
            ColorFormat.R8_UNORM => MetalApi.MTLPixelFormat.R8Unorm,
            _ => throw new ArgumentOutOfRangeException(nameof(format), format, null)
        };
    }
    
    public static ColorFormat ToColorFormat(this MetalApi.MTLPixelFormat format)
    {
        return format switch { 
            MetalApi.MTLPixelFormat.RGBA8Unorm => ColorFormat.R8G8B8A8_UNORM,
            MetalApi.MTLPixelFormat.BGRA8Unorm => ColorFormat.B8G8R8A8_UNORM,
            MetalApi.MTLPixelFormat.R8Unorm => ColorFormat.R8_UNORM,
            _ => throw new ArgumentOutOfRangeException(nameof(format), format, null)
        };
    }
}