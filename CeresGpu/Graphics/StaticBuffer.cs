using System;

namespace CeresGpu.Graphics;

public abstract class StaticBuffer<T> : IStaticBuffer<T> where T : unmanaged
{
    protected bool IsCommited;
    
    public abstract uint Count { get; }

    public virtual void Allocate(uint elementCount)
    {
        CheckCanModify();
    }

    public void Set(uint offset, Span<T> elements)
    {
        Set(offset, elements, (uint)elements.Length);
    }

    public void Set(Span<T> elements, uint count)
    {
        Set(0, elements, count);
    }

    public void Set(Span<T> elements)
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
    
    public virtual void Set(uint offset, Span<T> elements, uint count)
    {
        CheckCanModify();
    }

    public virtual void SetDirect(IBuffer<T>.DirectSetter setter)
    {
        CheckCanModify();
    }

    protected virtual void Commit()
    {
        IsCommited = true;
    }

    public abstract void Dispose();

    private void CheckCanModify()
    {
        if (IsCommited) {
            throw new InvalidOperationException("Static buffer cannnot be updated after use.");    
        }
    }
}