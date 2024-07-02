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