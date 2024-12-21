﻿using System;
using CeresGL;

namespace CeresGpu.Graphics.OpenGL;

public sealed class StreamingGLBuffer<T> : StreamingBuffer<T>, IGLBuffer where T : unmanaged
{
    // TODO: Should we still use different GL buffers for each frame?
    // While OpenGL buffers can technically have buffers modified while they're in use, the consequence might be that
    // we wait until the buffer is not in use by the GPU, unintentionally sycnhronizing with the gpu (ouch!)
    // Using a buffer for each swapchain frame would guarantee we avoid this. Maybe the drivers optimzie against this,
    // but of course that varies per driver and there's no guarantee that's happening. Also I'd be concerned about
    // the buffering the buffers confusing some poor OpenGL drivers?
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