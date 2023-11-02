using System.Numerics;
using CeresGpu.Graphics.Shaders;

namespace CeresGpu.Graphics
{
    public interface ICommandEncoder
    {
        ScissorRect CurrentDynamicScissor { get; }
        Viewport CurrentDynamicViewport { get; }
        
        void SetPipeline<ShaderT>(IPipeline<ShaderT> pipeline, IShaderInstance<ShaderT> shaderInstance) where ShaderT : IShader;
        void SetScissor(ScissorRect scissor);
        void SetViewport(Viewport viewport);
        void Draw(uint vertexCount, uint instanceCount, uint firstVertex, uint firstInstance);
        void DrawIndexedUshort(IBuffer<ushort> indexBuffer, uint indexCount, uint instanceCount, uint firstIndex, uint vertexOffset, uint firstInstance);
        void DrawIndexedUint(IBuffer<uint> indexBuffer, uint indexCount, uint instanceCount, uint firstIndex, uint vertexOffset, uint firstInstance);

        //void Clear(Viewport rect, Vector4 color);
    }
}