using System;
using System.Collections.Generic;
using CeresGpu.Graphics.Shaders;
using Silk.NET.Vulkan;
using VkDescriptorType = Silk.NET.Vulkan.DescriptorType;

namespace CeresGpu.Graphics.Vulkan;

public sealed class VulkanShaderInstanceBacking : IShaderInstanceBacking, IDeferredDisposable
{
    private readonly VulkanRenderer _renderer;
    public readonly VulkanShaderBacking Shader;

    /// <summary>
    /// All descriptor sets, including their working frame, layed out contiguously:
    /// [Set0Frame0][Set1Frame0][SetNFrame0][Set0FrameN][Set1FrameN][SetNFrameN]
    /// </summary>
    public readonly DescriptorSet[] DescriptorSets;
    private readonly DescriptorPool[] _poolsAllocatedFrom;
    
    private readonly Dictionary<VulkanDescriptorBindingInfo, IVulkanBuffer> _uniformBuffersByBinding = [];
    private readonly Dictionary<VulkanDescriptorBindingInfo, IVulkanBuffer> _storageBuffersByBinding = [];
    private readonly Dictionary<VulkanDescriptorBindingInfo, IVulkanTexture> _texturesByBinding = [];
    private readonly Dictionary<VulkanDescriptorBindingInfo, VulkanSampler> _samplersByBinding = [];
    
    public VulkanShaderInstanceBacking(VulkanRenderer renderer, VulkanShaderBacking shaderBacking)
    {
        _renderer = renderer;
        Shader = shaderBacking;
        
        // Allocate descriptor sets

        DescriptorSets = new DescriptorSet[shaderBacking.NumDescriptorSets * renderer.FrameCount];
        _poolsAllocatedFrom = new DescriptorPool[shaderBacking.NumDescriptorSets * renderer.FrameCount];
        
        Span<DescriptorSet> sets = stackalloc DescriptorSet[1];
        Span<DescriptorSetLayout> layouts = stackalloc DescriptorSetLayout[1];
        
        for (int frameIndex = 0; frameIndex < renderer.FrameCount; ++frameIndex) {
            for (int setIndex = 0; setIndex < shaderBacking.NumDescriptorSets; ++setIndex) {
                layouts[0] = shaderBacking.GetLayoutForDescriptorSet(setIndex);
                ReadOnlySpan<(VkDescriptorType, int)> descriptorCounts = shaderBacking.GetDescriptorCountsForDescriptorSet(setIndex);
                int i = frameIndex * (int)shaderBacking.NumDescriptorSets + setIndex;
                _renderer.DescriptorPoolManager.AllocateDescriptorSets(layouts, descriptorCounts, out _poolsAllocatedFrom[i], sets);
                DescriptorSets[i] = sets[0];
            }
        }
    }
    
    public void DeferredDispose()
    {
        Span<DescriptorSet> sets = stackalloc DescriptorSet[1];
        for (int frameIndex = 0; frameIndex < _renderer.FrameCount; ++frameIndex) {
            for (int setIndex = 0; setIndex < Shader.NumDescriptorSets; ++setIndex) {
                ReadOnlySpan<(VkDescriptorType, int)> descriptorCounts = Shader.GetDescriptorCountsForDescriptorSet(setIndex);
                int i = frameIndex * (int)Shader.NumDescriptorSets + setIndex;
                sets[0] = DescriptorSets[i];
                _renderer.DescriptorPoolManager.FreeDescriptorSets(sets, _poolsAllocatedFrom[i], descriptorCounts);
            }
        }
    }
    
    private void ReleaseUnmanagedResources()
    {
        if (!_renderer.IsDisposed) {
            _renderer.DeferDisposal(this);
        }
    }

    public void Dispose()
    {
        ReleaseUnmanagedResources();
        GC.SuppressFinalize(this);
    }

    ~VulkanShaderInstanceBacking()
    {
        ReleaseUnmanagedResources();
    }

    private VulkanDescriptorBindingInfo GetBinding(in DescriptorInfo descriptorInfo)
    {
        return (VulkanDescriptorBindingInfo)descriptorInfo.Binding;
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

    public void GetUsedBuffers(List<IBuffer> outBuffers)
    {
        // TODO: Iterating over the array probably generates garbage.
        foreach ((_, IVulkanBuffer buffer) in _uniformBuffersByBinding) {
            outBuffers.Add(buffer);
        }
        
        foreach ((_, IVulkanBuffer buffer) in _storageBuffersByBinding) {
            outBuffers.Add(buffer);
        }
    }

    public unsafe void Update()
    {
        // TODO: Get clever about combining these into a single vkUpdateDescriptorSets call.
        // TODO: Iterating over these dictionaries probably generates garbage.
        
        //DescriptorSet currentFrameDescriptorSet = DescriptorSets[_renderer.WorkingFrame];

        DescriptorSet GetDescriptorSet(in VulkanDescriptorBindingInfo descriptorInfo)
        {
            return DescriptorSets[Shader.NumDescriptorSets * _renderer.WorkingFrame + descriptorInfo.Set];
        }
        
        foreach ((VulkanDescriptorBindingInfo binding, IVulkanBuffer buffer) in _uniformBuffersByBinding) {
            
            buffer.Commit();
            
            DescriptorBufferInfo bufferInfo = new DescriptorBufferInfo(buffer.GetBufferForCurrentFrame(), 0, Vk.WholeSize);
            WriteDescriptorSet write = new(
                sType: StructureType.WriteDescriptorSet,
                pNext: null,
                dstSet: GetDescriptorSet(in binding),
                dstBinding: binding.Binding,
                dstArrayElement: 0,
                descriptorCount: 1,
                descriptorType: VkDescriptorType.UniformBuffer,
                pImageInfo: null,
                pBufferInfo: &bufferInfo,
                pTexelBufferView: null
            );
            _renderer.Vk.UpdateDescriptorSets(_renderer.Device, 1, in write, 0, null);
        }
        
        foreach ((VulkanDescriptorBindingInfo binding, IVulkanBuffer buffer) in _storageBuffersByBinding) {
            
            buffer.Commit();
            
            DescriptorBufferInfo bufferInfo = new DescriptorBufferInfo(buffer.GetBufferForCurrentFrame(), 0, Vk.WholeSize);
            WriteDescriptorSet write = new(
                sType: StructureType.WriteDescriptorSet,
                pNext: null,
                dstSet: GetDescriptorSet(in binding),
                dstBinding: binding.Binding,
                dstArrayElement: 0,
                descriptorCount: 1,
                descriptorType: VkDescriptorType.StorageBuffer,
                pImageInfo: null,
                pBufferInfo: &bufferInfo,
                pTexelBufferView: null
            );
            _renderer.Vk.UpdateDescriptorSets(_renderer.Device, 1, in write, 0, null);
        }
        
        foreach ((VulkanDescriptorBindingInfo binding, IVulkanTexture texture) in _texturesByBinding) {
            if (!_samplersByBinding.TryGetValue(binding, out VulkanSampler? sampler)) {
                sampler = _renderer.FallbackSampler;
            }

            DescriptorImageInfo imageInfo = new DescriptorImageInfo(sampler.Sampler, texture.GetImageView(), ImageLayout.ShaderReadOnlyOptimal);
            WriteDescriptorSet write = new(
                sType: StructureType.WriteDescriptorSet,
                pNext: null,
                dstSet: GetDescriptorSet(in binding),
                dstBinding: binding.Binding,
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