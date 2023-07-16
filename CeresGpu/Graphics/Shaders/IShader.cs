using System;
using System.IO;

namespace CeresGpu.Graphics.Shaders
{
    public interface IShader : IDisposable
    {
        IShaderBacking? Backing { get; set; }
        Stream? GetShaderResource(string postfix);
        ReadOnlySpan<VertexAttributeDescriptor> GetVertexAttributeDescriptors();
        ReadOnlySpan<VertexBufferLayout> GetVertexBufferLayouts();
        
        /// <summary>
        /// Meant for shader introspection. Not called internally by CeresGpu.
        /// </summary>
        ReadOnlySpan<DescriptorInfo> GetDescriptors();
    }
}