using System;
using System.Runtime.InteropServices;
using CeresGpu.MetalBinding;

namespace CeresGpu.Graphics.Metal
{
    public sealed class MetalStreamingBuffer<T> : StreamingBuffer<T>, IMetalBuffer where T : unmanaged
    {
        private readonly MetalRenderer _renderer;

        private readonly IntPtr[] _buffers;
        private readonly uint[] _sizes;

        /// <summary>
        /// Number of elements allocated.
        /// </summary>
        private uint _count;

        public override uint Count => _count;

        protected override IRenderer Renderer => _renderer;

        private uint ByteCount => Count * (uint)Marshal.SizeOf<T>();

        public MetalStreamingBuffer(MetalRenderer renderer)
        {
            _renderer = renderer;
            _buffers = new IntPtr[renderer.FrameCount];
            _sizes = new uint[renderer.FrameCount];
            
            RecreateBufferIfNecesary();
        }

        public IntPtr GetHandleForCurrentFrame()
        {
            return _buffers[_renderer.WorkingFrame];
        }

        protected override void AllocateImpl(uint elementCount)
        {
            _count = elementCount;
            RecreateBufferIfNecesary();
        }

        private void RecreateBufferIfNecesary()
        {
            int frame = _renderer.WorkingFrame;
            
            bool needsNewBuffer = _sizes[frame] != Count || _buffers[frame] == IntPtr.Zero;
            
            if (needsNewBuffer) {
                IntPtr oldBuffer = _buffers[frame];
                if (oldBuffer != IntPtr.Zero) {
                    MetalApi.metalbinding_release_buffer(oldBuffer);
                    _buffers[frame] = IntPtr.Zero;
                }

                // Metal will not create buffers of zero size.
                uint byteCount = ByteCount;
                if (byteCount == 0) {
                    byteCount = 1;
                }

                IntPtr newBuffer = MetalApi.metalbinding_new_buffer(_renderer.Context, byteCount);
                _sizes[frame] = Count;
                _buffers[frame] = newBuffer;
            }
        }
        
        protected override void SetImpl(uint offset, ReadOnlySpan<T> elements, uint count)
        {
            RecreateBufferIfNecesary();
            IntPtr buffer = _buffers[_renderer.WorkingFrame];
            MetalBufferUtil.CopyBuffer(buffer, offset, elements, count, Count);
        }

        protected override void SetDirectImpl(IStreamingBuffer<T>.DirectSetter setter)
        {
            IntPtr buffer = _buffers[_renderer.WorkingFrame];

            Span<T> directBuffer;
            unsafe {
                directBuffer = new Span<T>((void*)MetalApi.metalbinding_buffer_get_contents(buffer), (int)_count);
            }

            setter(directBuffer);
            
            MetalApi.metalbinding_buffer_did_modify_range(buffer, 0, (uint)Marshal.SizeOf<T>() * _count);
        }

        public void PrepareToUpdateExternally()
        {
            RecreateBufferIfNecesary();
        }

        private void ReleaseUnmanagedResources()
        {
            // TODO: We need to defer delete in case this gets released while being referenced by an in-flight argument buffer!
            for (int i = 0, ilen = _buffers.Length; i < ilen; ++i) {
                IntPtr buffer = _buffers[i];
                if (buffer != IntPtr.Zero) {
                    MetalApi.metalbinding_release_buffer(buffer);    
                }
                _buffers[i] = IntPtr.Zero;
            }
        }

        public override void Dispose()
        {
            ReleaseUnmanagedResources();
            GC.SuppressFinalize(this);
        }

        ~MetalStreamingBuffer() {
            ReleaseUnmanagedResources();
        }
    }
}