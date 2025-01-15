using System;
using System.Runtime.InteropServices;
using CeresGpu.Graphics.Shaders;
using CeresGpu.MetalBinding;

namespace CeresGpu.Graphics.Metal;

public interface IMetalPass
{
    IMetalPass? Prev { get; set; }
    IMetalPass? Next { get; set; }

    void Finish();
    IntPtr CommandBuffer { get; }
}

class MetalPassAnchor : IMetalPass
{
    public IMetalPass? Prev { get; set; }
    public IMetalPass? Next { get; set; }

    public IntPtr CommandBuffer => throw new NotSupportedException();
    public void Finish() => throw new NotSupportedException();

    public void ResetAsFront(MetalPassAnchor endAnchor)
    {
        Next = endAnchor;
        endAnchor.Prev = this;
    }
}

public sealed class MetalPass : IMetalPass, IPass
{
    private readonly MetalRenderer _renderer;
    private readonly IntPtr _commandBuffer; 
    private readonly IntPtr _encoder;
    
    public IntPtr CommandBuffer => _commandBuffer;
        
    public ScissorRect CurrentDynamicScissor { get; private set;  }
    public Viewport CurrentDynamicViewport { get; private set; }
    
    public IMetalPass? Prev { get; set; }
    public IMetalPass? Next { get; set; }

    private IUntypedShaderInstance? _shaderInstance;
    private MetalShaderInstanceBacking? _shaderInstanceBacking;

    public MetalPass(MetalRenderer renderer, MetalPassBacking passBacking, MetalFramebuffer framebuffer)
    {
        _renderer = renderer;
        
        IntPtr passDescriptor = MetalApi.metalbinding_create_render_pass_descriptor();
        try {
            if (passDescriptor == IntPtr.Zero) {
                throw new InvalidOperationException("Failed to create a pass descriptor for the current frame.");
            }

            for (int i = 0; i < passBacking.Definition.ColorAttachments.Length; ++i) {
                ref readonly MetalFramebuffer.ColorAttachment attachment = ref framebuffer.ColorAttachments[i];

                MetalApi.MTLLoadAction metalLoadAction =
                    MetalRenderPassUtil.TranslateLoadAction(passBacking.Definition.ColorAttachments[i].LoadAction);
                MetalApi.metalbinding_set_render_pass_descriptor_color_attachment(
                    passDescriptor,
                    (uint)i,
                    attachment.RenderTarget.GetCurrentFrameDrawable(),
                    metalLoadAction,
                    MetalApi.MTLStoreAction.Store,
                    attachment.ClearColor.X, attachment.ClearColor.Y, attachment.ClearColor.Z, attachment.ClearColor.W
                );
            }

            if (passBacking.Definition.DepthStencilAttachment != null) {
                MetalApi.metalbinding_set_render_pass_descriptor_depth_attachment(
                    passDescriptor,
                    framebuffer.DepthStencilAttachment!.GetCurrentFrameDrawable(),
                    MetalRenderPassUtil.TranslateLoadAction(passBacking.Definition.DepthStencilAttachment.Value
                        .LoadAction),
                    MetalApi.MTLStoreAction.Store,
                    framebuffer.DepthClearValue
                );
                MetalApi.metalbinding_set_render_pass_descriptor_stencil_attachment(
                    passDescriptor,
                    framebuffer.DepthStencilAttachment!.GetCurrentFrameDrawable(),
                    MetalRenderPassUtil.TranslateLoadAction(passBacking.Definition.DepthStencilAttachment.Value
                        .LoadAction),
                    MetalApi.MTLStoreAction.Store,
                    framebuffer.StencilClearValue
                );
            }

            _commandBuffer = MetalApi.metalbinding_create_command_buffer(renderer.Context);
            _encoder = MetalApi.metalbinding_new_command_encoder(_commandBuffer, passDescriptor);
        }
        finally {
            MetalApi.metalbinding_release_render_pass_descriptor(passDescriptor);
        }
    }
        
    private void ReleaseUnmanagedResources()
    {
        if (_encoder != IntPtr.Zero) {
            MetalApi.metalbinding_release_command_encoder(_encoder);
        }

        if (_commandBuffer != IntPtr.Zero) {
            MetalApi.metalbinding_release_command_buffer(_commandBuffer);
        }
    }

    private bool _isDisposed;
    
    public void Dispose()
    {
        if (_isDisposed) {
            return;
        }
        _isDisposed = true;
            
        ReleaseUnmanagedResources();
        GC.SuppressFinalize(this);
    }

    ~MetalPass() {
        ReleaseUnmanagedResources();
    }
    
    public void InsertBefore(IMetalPass other)
    {
        Prev = other.Prev;
        other.Prev = this;
        Next = other;
    }

    public void InsertAfter(IMetalPass other)
    {
        Next = other.Next;
        other.Next = this;
        Prev = other;
    }
    

    private object? _previousPipeline;

