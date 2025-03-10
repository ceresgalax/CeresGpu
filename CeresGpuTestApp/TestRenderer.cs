using System.Numerics;
using CeresGpu.Graphics;
using CeresGpu.Graphics.Shaders;
using SkiaSharp;

namespace CeresGpuTestApp;

public sealed class TestRenderer : IDisposable
{
    private readonly IPipeline<TestShader, TestShader.DefaultVertexBufferLayout> _pipeline;
    private readonly TestShader.DefaultVertexLayoutInstance _shaderInstance;
    private readonly IBuffer<TestShader.Vertex> _vbo;
    private readonly IBuffer<ushort> _indexBuffer;
    private readonly IBuffer<TestShader.VertUniforms> _ubo;
    private readonly ITexture _texture;
        
    public TestRenderer(IRenderer renderer, ShaderManager shaderManager, ReadOnlySpan<Type> passTypes)
    {
        TestShader shader = shaderManager.GetShader<TestShader>();
        PipelineDefinition pipelineDefinition = new PipelineDefinition();
        _pipeline = renderer.CreatePipeline(pipelineDefinition, passTypes, shader, TestShader.DefaultVertexBufferLayout.Instance);
        _shaderInstance = new TestShader.DefaultVertexLayoutInstance(renderer, shader);
            
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
        
        _shaderInstance.VertexBuffers.SetVertex(_vbo);
        _shaderInstance.SetVertUniforms(_ubo);
        _shaderInstance.Settex(_texture);

        Span<ushort> indices = [0, 1, 2];
        _indexBuffer = renderer.CreateStaticBuffer<ushort>(indices.Length);
        _indexBuffer.Set(indices);
    }

    public void Draw(IPass encoder)
    {
        encoder.SetPipeline(_pipeline, _shaderInstance);
        encoder.DrawIndexedUshort(_indexBuffer, 3, 1, 0, 0, 0);
        //encoder.Draw(3, 1, 0, 0);
    }
        
    private void LoadTexture(string path, ITexture texture)
    {
        using Stream stream = GetType().Assembly.GetManifestResourceStream("CeresGpuTestApp.test.png")!;
        //using FileStream stream = new(path, FileMode.Open, FileAccess.Read);
        using SKBitmap bitmap = SKBitmap.Decode(stream);
        texture.Set(bitmap);
    }

    public void Dispose()
    {
        _pipeline.Dispose();
        _shaderInstance.Dispose();
        _vbo.Dispose();
        _indexBuffer.Dispose();
        _ubo.Dispose();
        _texture.Dispose();
    }
}