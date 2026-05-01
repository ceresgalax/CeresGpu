using CeresGpu.Graphics.Shaders;

namespace CeresGpu.Graphics;

public interface IShaderInstance<TShader> : IUntypedShaderInstance 
    where TShader : IShader
{
}