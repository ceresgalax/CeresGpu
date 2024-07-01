using System;
using CeresGL;

namespace CeresGpu.Graphics.OpenGL;

/// <summary>
/// Like our normal streaming buffer impl for OpenGL, but uses mutliple buffers to simulate the invalidation of buffer
/// contents, making it easier to find incorrect usage of streaming buffers when using the OpenGL backend.
/// </summary>
public sealed class DebugStreamingGLBuffer<T> : StreamingBuffer<T>, IGLBuffer where T : unmanaged
{
    private readonly GLRenderer _renderer;
    private readonly GLBuffer<T>[] _buffers;

    const uint WORKING_BUFFER_COUNT = 3;
    
    /// <summary>
    /// Count of T elements the buffer is sized for.
    /// </summary>
    private uint _count;

    public DebugStreamingGLBuffer(GLRenderer renderer)
    {
        _renderer = renderer;
        _buffers = new GLBuffer<T>[WORKING_BUFFER_COUNT];
        IGLProvider provider = renderer.GLProvider;
        for (int i = 0; i < WORKING_BUFFER_COUNT; ++i) {
            _buffers[i] = new GLBuffer<T>(provider);
        }
    }

    public override uint Count => _count;

    protected override IRenderer Renderer => _renderer;

    public override void Allocate(uint elementCount)
    {
        base.Allocate(elementCount);
        _count = elementCount;
    }

    public override void Set(uint offset, Span<T> elements, uint count)
    {
        base.Set(offset, elements, count);
        RecreateBufferIfNecesary();
        _buffers[WorkingFrameIndex].Set(offset, elements, count);
    }

    public override void Dispose()
    {
        foreach (GLBuffer<T> buffer in _buffers) {
            buffer.Dispose();
        }
    }

    public uint CommitAndGetHandle()
    {
        Commit();
        return _buffers[WorkingFrameIndex].Handle;
    }

    private uint WorkingFrameIndex => _renderer.UniqueFrameId % WORKING_BUFFER_COUNT;
    
    private void RecreateBufferIfNecesary()
    {
        uint workingFrame = WorkingFrameIndex;
            
        bool needsNewBuffer = _buffers[workingFrame].Count != _count;
            
        if (needsNewBuffer) {
            _buffers[workingFrame].Allocate(_count, BufferUsageARB.STREAM_DRAW);
        }
    }
}