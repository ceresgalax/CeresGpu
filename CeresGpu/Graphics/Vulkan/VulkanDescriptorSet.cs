using System;
using CeresGpu.Graphics.Shaders;
using Silk.NET.Vulkan;
using VkDescriptorType = Silk.NET.Vulkan.DescriptorType;

namespace CeresGpu.Graphics.Vulkan;

public sealed class VulkanDescriptorSet : IDescriptorSet
{
    private readonly VulkanRenderer _renderer;
    private readonly VulkanShaderBacking _shaderBacking;
    private readonly DescriptorSet _descriptorSet;
    private readonly DescriptorPool _poolAllocatedFrom;
    private readonly int _setIndex;
    
    
    public VulkanDescriptorSet(VulkanRenderer renderer, VulkanShaderBacking shaderBacking, int setIndex, in DescriptorSetCreationHints hints)
    {
        _renderer = renderer;
        _shaderBacking = shaderBacking;
        _setIndex = setIndex;

        DescriptorSetLayout layout = shaderBacking.GetLayoutForDescriptorSet(setIndex);
        ReadOnlySpan<(VkDescriptorType, int)> descriptorCounts = shaderBacking.GetDescriptorCountsForDescriptorSet(setIndex);

        _descriptorSet = _renderer.DescriptorPoolManager.AllocateDescriptorSet(layout, descriptorCounts, out _poolAllocatedFrom);
    }
    
    private void ReleaseUnmanagedResources()
    {
        if (_renderer.IsDisposed) {
            return;
        }
        _renderer.DescriptorPoolManager.FreeDescriptorSet(_descriptorSet, _poolAllocatedFrom, _shaderBacking.GetDescriptorCountsForDescriptorSet(_setIndex));
    }

    public void Dispose()
    {
        ReleaseUnmanagedResources();
        GC.SuppressFinalize(this);
    }

    ~VulkanDescriptorSet()
    {
        ReleaseUnmanagedResources();
    }
    
    public void SetUniformBufferDescriptor<T>(IBuffer<T> buffer, in DescriptorInfo info) where T : unmanaged
    {
        throw new System.NotImplementedException();
    }

    public void SetShaderStorageBufferDescriptor<T>(IBuffer<T> buffer, in DescriptorInfo info) where T : unmanaged
    {
        throw new System.NotImplementedException();
    }

    public void SetTextureDescriptor(ITexture texture, in DescriptorInfo info)
    {
        throw new System.NotImplementedException();
    }

    public void SetSamplerDescriptor(ISampler sampler, in DescriptorInfo info)
    {
        throw new System.NotImplementedException();
    }


}