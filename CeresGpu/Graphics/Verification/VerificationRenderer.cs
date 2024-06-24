using System;
using System.Collections.Generic;
using System.Numerics;
using CeresGpu.Graphics.Shaders;

namespace CeresGpu.Graphics.Verification;

public class VerificationRenderer : IRenderer
{
    private readonly IRenderer _renderer;
    
    public VerificationRenderer(IRenderer renderer)
    {
        _renderer = renderer;
    }
    
    public void Dispose()
    {
        _renderer.Dispose();
    }

    public uint UniqueFrameId => _renderer.UniqueFrameId;

    public IBuffer<T> CreateStaticBuffer<T>(int elementCount) where T : unmanaged
    {
        IBuffer<T> buffer = _renderer.CreateStaticBuffer<T>(elementCount);
        return buffer;
    }

    public IBuffer<T> CreateStreamingBuffer<T>(int elementCount) where T : unmanaged
    {
        IBuffer<T> buffer = _renderer.CreateStreamingBuffer<T>(elementCount);
        return new VerificationStreamingBuffer<T>(buffer);
    }

    public ITexture CreateTexture()
    {
        return _renderer.CreateTexture();
    }

    public ISampler CreateSampler(in SamplerDescription description)
    {
        return _renderer.CreateSampler(in description);
    }

    public IShaderBacking CreateShaderBacking(IShader shader)
    {
        return _renderer.CreateShaderBacking(shader);
    }

    public IShaderInstanceBacking CreateShaderInstanceBacking(int vertexBufferCountHint, IShader shader)
    {
        return _renderer.CreateShaderInstanceBacking(vertexBufferCountHint, shader);
    }

    public IDescriptorSet CreateDescriptorSet(IShaderBacking shader, ShaderStage stage, int index,
        in DescriptorSetCreationHints hints)
    {
        return _renderer.CreateDescriptorSet(shader, stage, index, in hints);
    }

    public IPipeline<ShaderT> CreatePipeline<ShaderT>(PipelineDefinition definition, ShaderT shader) where ShaderT : IShader
    {
        return _renderer.CreatePipeline(definition, shader);
    }

    public IPass CreateFramebufferPass(LoadAction colorLoadAction, Vector4 clearColor, bool withDepthStencil, double depthClearValue, uint stencilClearValue)
    {
        return _renderer.CreateFramebufferPass(colorLoadAction, clearColor, withDepthStencil, depthClearValue, stencilClearValue);
    }

    public IPass CreatePass(ReadOnlySpan<ColorAttachment> colorAttachments, ITexture? depthStencilAttachment, LoadAction depthLoadAction,
        double depthClearValue, LoadAction stencilLoadAction, uint stenclClearValue)
    {
        return _renderer.CreatePass(colorAttachments, depthStencilAttachment, depthLoadAction, depthClearValue, stencilLoadAction, stenclClearValue);
    }

    public void Present(float minimumElapsedSeocnds)
    {
        _renderer.Present(minimumElapsedSeocnds);
    }

    public void GetDiagnosticInfo(IList<(string key, object value)> entries)
    {
        _renderer.GetDiagnosticInfo(entries);
    }
}