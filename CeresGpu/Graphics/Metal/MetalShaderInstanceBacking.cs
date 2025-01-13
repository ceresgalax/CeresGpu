using System;
using System.Collections.Generic;
using CeresGpu.Graphics.Shaders;
using CeresGpu.MetalBinding;

namespace CeresGpu.Graphics.Metal;

public sealed class MetalShaderInstanceBacking : IShaderInstanceBacking
{
    private readonly MetalRenderer _renderer;
    public readonly MetalShaderBacking Shader;
        
    public readonly IMetalBuffer[] ArgumentBuffers;
    private readonly IntPtr[] _argumentEncoders;
    private readonly uint[] _argumentBufferSizes;
        
    private readonly Dictionary<MetalDescriptorBindingInfo, IMetalBuffer> _uniformBuffersByBinding = [];
    private readonly Dictionary<MetalDescriptorBindingInfo, IMetalBuffer> _storageBuffersByBinding = [];
    private readonly Dictionary<MetalDescriptorBindingInfo, MetalTexture> _texturesByBinding = [];
    private readonly Dictionary<MetalDescriptorBindingInfo, MetalSampler> _samplersByBinding = [];
        
    public MetalShaderInstanceBacking(MetalRenderer renderer, MetalShaderBacking shader)
    {
        _renderer = renderer;
        Shader = shader;
            
        ArgumentBuffers = new IMetalBuffer[shader.ArgumentBufferDetails.Length];
        _argumentEncoders = new IntPtr[shader.ArgumentBufferDetails.Length];
        _argumentBufferSizes = new uint[shader.ArgumentBufferDetails.Length];
            
        for (int abstractedBufferIndex = 0; abstractedBufferIndex < shader.ArgumentBufferDetails.Length; ++abstractedBufferIndex) {
            MetalShaderBacking.ArgumentBufferInfo bufferInfo = shader.ArgumentBufferDetails[abstractedBufferIndex];
                
            IntPtr function = bufferInfo.Stage == ShaderStage.Vertex ? shader.VertexFunction : shader.FragmentFunction;
            IntPtr argumentEncoder = MetalApi.metalbinding_new_argument_encoder(function, bufferInfo.FunctionIndex);
            uint bufferSize = MetalApi.metalbinding_get_argument_buffer_size(argumentEncoder);
                
            MetalStreamingBuffer<byte> buffer = new MetalStreamingBuffer<byte>(renderer);
            buffer.Allocate(bufferSize);
                
            ArgumentBuffers[abstractedBufferIndex] = buffer;
            _argumentEncoders[abstractedBufferIndex] = argumentEncoder;
            _argumentBufferSizes[abstractedBufferIndex] = bufferSize;
        }
    }

    private MetalDescriptorBindingInfo GetBinding(in DescriptorInfo info)
    {
        return (MetalDescriptorBindingInfo)info.Binding;
    }
        
    public void SetUniformBufferDescriptor<T>(IBuffer<T> buffer, in DescriptorInfo info) where T : unmanaged
    {
        _uniformBuffersByBinding[GetBinding(in info)] = (IMetalBuffer)buffer;
    }

    public void SetShaderStorageBufferDescriptor<T>(IBuffer<T> buffer, in DescriptorInfo info) where T : unmanaged
    {
        _storageBuffersByBinding[GetBinding(in info)] = (IMetalBuffer)buffer;
    }

    public void SetTextureDescriptor(ITexture texture, in DescriptorInfo info)
    {
        _texturesByBinding[GetBinding(in info)] = (MetalTexture)texture;
    }

    public void SetSamplerDescriptor(ISampler sampler, in DescriptorInfo info)
    {
        _samplersByBinding[GetBinding(in info)] = (MetalSampler)sampler;
    }
    
    public void Update(IntPtr renderCommandEncoder)
    {
        // TODO: Iterating over these dictionaries probably generates garbage.
        
        foreach ((MetalDescriptorBindingInfo binding, IMetalBuffer buffer) in _uniformBuffersByBinding) {
            IntPtr encoder = _argumentEncoders[binding.AbstractedBufferIndex];
            uint stages = binding.Stage == ShaderStage.Vertex ? 0b01u : 0b10u;
            
            buffer.Commit();
            MetalApi.metalbinding_encode_buffer_argument(encoder, renderCommandEncoder, buffer.GetHandleForCurrentFrame(), 0, binding.FunctionArgumentBufferIndex, stages);
        }
        
        foreach ((MetalDescriptorBindingInfo binding, IMetalBuffer buffer) in _storageBuffersByBinding) {
            IntPtr encoder = _argumentEncoders[binding.AbstractedBufferIndex];
            uint stages = binding.Stage == ShaderStage.Vertex ? 0b01u : 0b10u;
            
            buffer.Commit();
            MetalApi.metalbinding_encode_buffer_argument(encoder, renderCommandEncoder, buffer.GetHandleForCurrentFrame(), 0, binding.FunctionArgumentBufferIndex, stages);
        }
        
        foreach ((MetalDescriptorBindingInfo binding, MetalTexture texture) in _texturesByBinding) {
            IntPtr encoder = _argumentEncoders[binding.AbstractedBufferIndex];
            uint stages = binding.Stage == ShaderStage.Vertex ? 0b01u : 0b10u;
            
            if (!_samplersByBinding.TryGetValue(binding, out MetalSampler? sampler)) {
                sampler = _renderer.FallbackSampler;
            }

            MetalApi.metalbinding_encode_sampler_argument(encoder, sampler.Handle, binding.SamplerBufferId);
            MetalApi.metalbinding_encode_texture_argument(encoder, renderCommandEncoder, texture.Handle, binding.FunctionArgumentBufferIndex, stages);
        }
        
    }


    private void ReleaseUnmanagedResources()
    {
        foreach (IntPtr argumentEncoder in _argumentEncoders) {
            MetalApi.metalbinding_release_argument_encoder(argumentEncoder);
        }
    }

    private void Dispose(bool disposing)
    {
        
        if (disposing) {
            _renderer.Dispose();
        }
    }

    public void Dispose()
    {
        ReleaseUnmanagedResources();
        GC.SuppressFinalize(this);
        
        foreach (IMetalBuffer argumentBuffer in ArgumentBuffers) {
            argumentBuffer.Dispose();
        }
    }

    ~MetalShaderInstanceBacking()
    {
        Dispose(false);
    }
}