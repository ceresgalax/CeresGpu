using System;
using System.Collections.Generic;
using CeresGL;
using CeresGpu.Graphics.Shaders;
using CeresGpu.Graphics.Vulkan;
using Silk.NET.Vulkan;

namespace CeresGpu.Graphics.OpenGL;

public sealed class GLShaderInstanceBacking : IShaderInstanceBacking
{
    private readonly GLRenderer _renderer;
    private readonly IShader _shader;
        
    private readonly VertexArray[] _vaos;
        
    private readonly Dictionary<GLDescriptorBindingInfo, IGLBuffer> _uniformBuffersByBinding = [];
    private readonly Dictionary<GLDescriptorBindingInfo, IGLBuffer> _storageBuffersByBinding = [];
    private readonly Dictionary<GLDescriptorBindingInfo, GLTexture> _texturesByBinding = [];
    private readonly Dictionary<GLDescriptorBindingInfo, GLSampler> _samplersByBinding = [];
        
    public GLShaderInstanceBacking(GLRenderer renderer, IShader shader)
    {
        _renderer = renderer;
        _shader = shader;
            
        IGLProvider provider = renderer.GLProvider;
            
        _vaos = new VertexArray[renderer.WorkingFrameCount];
        for (int i = 0, ilen = _vaos.Length; i < ilen; ++i) {
            _vaos[i] = new VertexArray(provider);
        }
    }

    public void PrepareAndBindVertexArrayObject(IVertexBufferLayout layout, IUntypedVertexBufferAdapter adapter)
    {
        VertexArray vao = _vaos[_renderer.WorkingFrame];
            
        // Note: This will throw a cast exception if any of the buffers are not a GLBuffer.
        // Which is correct, mixing buffers meant for different renderer types is bad.
        // TODO: However, maybe we could surface this issue a bit more gracefully?
        foreach (IGLBuffer? buffer in adapter.VertexBuffers) {
            buffer?.Commit();
        }

        vao.RecreateIfNecesaryAndBind(_shader, layout, adapter);
    }

    public void Dispose()
    {
        foreach (VertexArray vao in _vaos) {
            vao.Dispose();
        }
        _shader.Dispose();
    }
        
    private GLDescriptorBindingInfo GetBinding(in DescriptorInfo descriptorInfo)
    {
        return (GLDescriptorBindingInfo)descriptorInfo.Binding;
    }
    
    public void SetUniformBufferDescriptor<T>(IBuffer<T> buffer, in DescriptorInfo info) where T : unmanaged
    {
        _uniformBuffersByBinding[GetBinding(in info)] = (IGLBuffer)buffer;
    }

    public void SetShaderStorageBufferDescriptor<T>(IBuffer<T> buffer, in DescriptorInfo info) where T : unmanaged
    {
        _storageBuffersByBinding[GetBinding(in info)] = (IGLBuffer)buffer;
    }

    public void SetTextureDescriptor(ITexture texture, in DescriptorInfo info)
    {
        _texturesByBinding[GetBinding(in info)] = (GLTexture)texture;
    }

    public void SetSamplerDescriptor(ISampler sampler, in DescriptorInfo info)
    {
        _samplersByBinding[GetBinding(in info)] = (GLSampler)sampler;
    }
    
    public void UpdateBoundVao()
    {
        GL gl = _renderer.GLProvider.Gl;
        
        // TODO: Iterating over these dictionaries probably generates garbage.
            
        foreach ((GLDescriptorBindingInfo binding, IGLBuffer buffer) in _uniformBuffersByBinding) {
            buffer.Commit();
            gl.BindBufferBase(BufferTargetARB.UNIFORM_BUFFER, binding.Location, buffer.GetHandleForCurrentFrame());    
        }
            
        foreach ((GLDescriptorBindingInfo binding, IGLBuffer buffer) in _storageBuffersByBinding) {
            buffer.Commit();
            gl.BindBufferBase(BufferTargetARB.SHADER_STORAGE_BUFFER, binding.Location, buffer.GetHandleForCurrentFrame());
        }
            
        foreach ((GLDescriptorBindingInfo binding, GLTexture texture) in _texturesByBinding) {
            if (!_samplersByBinding.TryGetValue(binding, out GLSampler? sampler)) {
                sampler = _renderer.FallbackSampler;
            }

            gl.ActiveTexture((TextureUnit)((uint)TextureUnit.TEXTURE0 + binding.Location));
            gl.Uniform1ui((int)binding.Location, binding.Location);
            
            if (texture.Handle != 0) {
                gl.BindTexture(TextureTarget.TEXTURE_2D, texture.Handle);
            }
            else {
                gl.BindTexture(TextureTarget.TEXTURE_2D, _renderer.FallbackTexture.Handle);
            }

            gl.BindSampler(binding.Location, sampler.Handle);
        }
            
    }
        
}