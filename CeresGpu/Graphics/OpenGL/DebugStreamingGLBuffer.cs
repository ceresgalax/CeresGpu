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

    private uint _activeIndex;
    private uint _lastAllocationFrameId = uint.MaxValue;
    
    /// <summary>
    /// Count of T elements the buffer is sized for.
    /// </summary>
    private uint _count;

    public DebugStreamingGLBuffer(GLRenderer renderer)
    {
        _renderer = renderer;
        _buffers = new GLBuffer<T>[_renderer.WorkingFrameCount];
        IGLProvider provider = renderer.GLProvider;
        for (int i = 0; i < _buffers.Length; ++i) {
            _buffers[i] = new GLBuffer<T>(provider);
        }
    }

    public override uint Count => _count;

    protected override IRenderer Renderer => _renderer;

    protected override void AllocateImpl(uint elementCount)
    {
        if (_lastAllocationFrameId != _renderer.UniqueFrameId) {
            _lastAllocationFrameId = _renderer.UniqueFrameId;
            _activeIndex = (_activeIndex + 1) % _renderer.WorkingFrameCount;    
        }
        
        _count = elementCount;
        
         RecreateBufferIfNecesary();
    }

    protected override void SetImpl(uint offset, ReadOnlySpan<T> elements, uint count)
    {
        if (_lastAllocationFrameId != _renderer.UniqueFrameId) {
            Allocate(Count);
        }
        
        _buffers[_activeIndex].Set(offset, elements, count);
    }
    
    private T[] _directBuffer = [];
    
    protected override void SetDirectImpl(IBuffer<T>.DirectSetter setter)
    {
        if (_lastAllocationFrameId != _renderer.UniqueFrameId) {
            Allocate(Count);
        }
        
        // TODO: This is pretty inefficient. We should memory map the buffer instead?
        
        if (_directBuffer.Length != Count) {
            _directBuffer = new T[Count];
        }

        setter(_directBuffer);
        _buffers[_activeIndex].Set(0, _directBuffer, Count);
    }

    public override void Dispose()
    {
        foreach (GLBuffer<T> buffer in _buffers) {
            buffer.Dispose();
        }
    }

    void IGLBuffer.Commit()
    {
        if (GetIsCommited()) {
            return;
        }
        
        RecreateBufferIfNecesary();
        Commit();
    }

    public uint GetHandleForCurrentFrame()
    {
        return _buffers[_activeIndex].Handle;
    }
    
    private void RecreateBufferIfNecesary()
    {
        bool needsNewBuffer = _buffers[_activeIndex].Count != _count;
            
        if (needsNewBuffer) {
            _buffers[_activeIndex].Allocate(_count, BufferUsageARB.STREAM_DRAW);
        }
    }
}