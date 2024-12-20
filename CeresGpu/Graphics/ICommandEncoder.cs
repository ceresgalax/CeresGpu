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

        /// <summary>
        /// Encode any changes made to the active shader instance set by
        /// <see cref="SetPipeline{TShader,TVertexBufferLayout}"/>.
        /// Only values set directly to the shader instance need to be re-encoded by this method, such as setting
        /// different buffer references to the shader instance.
        /// </summary>
        // TODO: Accept some flags to allow specifying which resources of the shader instance need to be refreshed.
        //       (like vertex buffers, buffers, etc..)
        void RefreshPipeline();
        
        void SetScissor(ScissorRect scissor);
        void SetViewport(Viewport viewport);
        void Draw(uint vertexCount, uint instanceCount, uint firstVertex, uint firstInstance);
        void DrawIndexedUshort(IBuffer<ushort> indexBuffer, uint indexCount, uint instanceCount, uint firstIndex, uint vertexOffset, uint firstInstance);
        void DrawIndexedUint(IBuffer<uint> indexBuffer, uint indexCount, uint instanceCount, uint firstIndex, uint vertexOffset, uint firstInstance);
    }
}