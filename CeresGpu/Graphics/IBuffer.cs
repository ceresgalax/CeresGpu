using System;

namespace CeresGpu.Graphics
{
    public interface IBuffer<T> : IDisposable where T : unmanaged
    {
        /// <summary>
        /// Count of T elements the buffer can store.
        /// </summary>
        uint Count { get; }

        void Allocate(uint elementCount);
        void Set(uint offset, Span<T> elements);
        void Set(Span<T> elements, uint count);
        void Set(Span<T> elements);
        void Set(uint offset, Span<T> elements, uint count);
        void Set(in T element);
        void Set(uint offset, in T element);
    }
}