using Metalancer.Graphics.Shaders;

namespace Metalancer.Graphics
{
    public interface IShaderInstance<ShaderT> : IUntypedShaderInstance where ShaderT : IShader
    {
    }
}
