using CeresGpu.Graphics.Shaders;

namespace CeresGpu.Graphics
{
    public interface IShaderInstance<TShader, TVertexBufferLayout> : IUntypedShaderInstance 
        where TShader : IShader
        where TVertexBufferLayout : IVertexBufferLayout<TShader>
    {
        IVertexBufferAdapter<TShader, TVertexBufferLayout> VertexBuffers { get; }
    }
}
