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
        const int BPP = 4;
        const int WIDTH = 4;
        const int HEIGHT = 4;
        Span<byte> data = stackalloc byte[BPP * WIDTH * HEIGHT];
        for (int i = 0; i < data.Length; i += BPP)
        {
            data[i + 0] = 0xFF;
            data[i + 1] = 0x00;
            data[i + 2] = 0xFF;
            data[i + 3] = 0xFF;
        }
        
        texture.Set(data, 4, 4, ColorFormat.R8G8B8A8_UNORM);
        return texture;
    }
}