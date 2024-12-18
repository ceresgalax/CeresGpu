using CeresGpu.Graphics.Shaders;

namespace CeresGpu.Graphics
{
    public interface ICommandEncoder
    {
        ScissorRect CurrentDynamicScissor { get; }
        Viewport CurrentDynamicViewport { get; }
        
        void SetPipeline<TShader, TVertexBufferLayout>(
            IPipeline<TShader, TVertexBufferLayout> pipeline,
            IShaderInstance<TShader, TVertexBufferLayout> shaderInstance
        )
            where TShader : IShader
            where TVertexBufferLayout : IVertexBufferLayout<TShader>;
        
        void SetScissor(ScissorRect scissor);
        void SetViewport(Viewport viewport);
        void Draw(uint vertexCount, uint instanceCount, uint firstVertex, uint firstInstance);
        void DrawIndexedUshort(IBuffer<ushort> indexBuffer, uint indexCount, uint instanceCount, uint firstIndex, uint vertexOffset, uint firstInstance);
        void DrawIndexedUint(IBuffer<uint> indexBuffer, uint indexCount, uint instanceCount, uint firstIndex, uint vertexOffset, uint firstInstance);

        //void Clear(Viewport rect, Vector4 color);
    }
}