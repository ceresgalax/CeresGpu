using System;

namespace CeresGpu.Graphics;

public abstract class StreamingBuffer<T> : IStreamingBuffer<T> where T : unmanaged
{
    private uint _lastFrameCommited = uint.MaxValue;
    private uint _lastFrameSet = uint.MaxValue;

    private uint _head;
    
    /// <summary>
    /// Return the number of elements allocated in this buffer.
    /// </summary>
    public abstract uint Count { get; }
    
    // TODO: NEED THE RESET OF CERES GPU TO USE THIS FOR VALIDATION
    public uint NumElementsThisFrame => Renderer.UniqueFrameId == _lastFrameSet ? _head : 0;

    protected abstract IRenderer Renderer { get; }
    
    public void Allocate(uint elementCount)
    {
        PrepareToModify();
        AllocateImpl(elementCount);
        _lastFrameSet = Renderer.UniqueFrameId;
        _head = 0;
    }

    protected abstract void AllocateImpl(uint elementCount);

    public void Reset()
    {
        PrepareToModify();
        _lastFrameSet = Renderer.UniqueFrameId;
        _head = 0;
    }
    
    public void Set(ReadOnlySpan<T> elements)
    {
        Set(elements, (uint)elements.Length);
    }
    
    public void Set(in T element)
    {
        unsafe {
            fixed (T* p = &element) {
                Set(new Span<T>(p, 1));
            }
        }
    }

    public void Set(ReadOnlySpan<T> elements, uint count)
    {
        Set(0, elements, count);
        _head = count;
    }

    private void Set(uint offset, ReadOnlySpan<T> elements, uint count)
    {
        if (offset + count > Count) {
            throw new ArgumentOutOfRangeException(nameof(count), count, "Buffer allocation is too small.");
        }
        
        PrepareToModify();
        _lastFrameSet = Renderer.UniqueFrameId;
        
        SetImpl(offset, elements, count);
    }
    
    protected abstract void SetImpl(uint offset, ReadOnlySpan<T> elements, uint count);

    public void SetDirect(IStreamingBuffer<T>.DirectSetter setter, uint count)
    {
        if (count > Count) {
            throw new ArgumentOutOfRangeException(nameof(count), count, "Buffer allocation is too small.");
        }
        
        PrepareToModify();
        SetDirectImpl(setter, count);
        _head = count;
        _lastFrameSet = Renderer.UniqueFrameId;
    }

    public void Add(in T element)
    {
        unsafe {
            fixed (T* p = &element) {
                Add(new Span<T>(p, 1));
            }
        }
    }

    public void Add(ReadOnlySpan<T> elements)
    {
        Add(elements, (uint)elements.Length);
    }

    public void Add(ReadOnlySpan<T> elements, uint count)
    {
        if (_lastFrameSet != Renderer.UniqueFrameId) {
            throw new InvalidOperationException("Cannot Add to streaming buffer before it has been Set this frame.");
        }
        Set(_head, elements, count);
        _head += count;
    }

    protected abstract void SetDirectImpl(IStreamingBuffer<T>.DirectSetter setter, uint count);

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