using CeresGpu.Graphics.Shaders;

namespace CeresGpu.Graphics
{
    public interface IShaderInstance<ShaderT> : IUntypedShaderInstance where ShaderT : IShader
    {
    }
}
