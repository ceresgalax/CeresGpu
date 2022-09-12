using System;
using Metalancer.Graphics.Shaders;

namespace Metalancer.Graphics
{
    public interface IPipeline<ShaderT> : IDisposable where ShaderT : IShader
    {
    }
}