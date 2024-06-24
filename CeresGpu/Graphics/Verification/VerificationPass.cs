using CeresGpu.Graphics.Shaders;

namespace CeresGpu.Graphics.Verification;

public class VerificationPass : IPass
{
    private readonly IPass _inner;
    
    public VerificationPass(IPass inner)
    {
        _inner = inner;
    }

    public ScissorRect CurrentDynamicScissor => _inner.CurrentDynamicScissor;

    public Viewport CurrentDynamicViewport => _inner.CurrentDynamicViewport;

    public void SetPipeline<ShaderT>(IPipeline<ShaderT> pipeline, IShaderInstance<ShaderT> shaderInstance) where ShaderT : IShader
    {
        _inner.SetPipeline(pipeline, shaderInstance);
    }

    public void SetScissor(ScissorRect scissor)
    {
        _inner.SetScissor(scissor);
    }

    public void SetViewport(Viewport viewport)
    {
        _inner.SetViewport(viewport);
    }

    public void Draw(uint vertexCount, uint instanceCount, uint firstVertex, uint firstInstance)
    {
        _inner.Draw(vertexCount, instanceCount, firstVertex, firstInstance);
    }

    public void DrawIndexedUshort(IBuffer<ushort> indexBuffer, uint indexCount, uint instanceCount, uint firstIndex, uint vertexOffset,
        uint firstInstance)
    {
        _inner.DrawIndexedUshort(indexBuffer, indexCount, instanceCount, firstIndex, vertexOffset, firstInstance);
    }

    public void DrawIndexedUint(IBuffer<uint> indexBuffer, uint indexCount, uint instanceCount, uint firstIndex, uint vertexOffset,
        uint firstInstance)
    {
        _inner.DrawIndexedUint(indexBuffer, indexCount, instanceCount, firstIndex, vertexOffset, firstInstance);
    }

    public void Dispose()
    {
        _inner.Dispose();
    }

    public void Finish()
    {
        _inner.Finish();
    }
}