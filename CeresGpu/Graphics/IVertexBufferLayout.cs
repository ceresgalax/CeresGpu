using System;
using CeresGpu.Graphics.Shaders;

namespace CeresGpu.Graphics;


public struct VblAttributeDescriptor
{
    /// <summary>
    /// The index of the vertex attribute in the shader. 
    /// </summary>
    public int AttributeIndex;

    /// <summary>
    /// The index of the vertex buffer in the BufferAdapter that vertex data will be pulled from. we will use from the BufferAdapter 
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
/// <typeparam name="TShader"></typeparam>
public interface IVertexBufferLayout<TShader> where TShader : IShader
{
    ReadOnlySpan<VblBufferDescriptor> BufferDescriptors { get; }
    ReadOnlySpan<VblAttributeDescriptor> AttributeDescriptors { get; }
}