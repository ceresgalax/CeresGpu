using System;
using System.IO;

namespace CeresGpu.Graphics.Shaders
{
    public interface IShader : IDisposable
    {
        IShaderBacking? Backing { get; set; }
        Stream? GetShaderResource(string postfix);
        
        /// <summary>
        /// Get the vertex attribute descriptors of this shader. 
        /// The elements in the returned span must correspond exactly with the shader's vertex attribute indices.
        /// For shaders with sparse attribute indices, each unused attribute index should still have an element in the
        /// returned span with a default-initialized descriptor.
        /// </summary>
        // TODO: This should be a property - We expect to call this a lot and the implementation shouldn't be doing
        //       anything expensive when returning the descriptors. 
        ReadOnlySpan<ShaderVertexAttributeDescriptor> GetVertexAttributeDescriptors();
        
        /// <summary>
        /// Meant for shader introspection. Not called internally by CeresGpu.
        /// </summary>
        ReadOnlySpan<DescriptorInfo> GetDescriptors();
    }
}