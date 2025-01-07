using System;
using System.Collections.Generic;
using CeresGpu.Graphics.Shaders;

namespace CeresGpu.Graphics
{
    public interface IRenderer : IDisposable
    {
        uint UniqueFrameId { get; }
        
        /// <summary>
        /// Static buffers can only be set up once. Once used their contents cannot be changed again.
        /// </summary>
        IStaticBuffer<T> CreateStaticBuffer<T>(int elementCount) where T : unmanaged;
        
        /// <summary>
        /// Streaming buffers can be set every frame.
        /// If a streaming buffer is not set for a frame, the contents from the previous frame will be used.
        /// When a streaming buffer is set, buffer contents from the previous frame will not be retained.
        /// </summary>
        IStreamingBuffer<T> CreateStreamingBuffer<T>(int elementCount) where T : unmanaged;

        ITexture CreateTexture();
        ISampler CreateSampler(in SamplerDescription description);
        IShaderBacking CreateShaderBacking(IShader shader);
        IShaderInstanceBacking CreateShaderInstanceBacking(IShader shader);
        IDescriptorSet CreateDescriptorSet(IShaderBacking shader, ShaderStage stage, int index, in DescriptorSetCreationHints hints);

        void RegisterPassType<TRenderPass>(RenderPassDefinition definition) where TRenderPass : IRenderPass;
        
        IPipeline<TRenderPass, TShader, TVertexBufferLayout> CreatePipeline<TRenderPass, TShader, TVertexBufferLayout>(
            PipelineDefinition definition,
            TShader shader,
            TVertexBufferLayout vertexBufferLayout
        )
            where TRenderPass : IRenderPass
            where TShader : IShader
            where TVertexBufferLayout : IVertexBufferLayout<TShader>;

        IMutableFramebuffer CreateFramebuffer<TRenderPass>() where TRenderPass : IRenderPass;

        IRenderTarget CreateRenderTarget(ColorFormat format, uint width, uint height);
        IRenderTarget CreateRenderTarget(DepthStencilFormat format, uint width, uint height);
        IRenderTarget GetSwapchainColorTarget();
        
        // TODO: Rename IPass to something else? Like IPassEncoder?
        /// <summary>
        /// Create a pass which renders to the given attachments.
        /// </summary>
        /// <returns>The created pass</returns>
        IPass<TRenderPass> CreatePassEncoder<TRenderPass>(
            ReadOnlySpan<IPass> dependentPasses,
            TRenderPass pass

            // All of this is retrieved from the TRenderPass instance:
            // ReadOnlySpan<ColorAttachment> colorAttachments,
            // ITexture? depthStencilAttachment,
            // LoadAction depthLoadAction,
            // double depthClearValue,
            // LoadAction stencilLoadAction,
            // uint stenclClearValue 
        )
            where TRenderPass : IRenderPass;

        void Present(float minimumElapsedSeocnds);

        void GetDiagnosticInfo(IList<(string key, object value)> entries);
    }
}