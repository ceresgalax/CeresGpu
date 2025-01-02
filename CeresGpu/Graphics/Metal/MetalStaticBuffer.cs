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

        protected override void AllocateImpl(uint elementCount)
        {
            if (_buffer != IntPtr.Zero) {
                MetalApi.metalbinding_release_buffer(_buffer);
                _buffer = IntPtr.Zero;
            }

            _buffer = MetalApi.metalbinding_new_buffer(_renderer.Context, (uint)Marshal.SizeOf<T>() * elementCount);
            _count = elementCount;
        }
        
        protected override void SetImpl(uint offset, Span<T> elements, uint count)
        {
            MetalBufferUtil.CopyBuffer(_buffer, offset, elements, count, Count);
        }

        protected override void SetDirectImpl(IBuffer<T>.DirectSetter setter)
        {
            Span<T> directBuffer;
            unsafe {
                directBuffer = new Span<T>((void*)MetalApi.metalbinding_buffer_get_contents(_buffer), (int)_count);
            }
            setter(directBuffer);
            
            // We need to assume the user modified the whole buffer.
            MetalApi.metalbinding_buffer_did_modify_range(_buffer, 0, (uint)Marshal.SizeOf<T>() * _count);
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