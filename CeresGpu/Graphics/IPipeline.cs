using System;
using CeresGpu.Graphics.Shaders;

namespace CeresGpu.Graphics
{
    public interface IPipeline<TShader, TVertexBufferLayout> : IDisposable 
        where TShader : IShader
        where TVertexBufferLayout : IVertexBufferLayout<TShader>
    {
    }
}