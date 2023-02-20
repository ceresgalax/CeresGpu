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
            
            InputFormat format = bitmap.ColorType switch {
                SKColorType.Unknown => throw new ArgumentException()
                , SKColorType.Alpha8 => InputFormat.R8_UNORM
                , SKColorType.Rgb565 => InputFormat.R5G6B5_UNORM_PACK16
                , SKColorType.Argb4444 => InputFormat.B4G4R4A4_UNORM_PACK16
                , SKColorType.Rgba8888 => InputFormat.R8G8B8A8_UNORM
                , SKColorType.Rgb888x => InputFormat.R8G8B8A8_UNORM
                , SKColorType.Bgra8888 => InputFormat.B8G8R8A8_UNORM
                , SKColorType.Rgba1010102 => InputFormat.A2B10G10R10_UNORM_PACK32
                , SKColorType.Rgb101010x => InputFormat.A2B10G10R10_UNORM_PACK32
                , SKColorType.Gray8 => InputFormat.R8_UNORM
                , SKColorType.RgbaF16 => InputFormat.R16G16B16A16_SFLOAT
                , SKColorType.RgbaF16Clamped => InputFormat.R16G16B16A16_SFLOAT
                , SKColorType.RgbaF32 => InputFormat.R32G32B32A32_SFLOAT
                , SKColorType.Rg88 => InputFormat.R8G8_UNORM
                , SKColorType.AlphaF16 => InputFormat.R16_SFLOAT
                , SKColorType.RgF16 => InputFormat.R16G16_SFLOAT
                , SKColorType.Alpha16 => InputFormat.R16_UNORM
                , SKColorType.Rg1616 => InputFormat.R16G16_UNORM
                , SKColorType.Rgba16161616 => InputFormat.R16G16B16A16_UNORM
                , SKColorType.Bgra1010102 => InputFormat.A2R10G10B10_UNORM_PACK32
                , SKColorType.Bgr101010x => InputFormat.A2R10G10B10_UNORM_PACK32
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