using System;
using SkiaSharp;

namespace CeresGpu.Graphics
{
    public static class TexureUtil
    {
        public static void Set(this ITexture texture, SKBitmap bitmap)
        {
            byte[] data = new byte[bitmap.Width * bitmap.Height * 4];
            
            unsafe {
                fixed (byte* p = data) {
                    ConvertData(bitmap, (IntPtr)p);
                }
            }

            // TODO: Have not double checked that these map correctly
            
            ColorFormat format = bitmap.ColorType switch {
                SKColorType.Unknown => throw new ArgumentException()
                , SKColorType.Alpha8 => ColorFormat.R8_UNORM
                , SKColorType.Rgb565 => ColorFormat.R5G6B5_UNORM_PACK16
                , SKColorType.Argb4444 => ColorFormat.B4G4R4A4_UNORM_PACK16
                , SKColorType.Rgba8888 => ColorFormat.R8G8B8A8_UNORM
                , SKColorType.Rgb888x => ColorFormat.R8G8B8A8_UNORM
                , SKColorType.Bgra8888 => ColorFormat.B8G8R8A8_UNORM
                , SKColorType.Rgba1010102 => ColorFormat.A2B10G10R10_UNORM_PACK32
                , SKColorType.Rgb101010x => ColorFormat.A2B10G10R10_UNORM_PACK32
                , SKColorType.Gray8 => ColorFormat.R8_UNORM
                , SKColorType.RgbaF16 => ColorFormat.R16G16B16A16_SFLOAT
                , SKColorType.RgbaF16Clamped => ColorFormat.R16G16B16A16_SFLOAT
                , SKColorType.RgbaF32 => ColorFormat.R32G32B32A32_SFLOAT
                , SKColorType.Rg88 => ColorFormat.R8G8_UNORM
                , SKColorType.AlphaF16 => ColorFormat.R16_SFLOAT
                , SKColorType.RgF16 => ColorFormat.R16G16_SFLOAT
                , SKColorType.Alpha16 => ColorFormat.R16_UNORM
                , SKColorType.Rg1616 => ColorFormat.R16G16_UNORM
                , SKColorType.Rgba16161616 => ColorFormat.R16G16B16A16_UNORM
                , SKColorType.Bgra1010102 => ColorFormat.A2R10G10B10_UNORM_PACK32
                , SKColorType.Bgr101010x => ColorFormat.A2R10G10B10_UNORM_PACK32
                , _ => throw new ArgumentOutOfRangeException()
            };

            texture.Set(data, (uint)bitmap.Width, (uint)bitmap.Height, format);
        }
        
        private static unsafe void ConvertData(SKBitmap bitmap, IntPtr destIntPtr)
        {
            fixed (byte* src = &bitmap.GetPixelSpan().GetPinnableReference()) {
                if (src == (byte*)0) {
                    return;
                }
            
                byte* dest = (byte*) destIntPtr;

                int rowBytes = bitmap.RowBytes;
            
                for (int y = 0, ylen = bitmap.Height; y < ylen; ++y) {

                    byte* sourceRowStart = src + rowBytes * y;
                    byte* destRowStart = dest + rowBytes * (ylen - y - 1);
                    
                    Buffer.MemoryCopy(sourceRowStart, destRowStart, rowBytes, rowBytes);
                }
            }
        }
        
    }
}