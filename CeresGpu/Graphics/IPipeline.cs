using System;
using CeresGpu.Graphics.Shaders;

namespace CeresGpu.Graphics
{
    public interface IPipeline<ShaderT> : IDisposable where ShaderT : IShader
    {
    }
}