using System;

namespace CeresGpu.Graphics;

public abstract class StreamingBuffer<T> : IStreamingBuffer<T> where T : unmanaged
{
    private uint _lastFrameCommited = uint.MaxValue;
    private uint _lastFrameSet = uint.MaxValue;

    //private uint _head;
    
    /// <summary>
    /// Return the number of elements allocated in this buffer.
    /// </summary>
    public abstract uint Count { get; }
    
    public string Label { get; set; } = "";

    protected abstract IRenderer Renderer { get; }
    
    public void Allocate(uint elementCount)
    {
        PrepareToModify();
        AllocateImpl(elementCount);
        _lastFrameSet = Renderer.UniqueFrameId;
        //_head = 0;
    }
    
    protected abstract void AllocateImpl(uint elementCount);
    
    public void Set(ReadOnlySpan<T> elements)
    {
        Set(elements, (uint)elements.Length);
    }
    
    public void Set(uint offset, ReadOnlySpan<T> elements)
    {
        Set(offset, elements, (uint)elements.Length);
    }

    public void Set(in T element)
    {
        unsafe {
            fixed (T* p = &element) {
                Set(new Span<T>(p, 1));
            }
        }
    }

    public void Set(uint offset, in T element)
    {
        unsafe {
            fixed (T* p = &element) {
                Set(offset, new Span<T>(p, 1));
            }
        }
    }

    public void Set(ReadOnlySpan<T> elements, uint count)
    {
        Set(0, elements, count);
        //_head = count;
    }

    public void Set(uint offset, ReadOnlySpan<T> elements, uint count)
    {
        if (offset + count > Count) {
            throw new ArgumentOutOfRangeException(nameof(count), count, "Buffer allocation is too small.");
        }
        
        PrepareToModify();
        _lastFrameSet = Renderer.UniqueFrameId;
        
        SetImpl(offset, elements, count);
    }
    
    protected abstract void SetImpl(uint offset, ReadOnlySpan<T> elements, uint count);
    
    public void SetDirect(IBuffer<T>.DirectSetter setter)
    {
        PrepareToModify();
        SetDirectImpl(setter);
        //_head = count;
        _lastFrameSet = Renderer.UniqueFrameId;
    }

    protected abstract void SetDirectImpl(IBuffer<T>.DirectSetter setter);

    public virtual bool Commit()
    {
        // Early out if already commited for this unique frame. 
        if (GetIsCommited()) {
            return true;
        }
        
        if (_lastFrameSet != Renderer.UniqueFrameId) {
            return false;
        }
        
        _lastFrameCommited = Renderer.UniqueFrameId;
        return true;
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