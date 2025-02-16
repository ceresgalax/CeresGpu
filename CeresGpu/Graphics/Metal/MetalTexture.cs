using System;
using System.Runtime.InteropServices;
using CeresGpu.MetalBinding;

namespace CeresGpu.Graphics.Metal
{
    public sealed class MetalTexture : ITexture
    {
        private readonly MetalRenderer _renderer;
        private IntPtr _texture;
        private IntPtr _weakHandle;

        public uint Width { get; private set; }
        public uint Height { get; private set; }

        public IntPtr Handle => _texture;
        public IntPtr WeakHandle => _weakHandle;
        
        public MetalTexture(MetalRenderer renderer)
        {
            _renderer = renderer;
            _weakHandle = GCHandle.ToIntPtr(GCHandle.Alloc(this, GCHandleType.Weak));
        }

        public void Set(ReadOnlySpan<byte> data, uint width, uint height, ColorFormat format)
        {
            uint bytesPerPixel = (uint)format.GetBytesPerPixel();

            uint requiredSize;
            checked {
                requiredSize = width * height * bytesPerPixel;
            }

            if (data.Length < requiredSize) {
                throw new ArgumentException("Invalid data size", nameof(data));
            }
            
            if (_texture != IntPtr.Zero) {
                MetalApi.metalbinding_release_texture(_texture);
                _texture = IntPtr.Zero;
            }
            
            _texture = MetalApi.metalbinding_new_texture(_renderer.Context, width, height, format.ToMtlPixelFormat());

            Width = width;
            Height = height;

            unsafe {
                fixed (byte* p = data) {
                    MetalApi.metalbinding_set_texture_data(_texture, width, height, new IntPtr(p), bytesPerPixel * width);
                }
            }
        }

        private void ReleaseUnmanagedResources()
        {
            // TODO: Do we need to ensure this texture isn't still being used in an encoded arugment buffer before disposing?
            
            if (_texture != IntPtr.Zero) {
                MetalApi.metalbinding_release_texture(_texture);
                _texture = IntPtr.Zero;
            }
            if (_weakHandle != IntPtr.Zero) {
                GCHandle.FromIntPtr(_weakHandle).Free();
                _weakHandle = IntPtr.Zero;
            }
        }

        public void Dispose()
        {
            ReleaseUnmanagedResources();
            GC.SuppressFinalize(this);
        }

        ~MetalTexture() {
            ReleaseUnmanagedResources();
        }
    }
}