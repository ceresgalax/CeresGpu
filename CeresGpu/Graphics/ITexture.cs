using System;
using System.Numerics;

namespace CeresGpu.Graphics
{
    public interface ITexture : IDisposable
    {
        public uint Width { get; }
        public uint Height { get; }

        public Vector2 Size => new(Width, Height);

        public IntPtr WeakHandle { get; }
        
        public void Set(ReadOnlySpan<byte> data, uint width, uint height, ColorFormat format);
        
        
        /// <summary>
        /// Declare that you intend to use this texture in a pass that will mutate its contents.
        /// You must declare any intended mutations before encoding any commands into a pass that will mutate the texture.
        /// </summary>
        public void DeclareMutationInPass();

        /// <summary>
        /// Declare that you intend to use this texture in a pass that will read its contents.
        /// You must declare any reads before encoding any commands into a pass that will read the texture.
        /// You only need to declare reads if you also declare mutations on this texture.
        /// </summary>
        public void DeclareReadInPass();
    }
}
