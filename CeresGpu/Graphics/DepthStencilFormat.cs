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
