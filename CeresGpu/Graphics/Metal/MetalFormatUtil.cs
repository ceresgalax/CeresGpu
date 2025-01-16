using System;
using CeresGpu.MetalBinding;
using MTLPixelFormat = CeresGpu.MetalBinding.MetalApi.MTLPixelFormat;

namespace CeresGpu.Graphics.Metal;

public static class MetalFormatUtil
{
    public static MTLPixelFormat ToMtlPixelFormat(this ColorFormat format)
    {
        return format switch { 
            ColorFormat.R8G8B8A8_UNORM => MTLPixelFormat.RGBA8Unorm,
            ColorFormat.B8G8R8A8_UNORM => MTLPixelFormat.BGRA8Unorm,
            ColorFormat.R8_UNORM => MTLPixelFormat.R8Unorm,
            _ => throw new ArgumentOutOfRangeException(nameof(format), format, null)
        };
    }
    
    public static ColorFormat ToColorFormat(this MTLPixelFormat format)
    {
        return format switch { 
            MTLPixelFormat.RGBA8Unorm => ColorFormat.R8G8B8A8_UNORM,
            MTLPixelFormat.BGRA8Unorm => ColorFormat.B8G8R8A8_UNORM,
            MTLPixelFormat.R8Unorm => ColorFormat.R8_UNORM,
            _ => throw new ArgumentOutOfRangeException(nameof(format), format, null)
        };
    }
    
    public static MTLPixelFormat ToMtlPixelFormat(this DepthStencilFormat format)
    {
        return format switch {
            DepthStencilFormat.D16_UNORM => MTLPixelFormat.Depth16Unorm,
            //DepthStencilFormat.X8D24_UNORM_PACK32 => throw new NotSupportedException(),
            DepthStencilFormat.D32_SFLOAT => MTLPixelFormat.Depth32Float,
            DepthStencilFormat.S8_UINT => MTLPixelFormat.Stencil8,
            //DepthStencilFormat.D16_UNORM_S8_UINT => throw new NotSupportedException(),
            DepthStencilFormat.D24_UNORM_S8_UINT => MTLPixelFormat.Depth24Unorm_Stencil8,
            DepthStencilFormat.D32_SFLOAT_S8_UINT => MTLPixelFormat.Depth32Float_Stencil8,
            _ => throw new ArgumentOutOfRangeException(nameof(format), format, null)
        };
    }
}