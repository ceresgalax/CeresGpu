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
    public readonly DescriptorSet DescriptorSet;
    private readonly DescriptorPool _poolAllocatedFrom;
    private readonly int _setIndex;

    //private Dictionary<int, (VkDescriptorType type, object resource)> _resourcesByBinding = [];
    private Dictionary<uint, IVulkanBuffer> _uniformBuffersByBinding = [];
    private Dictionary<uint, IVulkanBuffer> _storageBuffersByBinding = [];
    private Dictionary<uint, IVulkanTexture> _texturesByBinding = [];
    private Dictionary<uint, VulkanSampler> _samplersByBinding = [];
    
    public VulkanDescriptorSet(VulkanRenderer renderer, VulkanShaderBacking shaderBacking, int setIndex, in DescriptorSetCreationHints hints)
    {
        _renderer = renderer;
        _shaderBacking = shaderBacking;
        _setIndex = setIndex;

        DescriptorSetLayout layout = shaderBacking.GetLayoutForDescriptorSet(setIndex);
        ReadOnlySpan<(VkDescriptorType, int)> descriptorCounts = shaderBacking.GetDescriptorCountsForDescriptorSet(setIndex);

        DescriptorSet = _renderer.DescriptorPoolManager.AllocateDescriptorSet(layout, descriptorCounts, out _poolAllocatedFrom);
    }
    
    private void ReleaseUnmanagedResources()
    {
        if (_renderer.IsDisposed) {
            return;
        }
        _renderer.DescriptorPoolManager.FreeDescriptorSet(DescriptorSet, _poolAllocatedFrom, _shaderBacking.GetDescriptorCountsForDescriptorSet(_setIndex));
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
        _samplersByBinding[GetBinding(in info)] = (VulkanSampler)sampler;
    }

    public unsafe void Update()
    {
        // TODO: Get clever about combining these into a single vkUpdateDescriptorSets call.
        // TODO: Iterating over these dictionaries probably generates garbage.
        
        foreach ((uint binding, IVulkanBuffer buffer) in _uniformBuffersByBinding) {
            DescriptorBufferInfo bufferInfo = new DescriptorBufferInfo(buffer.GetBufferForCurrentFrame(), 0, Vk.WholeSize);
            WriteDescriptorSet write = new(
                sType: StructureType.WriteDescriptorSet,
                pNext: null,
                dstSet: DescriptorSet,
                dstBinding: binding,
                dstArrayElement: 0,
                descriptorCount: 1,
                descriptorType: VkDescriptorType.UniformBuffer,
                pImageInfo: null,
                pBufferInfo: &bufferInfo,
                pTexelBufferView: null
            );
            _renderer.Vk.UpdateDescriptorSets(_renderer.Device, 1, in write, 0, null);
        }
        
        foreach ((uint binding, IVulkanBuffer buffer) in _storageBuffersByBinding) {
            DescriptorBufferInfo bufferInfo = new DescriptorBufferInfo(buffer.GetBufferForCurrentFrame(), 0, Vk.WholeSize);
            WriteDescriptorSet write = new(
                sType: StructureType.WriteDescriptorSet,
                pNext: null,
                dstSet: DescriptorSet,
                dstBinding: binding,
                dstArrayElement: 0,
                descriptorCount: 1,
                descriptorType: VkDescriptorType.StorageBuffer,
                pImageInfo: null,
                pBufferInfo: &bufferInfo,
                pTexelBufferView: null
            );
            _renderer.Vk.UpdateDescriptorSets(_renderer.Device, 1, in write, 0, null);
        }
        
        foreach ((uint binding, IVulkanTexture texture) in _texturesByBinding) {
            
            // TODO: Get fallback sampler?
            if (!_samplersByBinding.TryGetValue(binding, out VulkanSampler? sampler)) {
                sampler = _renderer.FallbackSampler;
            }

            DescriptorImageInfo imageInfo = new DescriptorImageInfo(sampler.Sampler, texture.GetImageView(), ImageLayout.ShaderReadOnlyOptimal);
            WriteDescriptorSet write = new(
                sType: StructureType.WriteDescriptorSet,
                pNext: null,
                dstSet: DescriptorSet,
                dstBinding: binding,
                dstArrayElement: 0,
                descriptorCount: 1,
                descriptorType: VkDescriptorType.CombinedImageSampler,
                pImageInfo: &imageInfo,
                pBufferInfo: null,
                pTexelBufferView: null
            );
            _renderer.Vk.UpdateDescriptorSets(_renderer.Device, 1, in write, 0, null);
        }
        
    }
    

}