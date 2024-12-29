using System;
using CeresGL;

namespace CeresGpu.Graphics.OpenGL
{
    public sealed class StaticGLBuffer<T> : StaticBuffer<T>, IGLBuffer where T : unmanaged
    {
        private GLBuffer<T> _inner;
        
        public StaticGLBuffer(IGLProvider glProvider)
        {
            _inner = new GLBuffer<T>(glProvider);
        }

        public override uint Count => _inner.Count;

        public override void Allocate(uint elementCount)
        {
            base.Allocate(elementCount);
            _inner.Allocate(elementCount, BufferUsageARB.STATIC_DRAW);
        }

        public override void Set(uint offset, Span<T> elements, uint count)
        {
            base.Set(offset, elements, count);
            _inner.Set(offset, elements, count);
        }

        private T[] _directBuffer = Array.Empty<T>();
        
        public override void SetDirect(IBuffer<T>.DirectSetter setter)
        {
            base.SetDirect(setter);
            
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

        public override void Dispose()
        {
            _inner.Dispose();
        }
    }
}