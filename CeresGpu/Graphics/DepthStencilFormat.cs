using System;

namespace CeresGpu.Graphics;

public enum DepthStencilFormat
{
    D16_UNORM,
    
    // Not supported by Metal or OpenGL.
    //X8D24_UNORM_PACK32,
    
    D32_SFLOAT,
    S8_UINT,
    
    // Not supported by Metal or OpenGL.
    // D16_UNORM_S8_UINT,
    
    D24_UNORM_S8_UINT,
    D32_SFLOAT_S8_UINT
}

public static class DepthStencilFormatUtil
{
    public static bool IsDepthFormat(this DepthStencilFormat format)
    {
        switch (format) {
            case DepthStencilFormat.D16_UNORM:
            case DepthStencilFormat.D32_SFLOAT:
                return true;
            case DepthStencilFormat.S8_UINT:
                return false;
            case DepthStencilFormat.D24_UNORM_S8_UINT:
            case DepthStencilFormat.D32_SFLOAT_S8_UINT:
                return true;
            default:
                throw new ArgumentOutOfRangeException(nameof(format), format, null);
        }
    }
    
    public static bool IsStencilFormat(this DepthStencilFormat format)
    {
        switch (format) {
            case DepthStencilFormat.D16_UNORM:
            case DepthStencilFormat.D32_SFLOAT:
                return false;
            case DepthStencilFormat.S8_UINT:
                return true;
            case DepthStencilFormat.D24_UNORM_S8_UINT:
            case DepthStencilFormat.D32_SFLOAT_S8_UINT:
                return true;
            default:
                throw new ArgumentOutOfRangeException(nameof(format), format, null);
        }
    }
}
