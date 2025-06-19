using System;

namespace CeresGpu.Graphics;

public interface IStreamingBuffer : IBuffer
{
}

/// <summary>
/// The contents of streaming buffers are invalidated every frame.
/// You must re-set the contents of the buffer every time before commands are encoded which use this buffer.
/// </summary>
public interface IStreamingBuffer<T> : IStreamingBuffer, IBuffer<T> where T : unmanaged
{
}