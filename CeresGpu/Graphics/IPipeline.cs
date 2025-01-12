using System;
using CeresGpu.Graphics.Shaders;

namespace CeresGpu.Graphics
{
    //public interface IPipeline<TRenderPass, TShader, TVertexBufferLayout> : IDisposable
    public interface IPipeline<TShader, TVertexBufferLayout> : IDisposable
        //where TRenderPass : IRenderPass
        where TShader : IShader
        where TVertexBufferLayout : IVertexBufferLayout<TShader>
    {
    }
}