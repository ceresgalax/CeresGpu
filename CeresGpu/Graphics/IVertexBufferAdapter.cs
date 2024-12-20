using System;
using CeresGpu.Graphics.Shaders;

namespace CeresGpu.Graphics;

public interface IUntypedVertexBufferAdapter
{
    // TODO: This is meant to return any IBuffer<T> without type info. 
    //    Buffer<T> doesn't implement an untyped interface because I want to make it hard to mix and match usage of the
    //    typed and untyped buffer manipulation. However, maybe that's not necesary, and there could be benefits of
    //    allowing the typed and untyped interfaces to coexist?
    ReadOnlySpan<object?> VertexBuffers { get; }
}

/// <summary>
/// Strongly typed interface to a vertex buffer adpater. This strongly typed interface can be used to ensure the vertex
/// buffers returned by the adapter conform with the buffer layout <see cref="TLayout"/>.
/// </summary>
/// <typeparam name="TShader"></typeparam>
/// <typeparam name="TLayout"></typeparam>
public interface IVertexBufferAdapter<TShader, TLayout> : IUntypedVertexBufferAdapter
    where TShader : IShader
    where TLayout : IVertexBufferLayout<TShader>
{
}