using System;
using System.Collections.Generic;
using CeresGpu.Graphics.Shaders;

namespace CeresGpu.Graphics;

public abstract class PassEncoder : IPass
{
    protected IUntypedShaderInstance? CurrentShaderInstance;
    
    public ScissorRect CurrentDynamicScissor { get; private set; }
    public Viewport CurrentDynamicViewport { get; private set; }
    
    public void SetPipeline<TShader, TVertexBufferLayout>(
        IPipeline<TShader, TVertexBufferLayout> pipeline,
        IShaderInstance<TShader, TVertexBufferLayout> shaderInstance
    )
        where TShader : IShader
        where TVertexBufferLayout : IVertexBufferLayout<TShader>
    {
        CurrentShaderInstance = shaderInstance;
        CommitBuffers();
        SetPipelineImpl(pipeline, shaderInstance);
    }
    
    protected abstract void SetPipelineImpl<TShader, TVertexBufferLayout>(
        IPipeline<TShader, TVertexBufferLayout> pipeline,
        IShaderInstance<TShader, TVertexBufferLayout> shaderInstance
    )
        where TShader : IShader 
        where TVertexBufferLayout : IVertexBufferLayout<TShader>;

    public void RefreshPipeline()
    {
        CommitBuffers();
        RefreshPipelineImpl();
    }

    protected abstract void RefreshPipelineImpl();

    public void SetScissor(ScissorRect scissor)
    {
        SetScissorImpl(scissor);
        CurrentDynamicScissor = scissor;
    }
    
    protected abstract void SetScissorImpl(ScissorRect scissor);

    public void SetViewport(Viewport viewport)
    {
        SetViewportImpl(viewport);
        CurrentDynamicViewport = viewport;
    }
    
    protected abstract void SetViewportImpl(Viewport viewport);

    public void Draw(uint vertexCount, uint instanceCount, uint firstVertex, uint firstInstance)
    {
        CommitBuffers();
        DrawImpl(vertexCount, instanceCount, firstVertex, firstInstance);
    }
    
    protected abstract void DrawImpl(uint vertexCount, uint instanceCount, uint firstVertex, uint firstInstance);

    public void DrawIndexedUshort(IBuffer<ushort> indexBuffer, uint indexCount, uint instanceCount, uint firstIndex, int vertexOffset, uint firstInstance)
    {
        CommitBuffers();
        CommitBufferOrThrow(indexBuffer);
        DrawIndexedUshortImpl(indexBuffer, indexCount, instanceCount, firstIndex, vertexOffset, firstInstance);
    }
    
    protected abstract void DrawIndexedUshortImpl(IBuffer<ushort> indexBuffer, uint indexCount, uint instanceCount, uint firstIndex, int vertexOffset, uint firstInstance);

    public void DrawIndexedUint(IBuffer<uint> indexBuffer, uint indexCount, uint instanceCount, uint firstIndex, int vertexOffset, uint firstInstance)
    {
        CommitBuffers();
        CommitBufferOrThrow(indexBuffer);
        DrawIndexedUintImpl(indexBuffer, indexCount, instanceCount, firstIndex, vertexOffset, firstInstance);
    }
    
    protected abstract void DrawIndexedUintImpl(IBuffer<uint> indexBuffer, uint indexCount, uint instanceCount, uint firstIndex, int vertexOffset, uint firstInstance);

    private void CommitBuffers()
    {
        if (CurrentShaderInstance == null) {
            throw new InvalidOperationException("No shader instance is set. Must call SetPipeline first!");
        }
        
        // TODO: GC -- Pool these lists and be GC-free!
        List<IBuffer> usedBuffers = [];
        CurrentShaderInstance.Backing.GetUsedBuffers(usedBuffers);

        foreach (IBuffer buffer in usedBuffers) {
            CommitBufferOrThrow(buffer);
        }
        
        foreach (object? untypedBuff in CurrentShaderInstance.VertexBufferAdapter.VertexBuffers) {
            if (untypedBuff is IBuffer buffer) {
                CommitBufferOrThrow(buffer);
            }
        }
    }
    
    private static void CommitBufferOrThrow(IBuffer buffer)
    {
        if (!buffer.Commit()) {
            throw new InvalidOperationException("Failed to commit buffer. This likely means that a streaming buffer did not have it's contents set this frame before being encoding by this pass encoder.");
        }
    }
    
}