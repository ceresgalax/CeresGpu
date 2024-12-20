using System;
using CeresGpu.Graphics.Shaders;

namespace CeresGpu.Graphics;


public struct VblAttributeDescriptor
{
    /// <summary>
    /// The index of the vertex attribute in the shader, specifying the index of the vertex attribute
    /// in <see cref="IShader.VertexAttributeDescriptors"/>.
    /// </summary>
    public uint AttributeIndex;

    /// <summary>
    /// The index of the vertex buffer in the BufferAdapter that vertex data will be pulled from. 
    /// </summary>
    public uint BufferIndex;

    /// <summary>
    /// The offset after the stride that each element of this attribute in the vertex buffer starts at.
    /// </summary>
    public uint BufferOffset;
}

public struct VblBufferDescriptor
{
    /// <summary>
    /// The step function used by the vertex buffer described by this descriptor.
    /// </summary>
    public VertexStepFunction StepFunction;
    
    /// <summary>
    /// The stride between vertex elements in the vertex buffer described by this descriptor.
    /// </summary>
    public uint Stride;
    
    /// <summary>
    /// The Type of structure which has been generated for this vertex buffer this shader accepts.
    /// Intended for use by shader introspection tools.
    ///
    /// Not used by CeresGpu itself. 
    /// </summary>
    public Type? BufferType;
}

/// <summary>
/// Each instantiation of this generic type represents a buffer layout that a pipeline targeting <see cref="TShader"/>
/// can be compatible with.  
/// </summary>
public interface IVertexBufferLayout<TShader> : IVertexBufferLayout where TShader : IShader
{
}

/// <summary>
/// The non-generic interface to a vertex buffer layout which is not tied to a specific shader.
/// Typically used internally within CeresGpu.
/// </summary>
public interface IVertexBufferLayout
{
    ReadOnlySpan<VblBufferDescriptor> BufferDescriptors { get; }
    ReadOnlySpan<VblAttributeDescriptor> AttributeDescriptors { get; }
}
