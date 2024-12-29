using CeresGpu.Graphics.Shaders;

namespace CeresGpu.Graphics
{
    public interface IShaderInstanceWithAdapter<TShader, TVertexBufferLayout, out TVertexBufferAdapter>
        : IShaderInstance<TShader, TVertexBufferLayout>
        where TShader : IShader
        where TVertexBufferLayout : IVertexBufferLayout<TShader>
        where TVertexBufferAdapter : IVertexBufferAdapter<TShader, TVertexBufferLayout>
    {
        public TVertexBufferAdapter Adapter { get; }
    }

    public interface IShaderInstance<TShader, TVertexBufferLayout> : IUntypedShaderInstance 
        where TShader : IShader
        where TVertexBufferLayout : IVertexBufferLayout<TShader>
    {
        IVertexBufferAdapter<TShader, TVertexBufferLayout> VertexBuffers { get; }
    }
}
