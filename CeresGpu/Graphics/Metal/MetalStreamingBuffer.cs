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

        void IMetalBuffer.Commit()
        {
            Commit();
        }

        public override void Allocate(uint elementCount)
        {
            base.Allocate(elementCount);
            _count = elementCount;
        }

        private void RecreateBufferIfNecesary()
        {
            int workingFrame = _renderer.WorkingFrame;
            
            bool needsNewBuffer = _sizes[workingFrame] != Count || _buffers[workingFrame] == IntPtr.Zero;
            
            if (needsNewBuffer) {
                IntPtr oldBuffer = _buffers[workingFrame];
                if (oldBuffer != IntPtr.Zero) {
                    MetalApi.metalbinding_release_buffer(oldBuffer);
                    _buffers[workingFrame] = IntPtr.Zero;
                }

                // Metal will not create buffers of zero size.
                uint byteCount = ByteCount;
                if (byteCount == 0) {
                    byteCount = 1;
                }

                IntPtr newBuffer = MetalApi.metalbinding_new_buffer(_renderer.Context, byteCount);
                _sizes[workingFrame] = Count;
                _buffers[workingFrame] = newBuffer;
            }
        }
        
        public override void Set(uint offset, Span<T> elements, uint count)
        {
            base.Set(offset, elements, count);
            
            RecreateBufferIfNecesary();
            
            int workingFrame = _renderer.WorkingFrame;
            IntPtr buffer = _buffers[workingFrame];

            if (buffer == IntPtr.Zero) {
                throw new InvalidOperationException("Internal error. Somehow a buffer is null");
            }

            MetalBufferUtil.CopyBuffer(buffer, offset, elements, count, Count);
        }

        public void PrepareToUpdateExternally()
        {
            RecreateBufferIfNecesary();
        }

        private void ReleaseUnmanagedResources()
        {
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