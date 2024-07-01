namespace CeresGpu.Graphics;

/// <summary>
/// The contents of this buffer are static, and cannot be altered after calling <see cref="IBuffer{T}.Commit"/>.
///
/// These buffers are cheaper memory wise, as memory is not needed to contain the contents of these buffers
/// for in-flight frames.
/// </summary>
public interface IStaticBuffer<T> : IBuffer<T> where T : unmanaged
{
}