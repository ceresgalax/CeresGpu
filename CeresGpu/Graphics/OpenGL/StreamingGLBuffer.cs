using System;
using CeresGL;

namespace CeresGpu.Graphics.OpenGL;

public sealed class StreamingGLBuffer<T> : StreamingBuffer<T>, IGLBuffer where T : unmanaged
{
    private readonly GLRenderer _renderer;
    private readonly GLBuffer<T> _inner;

    public StreamingGLBuffer(GLRenderer renderer)
    {
        _renderer = renderer;
        _inner = new GLBuffer<T>(renderer.GLProvider);
    }

    public override uint Count => _inner.Count;

    protected override IRenderer Renderer => _renderer;

    public override void Allocate(uint elementCount)
    {
        base.Allocate(elementCount);
        _inner.Allocate(elementCount, BufferUsageARB.STREAM_DRAW);
    }

    public override void Set(uint offset, Span<T> elements, uint count)
    {
        base.Set(offset, elements, count);
        _inner.Set(offset, elements, count);
    }

    public override void Dispose()
    {
        _inner.Dispose();
    }

    public uint CommitAndGetHandle()
    {
        Commit();
        return _inner.Handle;
    }
}