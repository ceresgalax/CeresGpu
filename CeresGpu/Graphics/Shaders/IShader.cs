using System;
using System.IO;

namespace CeresGpu.Graphics.Shaders
{
    public interface IShader : IDisposable
    {
        /// <summary>
        /// Prepare the shader to be used with the given renderer.
        /// This allows the shader to populate information specific to the renderer's implementation
        /// (e.g Metal, Vulkan, etc).
        /// </summary>
        public void Prime(IRenderer renderer);
        
        IShaderBacking? Backing { get; set; }
        Stream? GetShaderResource(string postfix);
        
        /// <summary>
        /// Get the vertex attribute descriptors of this shader. 
        /// The elements in the returned span must correspond exactly with the shader's vertex attribute indices.
        /// For shaders with sparse attribute indices, each unused attribute index should still have an element in the
        /// returned span with a default-initialized descriptor.
        /// </summary>
        ReadOnlySpan<ShaderVertexAttributeDescriptor> VertexAttributeDescriptors { get; }
        
        /// <summary>
        /// Meant for shader introspection. Not called internally by CeresGpu.
        /// </summary>
        ReadOnlySpan<DescriptorInfo> GetDescriptors();
    }
}