    public void SetPipeline<TShader, TVertexBufferLayout>(
        IPipeline<TShader, TVertexBufferLayout> pipeline,
        IShaderInstance<TShader, TVertexBufferLayout> shaderInstance
    )
        where TShader : IShader
        where TVertexBufferLayout : IVertexBufferLayout<TShader> 
    {
        if (pipeline is not MetalPipeline<TShader, TVertexBufferLayout> metalPipeline) {
            throw new ArgumentException("Incompatible pipeline", nameof(pipeline));
        }
        if (shaderInstance.Backing is not MetalShaderInstanceBacking shaderInstanceBacking) {
            throw new ArgumentException("Incompatible shader instance", nameof(shaderInstance));
        }
            
        if (_previousPipeline != pipeline) {
            MetalApi.metalbinding_command_encoder_set_pipeline(_encoder, metalPipeline.Handle);
            _previousPipeline = pipeline;
        }

        for (int i = 0; i < shaderInstanceBacking.Shader.ArgumentBufferDetails.Length; ++i) {
            MetalShaderBacking.ArgumentBufferInfo argBufferInfo = shaderInstanceBacking.Shader.ArgumentBufferDetails[i];

            IMetalBuffer argumentBuffer = shaderInstanceBacking.ArgumentBuffers[i];
            argumentBuffer.PrepareToUpdateExternally();
                
            switch (argBufferInfo.Stage) {
                case ShaderStage.Vertex:
                    MetalApi.metalbinding_command_encoder_set_vertex_buffer(_encoder, argumentBuffer.GetHandleForCurrentFrame(), 0, argBufferInfo.FunctionIndex);
                    break;
                case ShaderStage.Fragment:
                    MetalApi.metalbinding_command_encoder_set_fragment_buffer(_encoder, argumentBuffer.GetHandleForCurrentFrame(), 0, argBufferInfo.FunctionIndex);
                    break;
            }
        }
            
        _shaderInstance = shaderInstance;
        _shaderInstanceBacking = shaderInstanceBacking;
        UpdateShaderInstance();

        MetalApi.MTLCullMode cullMode = metalPipeline.CullMode switch {
            CullMode.None => MetalApi.MTLCullMode.None
            , CullMode.Front => MetalApi.MTLCullMode.Front
            , CullMode.Back => MetalApi.MTLCullMode.Back
            , _ => throw new ArgumentOutOfRangeException()
        };

        MetalApi.metalbinding_command_encoder_set_cull_mode(_encoder, cullMode);
            
        MetalApi.metalbinding_command_encoder_set_dss(_encoder, metalPipeline.DepthStencilState);
    }

    public void RefreshPipeline()
    {
        UpdateShaderInstance();
    }

    public void SetScissor(ScissorRect scissor)
    {
        MetalApi.metalbinding_command_encoder_set_scissor(_encoder, scissor.X, scissor.Y, scissor.Width, scissor.Height);
        CurrentDynamicScissor = scissor;
    }

    public void SetViewport(Viewport viewport)
    {
        MetalApi.metalbinding_command_encoder_set_viewport(_encoder, viewport.X, viewport.Y, viewport.Width, viewport.Height);
        CurrentDynamicViewport = viewport;
    }

    public void Draw(uint vertexCount, uint instanceCount, uint firstVertex, uint firstInstance)
    {
        MetalApi.metalbinding_command_encoder_draw(_encoder, vertexCount, instanceCount, firstVertex, firstInstance);
    }

    public void DrawIndexedUshort(IBuffer<ushort> indexBuffer, uint indexCount, uint instanceCount, uint firstIndex, int vertexOffset, uint firstInstance)
    {
        if (indexBuffer is not IMetalBuffer metalBuffer) {
            throw new ArgumentException("Incompatible buffer", nameof(indexBuffer));
        }

        if (indexCount == 0) {
            return;
        }

        uint indexBufferOffset = (uint)Marshal.SizeOf<ushort>() * firstIndex;
            
        MetalApi.metalbinding_command_encoder_draw_indexed(_encoder, MetalApi.MTLIndexType.UInt16, 
            metalBuffer.GetHandleForCurrentFrame(), indexCount, instanceCount, indexBufferOffset, vertexOffset, firstInstance);
    }

    public void DrawIndexedUint(IBuffer<uint> indexBuffer, uint indexCount, uint instanceCount, uint firstIndex, int vertexOffset, uint firstInstance)
    {
        if (indexBuffer is not IMetalBuffer metalBuffer) {
            throw new ArgumentException("Incompatible buffer", nameof(indexBuffer));
        }
            
        uint indexBufferOffset = (uint)Marshal.SizeOf<uint>() * firstIndex;
            
        MetalApi.metalbinding_command_encoder_draw_indexed(_encoder, MetalApi.MTLIndexType.UInt32, 
            metalBuffer.GetHandleForCurrentFrame(), indexCount, instanceCount, indexBufferOffset, vertexOffset, firstInstance);
    }

    private void UpdateShaderInstance()
    {
        if (_shaderInstanceBacking == null || _shaderInstance == null) {
            throw new InvalidOperationException("No shader instance set. Must call SetPipeline first!");
        }

        ReadOnlySpan<object?> vertexBuffers = _shaderInstance.VertexBufferAdapter.VertexBuffers;
            
        for (int i = 0, ilen = vertexBuffers.Length; i < ilen; ++i) {
            // No vertex buffer set.
            // TODO: log to let the user know they didn't set a vertex buffer.
            object? untypedVertexBuffer = vertexBuffers[i];
            if (untypedVertexBuffer == null) {
                continue;
            }
            if (untypedVertexBuffer is IMetalBuffer buffer) {
                buffer.Commit();
                MetalApi.metalbinding_command_encoder_set_vertex_buffer(_encoder, buffer.GetHandleForCurrentFrame(), 0, MetalBufferTableConstants.INDEX_VERTEX_BUFFER_MAX - (uint)i);    
            } else {
                throw new InvalidOperationException($"Buffer returned by vertex buffer adapter at index {i} is not compatible with MetalPass.");
            }
        }
            
        _shaderInstanceBacking.Update(_encoder);
    }


    private bool _isFinished;
        
    public void Finish()
    {
        if (_isFinished) {
            return;
        }
        _isFinished = true;
            
        MetalApi.metalbinding_command_encoder_end_encoding(_encoder);
    }

}