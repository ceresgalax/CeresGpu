using System;

namespace CeresGpu.Graphics;

/// <summary>
/// The contents of this buffer are static, and cannot be altered after calling <see cref="IBuffer{T}.Commit"/>.
///
/// These buffers are cheaper memory wise, as memory is not needed to contain the contents of these buffers
/// for in-flight frames.
/// </summary>
public interface IStaticBuffer<T> : IBuffer<T> where T : unmanaged
{
    
    /// <summary>
    /// Set elements into the buffer
    /// </summary>
    /// <param name="offset">The element offset into the buffer to start setting elements at.</param>
    /// <param name="elements">
    /// The elements to set into the buffer. All elements will be set into the buffer.
    /// The buffer must be allocated to contain at least <see cref="elements"/>.count + <see cref="offset"/> elements.
    /// </param>
    void Set(uint offset, ReadOnlySpan<T> elements);
        
    void Set(ReadOnlySpan<T> elements, uint count);
        
    void Set(ReadOnlySpan<T> elements);
        
    /// <summary>
    /// Set elements into the buffer.
    /// </summary>
    /// <param name="offset">The element offset into the buffer to start setting elements at.</param>
    /// <param name="elements">The elements to set into the buffer.</param>
    /// <param name="count">The number of elements from <see cref="elements"/> to set into the buffer.</param>
    void Set(uint offset, ReadOnlySpan<T> elements, uint count);
        
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