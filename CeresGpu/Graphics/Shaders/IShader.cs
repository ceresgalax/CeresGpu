using System;

namespace Metalancer.Graphics.Shaders
{
    public interface IShader : IDisposable
    {
        IShaderBacking? Backing { get; set; }
        string GetShaderResourcePrefix();
        public ReadOnlySpan<VertexAttributeDescriptor> GetVertexAttributeDescriptors();
        public ReadOnlySpan<VertexBufferLayout> GetVertexBufferLayouts();
    }
}