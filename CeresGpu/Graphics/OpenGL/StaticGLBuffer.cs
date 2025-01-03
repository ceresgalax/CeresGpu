using System;
using CeresGL;

namespace CeresGpu.Graphics.OpenGL
{
    public sealed class StaticGLBuffer<T> : StaticBuffer<T>, IGLBuffer where T : unmanaged
    {
        private readonly GLBuffer<T> _inner;
        
        public StaticGLBuffer(IGLProvider glProvider)
        {
            _inner = new GLBuffer<T>(glProvider);
        }

        public override uint Count => _inner.Count;

        protected override void AllocateImpl(uint elementCount)
        {
            _inner.Allocate(elementCount, BufferUsageARB.STATIC_DRAW);
        }

        protected override void SetImpl(uint offset, Span<T> elements, uint count)
        {
            _inner.Set(offset, elements, count);
        }

        private T[] _directBuffer = Array.Empty<T>();

        protected override void SetDirectImpl(IBuffer<T>.DirectSetter setter)
        {
            // TODO: This is pretty inefficient. We should memory map the buffer instead?
        
            if (_directBuffer.Length != Count) {
                _directBuffer = new T[Count];
            }

            setter(_directBuffer);
            _inner.Set(0, _directBuffer, Count);
            
        }

        void IGLBuffer.Commit()
        {
            Commit();
        }

        public uint GetHandleForCurrentFrame()
        {
            return _inner.Handle;
        }

        protected override void Dispose(bool isDisposing)
        {
            if (isDisposing) {
                _inner.Dispose();    
            }
        }
    }
}