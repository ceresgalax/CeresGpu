using System;
using System.Drawing;
using System.IO;
using System.Numerics;
using Metalancer.Graphics.Shaders;
using Metalancer.Renderers;

namespace Metalancer.Graphics.Test
{
    public class TestRenderer
    {
        private IPipeline<TestShader> _pipeline;
        private TestShader.Instance _shaderInstance;
        private IBuffer<TestShader.Vertex> _vbo;
        private IBuffer<ushort> _indexBuffer;
        private IBuffer<TestShader.VertUniforms> _ubo;
        private ITexture _texture;
        
        public TestRenderer(IRenderer renderer, ShaderManager shaderManager)
        {
            TestShader shader = shaderManager.GetShader<TestShader>();
            PipelineDefinition pipelineDefinition = new PipelineDefinition();
            _pipeline = renderer.CreatePipeline(pipelineDefinition, shader);
            _shaderInstance = new TestShader.Instance(renderer, shader);
            
            Span<TestShader.Vertex> verts = stackalloc TestShader.Vertex[] {
                new() {vert_pos = new Vector2(-1f, -1f)},
                new() {vert_pos = new Vector2(0f, 1f)},
                new() {vert_pos = new Vector2(1f, -1f)}
            };
            _vbo = renderer.CreateStaticBuffer<TestShader.Vertex>(verts.Length);
            _vbo.Set(verts);

            _ubo = renderer.CreateStaticBuffer<TestShader.VertUniforms>(1);
            Span<TestShader.VertUniforms> uniforms = stackalloc TestShader.VertUniforms[] {
                new() { scale = 2f }
            };
            _ubo.Set(uniforms);

            _texture = renderer.CreateTexture();
            LoadTexture("test.png", _texture);

            _shaderInstance.SetVertex(_vbo);
            _shaderInstance.SetVertUniforms(_ubo);
            _shaderInstance.Settex(_texture);

            Span<ushort> indices = stackalloc ushort[] { 0, 1, 2 };
            _indexBuffer = renderer.CreateStaticBuffer<ushort>(indices.Length);
            _indexBuffer.Set(indices);
        }

        public void Draw(ICommandEncoder encoder)
        {
            encoder.SetPipeline(_pipeline, _shaderInstance);
            encoder.DrawIndexedUshort(_indexBuffer, 3, 1, 0, 0, 0);
            //encoder.Draw(3, 1, 0, 0);
        }
        
        private void LoadTexture(string path, ITexture texture)
        {
            using FileStream stream = new(path, FileMode.Open, FileAccess.Read);
            using Bitmap bitmap = new Bitmap(stream);
            texture.Set(bitmap);
        }
    }
}