using System;
using System.Runtime.InteropServices;
using Metalancer.MetalBinding;

namespace Metalancer.Graphics.Metal
{
    public sealed class MetalStaticBuffer<T> : IMetalBuffer, IBuffer<T> where T : unmanaged
    {
        private readonly MetalRenderer _renderer;

        private IntPtr _buffer;
        
        public MetalStaticBuffer(MetalRenderer renderer)
        {
            _renderer = renderer;
        }
        
        public uint Count { get; private set; }

        public IntPtr GetHandleForCurrentFrame()
        {
            return _buffer;
        }

        public void ThrowIfNotReadyForUse()
        {
        }

        public void PrepareToUpdateExternally()
        {
        }

        public void Allocate(uint elementCount)
        {
            if (_buffer != IntPtr.Zero) {
                MetalApi.metalbinding_release_buffer(_buffer);
                _buffer = IntPtr.Zero;
            }

            _buffer = MetalApi.metalbinding_new_buffer(_renderer.Context, (uint)Marshal.SizeOf<T>() * elementCount);
            Count = elementCount;
        }

        public void Set(uint offset, Span<T> elements)
        {
            Set(offset, elements, (uint)elements.Length);
        }

        public void Set(Span<T> elements, uint count)
        {
            Set(0, elements, count);
        }

        public void Set(Span<T> elements)
        {
            Set(0, elements, (uint)elements.Length);
        }

        public void Set(uint offset, Span<T> elements, uint count)
        {
            MetalBufferUtil.CopyBuffer(_buffer, offset, elements, count, Count);
        }

        public void Set(in T element)
        {
            Set(0, in element);
        }

        public void Set(uint offset, in T element)
        {
            unsafe {
                fixed (T* p = &element) {
                    Set(offset, new Span<T>(p, 1));
                }
            }
        }

        private void ReleaseUnmanagedResources()
        {
            if (_buffer != IntPtr.Zero) {
                MetalApi.metalbinding_release_buffer(_buffer);
                _buffer = IntPtr.Zero;
            }
        }

        public void Dispose()
        {
            ReleaseUnmanagedResources();
            GC.SuppressFinalize(this);
        }

        ~MetalStaticBuffer() {
            ReleaseUnmanagedResources();
        }
    }
}