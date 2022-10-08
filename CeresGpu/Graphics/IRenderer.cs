using System;
using System.Collections.Generic;
using System.Numerics;
using Metalancer.Graphics.Shaders;

namespace Metalancer.Graphics
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
        
        IPass CreateFramebufferPass(bool clear, Vector4 clearColor);

        void Present(float minimumElapsedSeocnds);

        void GetDiagnosticInfo(IList<(string key, object value)> entries);
    }
}