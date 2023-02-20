using System;
using System.Drawing;
using System.Numerics;
using CeresGpu.Graphics;

namespace CeresGpu.Graphics
{
    public interface ITexture : IDisposable
    {
        public uint Width { get; }
        public uint Height { get; }

        public Vector2 Size => new(Width, Height);

        public IntPtr WeakHandle { get; }
        
        public void Set(ReadOnlySpan<byte> data, uint width, uint height, InputFormat format);
        public void SetFilter(MinMagFilter min, MinMagFilter mag);
    }
}
