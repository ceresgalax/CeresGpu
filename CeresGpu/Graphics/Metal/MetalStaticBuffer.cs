using System;
using System.Runtime.InteropServices;
using CeresGpu.MetalBinding;

namespace CeresGpu.Graphics.Metal
{
    public sealed class MetalStaticBuffer<T> : StaticBuffer<T>, IMetalBuffer where T : unmanaged
    {
        private readonly MetalRenderer _renderer;

        private IntPtr _buffer;
        private uint _count;
        
        public MetalStaticBuffer(MetalRenderer renderer)
        {
            _renderer = renderer;
        }

        public override uint Count => _count;

        public IntPtr GetHandleForCurrentFrame()
        {
            return _buffer;
        }

        void IMetalBuffer.Commit()
        {
            Commit();
        }

        public void PrepareToUpdateExternally()
        {
        }

        public override void Allocate(uint elementCount)
        {
            base.Allocate(elementCount);
            if (_buffer != IntPtr.Zero) {
                MetalApi.metalbinding_release_buffer(_buffer);
                _buffer = IntPtr.Zero;
            }

            _buffer = MetalApi.metalbinding_new_buffer(_renderer.Context, (uint)Marshal.SizeOf<T>() * elementCount);
            _count = elementCount;
        }
        
        public override void Set(uint offset, Span<T> elements, uint count)
        {
            base.Set(offset, elements, count);
            MetalBufferUtil.CopyBuffer(_buffer, offset, elements, count, Count);
        }

        private void ReleaseUnmanagedResources()
        {
            if (_buffer != IntPtr.Zero) {
                MetalApi.metalbinding_release_buffer(_buffer);
                _buffer = IntPtr.Zero;
            }
        }

        public override void Dispose()
        {
            ReleaseUnmanagedResources();
            GC.SuppressFinalize(this);
        }

        ~MetalStaticBuffer() {
            ReleaseUnmanagedResources();
        }
    }
}