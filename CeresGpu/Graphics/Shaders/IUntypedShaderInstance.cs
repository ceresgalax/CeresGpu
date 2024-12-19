using System;

namespace CeresGpu.Graphics.Shaders
{
    public interface IUntypedShaderInstance : IDisposable
    {
        IShaderInstanceBacking Backing { get; }
        ReadOnlySpan<IDescriptorSet> GetDescriptorSets();
        IUntypedVertexBufferAdapter VertexBufferAdapter { get; }
    }
}