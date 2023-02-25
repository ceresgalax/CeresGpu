using System;
using System.Runtime.InteropServices;
using CeresGpu.MetalBinding;

namespace CeresGpu.Graphics.Metal
{
    public sealed class MetalStreamingBuffer<T> : IMetalBuffer, IBuffer<T> where T : unmanaged
    {
        private readonly MetalRenderer _renderer;

        private readonly IntPtr[] _buffers;
        private readonly uint[] _sizes;

        private int _lastFrameUpdated;
        private uint _lastFrameUsed = uint.MaxValue;
        
        public uint Count { get; private set; }

        private uint ByteCount => Count * (uint)Marshal.SizeOf<T>();

        public MetalStreamingBuffer(MetalRenderer renderer)
        {
            _renderer = renderer;
            _buffers = new IntPtr[renderer.FrameCount];
            _sizes = new uint[renderer.FrameCount];
            
            RecreateBufferIfNecesary();
            _lastFrameUpdated = renderer.WorkingFrame;
        }

        public IntPtr GetHandleForCurrentFrame()
        {
            //ThrowIfNotReadyForUse();
            //RecreateBufferIfNecesary();
            //int workingFrame = _renderer.WorkingFrame;
            _lastFrameUsed = _renderer.UniqueFrameId;
            return _buffers[_lastFrameUpdated];
        }
        
        public void ThrowIfNotReadyForUse()
        {
        //     int workingFrame = _renderer.WorkingFrame;
        //     if (_lastFrameUpdated != workingFrame) {
        //         throw new InvalidOperationException("Cannot use streaming buffer until contents have been updated for this frame.");
        //     }
        }

        public void Allocate(uint elementCount)
        {
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
        
        public void Set(uint offset, Span<T> elements, uint count)
        {
            if (_lastFrameUsed == _renderer.UniqueFrameId) {
                throw new InvalidOperationException("Cannot set buffer after it has been encoded for this frame.");
            }
            
            RecreateBufferIfNecesary();
            
            int workingFrame = _renderer.WorkingFrame;
            IntPtr buffer = _buffers[workingFrame];

            if (buffer == IntPtr.Zero) {
                throw new InvalidOperationException("Internal error. Somehow a buffer is null");
            }

            MetalBufferUtil.CopyBuffer(buffer, offset, elements, count, Count);

            _lastFrameUpdated = workingFrame;
        }

        public void Set(in T element)
        {
            Set(0, in element);
        }

        public void Set(uint offset, in T element)
        {
            unsafe {
                fixed (T* p = &element) {
                    Set(offset, new Span<T>(p, 1), 1);
                }
            }
        }

        public void PrepareToUpdateExternally()
        {
            RecreateBufferIfNecesary();
            _lastFrameUpdated = _renderer.WorkingFrame;
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

        public void Dispose()
        {
            ReleaseUnmanagedResources();
            GC.SuppressFinalize(this);
        }

        ~MetalStreamingBuffer() {
            ReleaseUnmanagedResources();
        }
    }
}