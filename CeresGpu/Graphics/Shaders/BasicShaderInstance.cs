namespace CeresGpu.Graphics.Shaders;

public class BasicShaderInstance<TShader> : IShaderInstance<TShader> where TShader : IShader
{
    public IShaderInstanceBacking Backing { get; init; }
    
    public BasicShaderInstance(IRenderer renderer, TShader shader)
    {
        Backing = renderer.CreateShaderInstanceBacking(shader);
    }
    
    public void Dispose()
    {
        Backing.Dispose();
    }
    
}