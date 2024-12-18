using System;
using System.Numerics;
using Metalancer.Renderers;

namespace CeresGpu.Graphics.Metal.Clearing
{
    // TODO: Get the auto-generated default layout from the shader.
    class ClearRendererVertexBufferLayout : IVertexBufferLayout<ClearShader>
    {
        public ReadOnlySpan<VblBufferDescriptor> BufferDescriptors => throw new NotImplementedException();
        public ReadOnlySpan<VblAttributeDescriptor> AttributeDescriptors => throw new NotImplementedException();
    };
    
    public sealed class ClearRenderer : IDisposable, Pool<ClearRenderer.Resources>.IFactory
    {
        private readonly IRenderer _renderer;
        private readonly IPipeline<ClearShader, ClearRendererVertexBufferLayout> _pipeline;
        private readonly ClearShader _shader = new();
        private readonly Pool<Resources> _pool;
        
        public struct Resources
        {
            public ClearShader.Instance ShaderInstance;
            public IBuffer<ClearShader.FragUniforms> UniformBuffer;
        }
        
        public ClearRenderer(IRenderer renderer)
        {
            PipelineDefinition def = new PipelineDefinition();
            
            _renderer = renderer;
            _shader.Backing = renderer.CreateShaderBacking(_shader);
            _pipeline = renderer.CreatePipeline(def, _shader, new ClearRendererVertexBufferLayout());
            _pool = new Pool<Resources>(this);
        }

        public Resources Make()
        {
            return new Resources {
                ShaderInstance = new ClearShader.Instance(_renderer, _shader),
                UniformBuffer = _renderer.CreateStreamingBuffer<ClearShader.FragUniforms>(1)
            };
        }

        public void DisposeOf(Resources item)
        {
            item.ShaderInstance.Dispose();
            item.UniformBuffer.Dispose();
        }

        public void Dispose()
        {
            _pool.Dispose();
            _shader.Dispose();
        }

        public void NewFrame()
        {
            _pool.Reset();
        }
        
        public void Clear(ICommandEncoder encoder, Viewport rect, Vector4 color)
        {
            Resources resources = _pool.Get();
            resources.UniformBuffer.Set(new ClearShader.FragUniforms {
                clearColor = color
            });
            encoder.SetPipeline(_pipeline, resources.ShaderInstance);
            encoder.SetViewport(rect);
            encoder.Draw(6, 1, 0, 0);
        }
    }
}