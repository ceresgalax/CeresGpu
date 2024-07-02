using System;
using System.Runtime.InteropServices;
using CeresGpu.MetalBinding;

namespace CeresGpu.Graphics.Metal
{
    public sealed class MetalStreamingBuffer<T> : StreamingBuffer<T>, IMetalBuffer where T : unmanaged
    {
        private readonly MetalRenderer _renderer;

        private int _activeIndex;
        
        private readonly IntPtr[] _buffers;
        private readonly uint[] _sizes;
        
        private uint _lastAllocationFrameId = uint.MaxValue;
        
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
            return _buffers[_activeIndex];
        }

        void IMetalBuffer.Commit()
        {
            Commit();
        }

        public override void Allocate(uint elementCount)
        {
            base.Allocate(elementCount);

            if (_lastAllocationFrameId != _renderer.UniqueFrameId) {
                _lastAllocationFrameId = _renderer.UniqueFrameId;
                _activeIndex = (_activeIndex + 1) % _renderer.FrameCount;    
            }
            
            _count = elementCount;
            
            RecreateBufferIfNecesary();
        }

        private void RecreateBufferIfNecesary()
        {
            bool needsNewBuffer = _sizes[_activeIndex] != Count || _buffers[_activeIndex] == IntPtr.Zero;
            
            if (needsNewBuffer) {
                IntPtr oldBuffer = _buffers[_activeIndex];
                if (oldBuffer != IntPtr.Zero) {
                    MetalApi.metalbinding_release_buffer(oldBuffer);
                    _buffers[_activeIndex] = IntPtr.Zero;
                }

                // Metal will not create buffers of zero size.
                uint byteCount = ByteCount;
                if (byteCount == 0) {
                    byteCount = 1;
                }

                IntPtr newBuffer = MetalApi.metalbinding_new_buffer(_renderer.Context, byteCount);
                _sizes[_activeIndex] = Count;
                _buffers[_activeIndex] = newBuffer;
            }
        }
        
        public override void Set(uint offset, Span<T> elements, uint count)
        {
            base.Set(offset, elements, count);

            if (_lastAllocationFrameId != _renderer.UniqueFrameId) {
                Allocate(Count);
            }
            
            // if (_lastAllocationFrameId != _renderer.UniqueFrameId) {
            //     throw new InvalidOperationException("Must call Allocate the same frame before modifying a streaming buffer");
            // }
            
            IntPtr buffer = _buffers[_activeIndex];

            // if (buffer == IntPtr.Zero) {
            //     throw new InvalidOperationException("Internal error. Somehow a buffer is null");
            // }

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