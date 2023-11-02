using System;
using System.Collections.Generic;
using System.Numerics;
using CeresGpu.Graphics.Shaders;
using CeresGpu.MetalBinding;

namespace CeresGpu.Graphics
{
    public interface IRenderer : IDisposable
    {
        uint UniqueFrameId { get; }
        
        /// <summary>
        /// Static buffers can only be set up once. Once used their contents cannot be changed again.
        /// </summary>
        IBuffer<T> CreateStaticBuffer<T>(int elementCount) where T : unmanaged;
        
        /// <summary>
        /// Streaming buffers can be set every frame.
        /// If a streaming buffer is not set for a frame, the contents from the previous frame will be used.
        /// When a streaming buffer is set, buffer contents from the previous frame will not be retained.
        /// </summary>
        IBuffer<T> CreateStreamingBuffer<T>(int elementCount) where T : unmanaged;

        ITexture CreateTexture();
        IShaderBacking CreateShaderBacking(IShader shader);
        IShaderInstanceBacking CreateShaderInstanceBacking(int vertexBufferCountHint, IShader shader);
        IDescriptorSet CreateDescriptorSet(IShaderBacking shader, ShaderStage stage, int index, in DescriptorSetCreationHints hints);

        IPipeline<ShaderT> CreatePipeline<ShaderT>(PipelineDefinition definition, ShaderT shader) where ShaderT : IShader;
        
        /// <summary>
        /// Create an IPass with the next swapchain texture as the attachments.
        /// </summary>
        /// <param name="clear">
        /// If true, the pass will be configued so that the attachment textures are cleared before rendering.
        /// </param>
        /// <param name="clearColor">
        /// The color that the color attachments will be cleared with if <see cref="clear"/> is true.
        /// </param>
        /// <returns>The created pass.</returns>
        IPass CreateFramebufferPass(LoadAction colorLoadAction, Vector4 clearColor, bool withDepthStencil, double depthClearValue, uint stencilClearValue);
        
        /// <summary>
        /// Create an IPass which renders to the given attachments.
        /// </summary>
        /// <returns>The created pass</returns>
        IPass CreatePass(
            ReadOnlySpan<ColorAttachment> colorAttachments,
            ITexture? depthStencilAttachment,
            LoadAction depthLoadAction,
            double depthClearValue,
            LoadAction stencilLoadAction,
            uint stenclClearValue 
        );

        void Present(float minimumElapsedSeocnds);

        void GetDiagnosticInfo(IList<(string key, object value)> entries);
    }
}