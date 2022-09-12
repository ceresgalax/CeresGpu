using System;
using System.Drawing;
using System.Drawing.Imaging;

namespace Metalancer.Graphics
{
    public static class TexureUtil
    {
        public static void Set(this ITexture texture, Bitmap bitmap)
        {
            byte[] data = new byte[bitmap.Width * bitmap.Height * 4];

            unsafe {
                fixed (byte* p = data) {
                    ConvertData(bitmap, (IntPtr)p);
                }
            }

            texture.Set(data, (uint)bitmap.Width, (uint)bitmap.Height, InputFormat.B8G8R8A8_UNORM);
        }
        
        private static unsafe void ConvertData(Bitmap bitmap, IntPtr destIntPtr)
        {
            //Console.WriteLine("Starting prep of pixel data");
            
            BitmapData bitmapData = bitmap.LockBits(
                new Rectangle(0, 0, bitmap.Width, bitmap.Height),
                ImageLockMode.ReadOnly,
                System.Drawing.Imaging.PixelFormat.Format32bppArgb);
            
            byte* src = (byte*)bitmapData.Scan0;
            
            if (src == (byte*)0) {
                return;
            }
            
            byte* dest = (byte*) destIntPtr;
            
            for (int y = 0, ylen = bitmap.Height; y < ylen; ++y) {
                for (int x = 0, xlen = bitmap.Width; x < xlen; ++x) {
                    int destBase = (y * xlen + x) * 4;
                    int srcBase = ((ylen - y - 1) * xlen + x) * 4;
                    dest[destBase] = src[srcBase + 0];
                    dest[destBase + 1] = src[srcBase + 1];
                    dest[destBase + 2] = src[srcBase + 2];
                    dest[destBase + 3] = src[srcBase + 3];
                }
            }
        }
        
    }
}