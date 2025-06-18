using System;
using CeresGL;

namespace CeresGpu.Graphics.OpenGL;

public sealed class StreamingGLBuffer<T> : StreamingBuffer<T>, IGLBuffer where T : unmanaged
{
    // TODO: Should we still use different GL buffers for each frame?
    // While OpenGL buffers can technically have buffers modified while they're in use, the consequence might be that
    // we wait until the buffer is not in use by the GPU, unintentionally sycnhronizing with the gpu (ouch!)
    // Using a buffer for each swapchain frame would guarantee we avoid this. Maybe the drivers optimzie against this,
    // but of course that varies per driver and there's no guarantee that's happening. Also I'd be concerned about
    // the multiple buffers confusing some poor OpenGL drivers?
    // I should just implement the Vulkan backend already :) 
    
    private readonly GLRenderer _renderer;
    private readonly GLBuffer<T> _inner;

    public StreamingGLBuffer(GLRenderer renderer)
    {
        _renderer = renderer;
        _inner = new GLBuffer<T>(renderer.GLProvider);
    }

    public override uint Count => _inner.Count;

    protected override IRenderer Renderer => _renderer;

    protected override void AllocateImpl(uint elementCount)
    {
        _inner.Allocate(elementCount, BufferUsageARB.STREAM_DRAW);
    }

    protected override void SetImpl(uint offset, ReadOnlySpan<T> elements, uint count)
    {
        _inner.Set(offset, elements, count);
    }

    private T[] _directBuffer = [];
    
    protected override void SetDirectImpl(IStreamingBuffer<T>.DirectSetter setter, uint count)
    {
        // TODO: This is pretty inefficient. We should memory map the buffer instead?
        
        if (_directBuffer.Length != Count) {
            _directBuffer = new T[Count];
        }

        setter(_directBuffer.AsSpan(0, (int)count));
        _inner.Set(0, _directBuffer, count);
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