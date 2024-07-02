using System;

namespace CeresGpu.Graphics;

/// <summary>
/// Utility code for implementing IRenderer classes. 
/// </summary>
public static class RendererUtil
{
    public static ITexture CreateFallbackTexture(IRenderer renderer)
    {
        ITexture texture = renderer.CreateTexture();
        Span<byte> data = stackalloc byte[3 * 4 * 4];
        for (int i = 0; i < (4 * 4 * 3); i += 3)
        {
            data[i + 0] = 0xFF;
            data[i + 1] = 0x00;
            data[i + 2] = 0xFF;    
        }
        
        texture.Set(data, 4, 4, InputFormat.R8G8B8_UNORM);
        return texture;
    }
}