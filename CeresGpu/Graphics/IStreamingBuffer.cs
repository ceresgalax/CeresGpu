using System;

namespace CeresGpu.Graphics;

/// <summary>
/// The contents of streaming buffers are invalidated every frame.
/// You must re-set the contents of the buffer every time before commands are encoded which use this buffer.
/// In Debug build configuration, an exception will be thrown if you attempt to use a buffer which is not fully re-set.
/// ^ TODO: This statement is not true anymore. Now each frame, the 'head' offset resets, and attempting to encode a command
///         which reads past the head offset will throw an exception. (And of course, the head offset cannot be changed after 'commiting' the buffer for use in encoding for the frame)
/// </summary>
public interface IStreamingBuffer<T> : IBuffer<T> where T : unmanaged
{
    uint NumElementsThisFrame { get; }

    void Reset();
    
    /// <summary>
    /// Reset the elements in this buffer.
    /// This will reset any existing elements set into the buffer this frame.
    /// </summary>
    /// <param name="elements">
    /// The elements to set into the buffer. The first <see cref="count"/> elements will be copied from this span.
    /// The buffer must be allocated to contain at least <see cref="count"/> elements.
    /// </param>
    /// <param name="count">The number of elements from <see cref="elements"/> to copy into the buffer.</param>
    void Set(ReadOnlySpan<T> elements, uint count);
        
    void Set(ReadOnlySpan<T> elements);
        
    /// <summary>
    /// Reset the contents of the buffer for this frame with a single element.
    /// </summary>
    /// <param name="element">The element to set into the very beginning of the buffer.</param>
    void Set(in T element);
    
    public delegate void DirectSetter(Span<T> elements);
        
    /// <summary>
    /// Reset the elements in this buffer using the given function.
    /// </summary>
    void SetDirect(DirectSetter setter, uint count);

    void Add(in T element);
    void Add(ReadOnlySpan<T> elements);
    void Add(ReadOnlySpan<T> elements, uint count);


}