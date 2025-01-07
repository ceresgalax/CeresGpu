using System;

namespace CeresGpu.Graphics;

public abstract class StaticBuffer<T> : IStaticBuffer<T> where T : unmanaged
{
    protected bool IsCommited;
    
    public abstract uint Count { get; }

    public void Allocate(uint elementCount)
    {
        CheckCanModify();
        AllocateImpl(elementCount);
    }

    protected abstract void AllocateImpl(uint elementCount);

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
    
    public void Set(uint offset, ReadOnlySpan<T> elements, uint count)
    {
        CheckCanModify();
        if (count + offset > Count) {
            throw new IndexOutOfRangeException();
        }
        SetImpl(offset, elements, count);
    }

    protected abstract void SetImpl(uint offset, ReadOnlySpan<T> elements, uint count);

    public void SetDirect(IBuffer<T>.DirectSetter setter)
    {
        CheckCanModify();
        SetDirectImpl(setter);
    }

    protected abstract void SetDirectImpl(IBuffer<T>.DirectSetter setter); 

    protected virtual void Commit()
    {
        IsCommited = true;
    }

    

    private void CheckCanModify()
    {
        if (IsCommited) {
            throw new InvalidOperationException("Static buffer cannnot be updated after use.");    
        }
    }

    protected virtual void Dispose(bool disposing)
    {
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    ~StaticBuffer()
    {
        Dispose(false);
    }
}