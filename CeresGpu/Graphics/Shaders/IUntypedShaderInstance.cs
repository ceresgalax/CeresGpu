using System;

namespace CeresGpu.Graphics.Shaders
{
    public interface IUntypedShaderInstance : IDisposable
    {
        IShaderInstanceBacking Backing { get; }
        IUntypedVertexBufferAdapter VertexBufferAdapter { get; }
    }
}