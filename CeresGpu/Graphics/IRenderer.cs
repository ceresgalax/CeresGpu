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

        bool IsPassRegistered<TRenderPass>() where TRenderPass : IRenderPass;
        void RegisterPassType<TRenderPass>(RenderPassDefinition definition) where TRenderPass : IRenderPass;
        
        IPipeline<TShader, TVertexBufferLayout> CreatePipeline<TShader, TVertexBufferLayout>(
            PipelineDefinition definition,
            ReadOnlySpan<Type> supportedRenderPasses,
            TShader shader,
            TVertexBufferLayout vertexBufferLayout
        )
            where TShader : IShader
            where TVertexBufferLayout : IVertexBufferLayout<TShader>;

        IFramebuffer CreateFramebuffer<TRenderPass>(ReadOnlySpan<IRenderTarget> colorAttachments, IRenderTarget? depthStencilAttachment)
            where TRenderPass : IRenderPass;

        IRenderTarget CreateRenderTarget(ColorFormat format, bool matchSwapchainSize, uint width, uint height);
        IRenderTarget CreateRenderTarget(DepthStencilFormat format, bool matchSwapchainSize, uint width, uint height);
        IRenderTarget GetSwapchainColorTarget();
        
        // TODO: Rename IPass to something else? Like IPassEncoder?
        /// <summary>
        /// Create a pass which renders to the given attachments.
        /// </summary>
        /// <returns>The created pass</returns>
        IPass CreatePassEncoder<TRenderPass>(TRenderPass pass, IPass? occursBefore = null)
            where TRenderPass : IRenderPass;

        void Present(float minimumElapsedSeocnds);

        void GetDiagnosticInfo(IList<(string key, object value)> entries);
    }
}