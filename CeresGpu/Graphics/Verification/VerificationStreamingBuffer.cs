using System;

namespace CeresGpu.Graphics.Verification;

public class VerificationStreamingBuffer<T> : IBuffer<T> where T : unmanaged
{
    private readonly IBuffer<T> _inner;

    public VerificationStreamingBuffer(IBuffer<T> inner)
    {
        _inner = inner;
    }
    
    public void Dispose()
    {
        _inner.Dispose();
    }

    public uint Count => _inner.Count;

    public void Allocate(uint elementCount)
    {
        _inner.Allocate(elementCount);
    }

    public void Set(uint offset, Span<T> elements)
    {
        _inner.Set(offset, elements);
    }

    public void Set(Span<T> elements, uint count)
    {
        _inner.Set(elements, count);
    }

    public void Set(Span<T> elements)
    {
        _inner.Set(elements);
    }

    public void Set(uint offset, Span<T> elements, uint count)
    {
        _inner.Set(offset, elements, count);
    }

    public void Set(in T element)
    {
        _inner.Set(in element);
    }

    public void Set(uint offset, in T element)
    {
        _inner.Set(offset, in element);
    }
}