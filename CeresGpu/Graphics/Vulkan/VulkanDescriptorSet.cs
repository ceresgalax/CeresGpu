using System;
using System.Collections.Generic;
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

    //private Dictionary<int, (VkDescriptorType type, object resource)> _resourcesByBinding = [];
    private Dictionary<uint, IVulkanBuffer> _uniformBuffersByBinding = [];
    private Dictionary<uint, IVulkanBuffer> _storageBuffersByBinding = [];
    private Dictionary<uint, IVulkanTexture> _texturesByBinding = [];
    private Dictionary<uint, object> _samplersByBinding = [];
    
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

    private uint GetBinding(in DescriptorInfo descriptorInfo)
    {
        return ((VulkanDescriptorBindingInfo)descriptorInfo.Binding).Binding;
    }
    
    public void SetUniformBufferDescriptor<T>(IBuffer<T> buffer, in DescriptorInfo info) where T : unmanaged
    {
        _uniformBuffersByBinding[GetBinding(in info)] = (IVulkanBuffer)buffer;

        // IVulkanBuffer vulkanBuffer = (IVulkanBuffer)buffer;
        // DescriptorBufferInfo bufferInfo = new DescriptorBufferInfo(vulkanBuffer.GetBufferForCurrentFrame(), 0, Vk.WholeSize);
        // WriteDescriptorSet write = new(
        //     sType: StructureType.WriteDescriptorSet,
        //     pNext: null,
        //     dstSet: _descriptorSet,
        //     dstBinding: ((VulkanDescriptorBindingInfo)info.Binding).Binding,
        //     dstArrayElement: 0,
        //     descriptorCount: 1,
        //     descriptorType: VkDescriptorType.UniformBuffer,
        //     pImageInfo: null,
        //     pBufferInfo: &bufferInfo,
        //     pTexelBufferView: null
        // );
        // _renderer.Vk.UpdateDescriptorSets();
    }

    public void SetShaderStorageBufferDescriptor<T>(IBuffer<T> buffer, in DescriptorInfo info) where T : unmanaged
    {
        _storageBuffersByBinding[GetBinding(in info)] = (IVulkanBuffer)buffer;
    }

    public void SetTextureDescriptor(ITexture texture, in DescriptorInfo info)
    {
        _texturesByBinding[GetBinding(in info)] = (IVulkanTexture)texture;
    }

    public void SetSamplerDescriptor(ISampler sampler, in DescriptorInfo info)
    {
        _samplersByBinding[GetBinding(in info)] = sampler;
    }


}