using System;
using System.Collections.Generic;
using CeresGpu.Graphics.Shaders;

namespace CeresGpu.Graphics;

public interface IShaderInstanceBacking : IDisposable
{
    void SetUniformBufferDescriptor<T>(IBuffer<T> buffer, in DescriptorInfo info) where T : unmanaged;
    void SetShaderStorageBufferDescriptor<T>(IBuffer<T> buffer, in DescriptorInfo info) where T : unmanaged;
    void SetTextureDescriptor(ITexture texture, in DescriptorInfo info);
    void SetSamplerDescriptor(ISampler sampler, in DescriptorInfo info);
    
    void GetUsedBuffers(List<IBuffer> outBuffers);
}