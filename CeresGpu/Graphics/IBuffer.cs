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
        
        /// <summary>
        /// Set elements into the buffer
        /// </summary>
        /// <param name="offset">The element offset into the buffer to start setting elements at.</param>
        /// <param name="elements">
        /// The elements to set into the buffer. All elements will be set into the buffer.
        /// The buffer must be allocated to contain at least <see cref="elements"/>.count + <see cref="offset"/> elements.
        /// </param>
        void Set(uint offset, Span<T> elements);
        
        void Set(Span<T> elements, uint count);
        
        void Set(Span<T> elements);
        
        /// <summary>
        /// Set elements into the buffer.
        /// </summary>
        /// <param name="offset">The element offset into the buffer to start setting elements at.</param>
        /// <param name="elements">The elements to set into the buffer.</param>
        /// <param name="count">The number of elements from <see cref="elements"/> to set into the buffer.</param>
        void Set(uint offset, Span<T> elements, uint count);
        
        /// <summary>
        /// Set a single element to the beginning of the buffer.
        /// </summary>
        /// <param name="element">The element to set into the very beginning of the buffer.</param>
        void Set(in T element);
        
        /// <summary>
        /// Set a single element into a specific spot in the buffer.
        /// </summary>
        /// <param name="offset">The element offset into the buffer to set the element at.</param>
        /// <param name="element">The element to set into the buffer.</param>
        void Set(uint offset, in T element);

        public delegate void DirectSetter(Span<T> elements);
        
        void SetDirect(DirectSetter setter);
    }
}