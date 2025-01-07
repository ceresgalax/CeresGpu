using System;

namespace CeresGpu.Graphics;

public abstract class StreamingBuffer<T> : IStreamingBuffer<T> where T : unmanaged
{
    private uint _lastFrameCommited = uint.MaxValue;

    // private readonly ValidRegionTracker _validRegionTracker = new();
    
    public abstract uint Count { get; }

    protected abstract IRenderer Renderer { get; }

    public virtual void Allocate(uint elementCount)
    {
        PrepareToModify();
        // _validRegionTracker.Reset();
    }

    public void Set(uint offset, ReadOnlySpan<T> elements)
    {
        Set(offset, elements, (uint)elements.Length);
    }

    public void Set(ReadOnlySpan<T> elements, uint count)
    {
        Set(0, elements, count);
    }

    public void Set(ReadOnlySpan<T> elements)
    {
        Set(0, elements, (uint)elements.Length);
    }
    
    public void Set(in T element)
    {
        Set(0, in element);
    }
    
    public void Set(uint offset, in T element)
    {
        unsafe {
            fixed (T* p = &element) {
                Set(offset, new Span<T>(p, 1));
            }
        }
    }
    
    public virtual void Set(uint offset, ReadOnlySpan<T> elements, uint count)
    {
        PrepareToModify();
    }

    public virtual void SetDirect(IBuffer<T>.DirectSetter setter)
    {
        PrepareToModify();
    }

    protected virtual void Commit()
    {
        // Early out if already commited for this unique frame. 
        if (GetIsCommited()) {
            return;
        }
        
        _lastFrameCommited = Renderer.UniqueFrameId;
    }

    public abstract void Dispose();

    private void PrepareToModify()
    {
        if (_lastFrameCommited == Renderer.UniqueFrameId) {
            throw new InvalidOperationException("Cannot modify streaming buffer already used this frame.");
        }
    }

    protected bool GetIsCommited()
    {
        return _lastFrameCommited == Renderer.UniqueFrameId;
    }
}