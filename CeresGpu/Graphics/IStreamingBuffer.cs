namespace CeresGpu.Graphics;

/// <summary>
/// The contents of streaming buffers are invalidated every frame.
/// You must re-set the contents of the buffer every time before commands are encoded which use this buffer.
/// In Debug build configuration, an exception will be thrown if you attempt to use a buffer which is not fully re-set.
/// </summary>
public interface IStreamingBuffer<T> : IBuffer<T> where T : unmanaged
{
}