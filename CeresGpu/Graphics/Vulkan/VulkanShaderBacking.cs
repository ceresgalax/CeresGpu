using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using CeresGpu.Graphics.Shaders;
using Silk.NET.Vulkan;
using DescriptorType = CeresGpu.Graphics.Shaders.DescriptorType;
using VkDescriptorType = Silk.NET.Vulkan.DescriptorType;

namespace CeresGpu.Graphics.Vulkan;


public class VulkanShaderBacking : IShaderBacking
{
    private readonly VulkanRenderer _renderer;
    public readonly ShaderModule ShaderModule; 
    
    public readonly PipelineLayout PipelineLayout;

    private readonly DescriptorSetLayout[] _descriptorSetLayouts;
    private readonly (VkDescriptorType, int)[][] _descriptorCountsBySet;
    
    public unsafe VulkanShaderBacking(VulkanRenderer renderer, IShader shader)
    {
        _renderer = renderer;
        Vk vk = renderer.Vk;
        
        //
        // Create the shader module.
        //
        
        using MemoryStream memoryStream = new();
        Stream? stream = shader.GetShaderResource(".vulkan");
        if (stream == null) {
            throw new InvalidOperationException("Cannot find vulkan shader binary.");
        }
        stream.CopyTo(memoryStream);

        byte[] bytes = memoryStream.ToArray();
        fixed (byte* pBytes = bytes) {
            ShaderModuleCreateInfo moduleCreateInfo = new(
                StructureType.ShaderModuleCreateInfo,
                pNext: null,
                flags: ShaderModuleCreateFlags.None,
                codeSize: (nuint)bytes.Length, // Yes, this is size in bytes, despite pCode being a uint pointer.
                pCode: (uint*)pBytes // Yes, it's a uint pointer, not a byte or void pointer.
            );

            Result createResult = vk.CreateShaderModule(renderer.Device, in moduleCreateInfo, null, out ShaderModule);
            if (createResult != Result.Success) {
                Console.Error.WriteLine($"Failed to create shader module: {createResult}");
            }
        }
        
        ReadOnlySpan<DescriptorInfo> descriptors = shader.GetDescriptors();
        
        //
        // How many descriptor sets do we need?
        //
        // TODO: Can this count be baked into the generated shader?
        uint numDescriptorSets = 0;
        for (int descriptorIndex = 0; descriptorIndex < descriptors.Length; ++descriptorIndex) {
            ref readonly DescriptorInfo descriptorInfo = ref descriptors[descriptorIndex];
            VulkanDescriptorBindingInfo binding = (VulkanDescriptorBindingInfo)descriptorInfo.Binding;
            numDescriptorSets = Math.Max(binding.Set + 1, numDescriptorSets);
        }
        _descriptorSetLayouts = new DescriptorSetLayout[numDescriptorSets];
        _descriptorCountsBySet = new (VkDescriptorType, int)[numDescriptorSets][];

        //
        // Create the layouts
        //
        for (int descriptorSetIndex = 0; descriptorSetIndex < numDescriptorSets; ++descriptorSetIndex) {
            List<DescriptorSetLayoutBinding> bindings = [];
            Dictionary<DescriptorType, int> descriptorCounts = [];
            
            for (int descriptorIndex = 0; descriptorIndex < descriptors.Length; ++descriptorIndex) {
                ref readonly DescriptorInfo descriptorInfo = ref descriptors[descriptorIndex];
                VulkanDescriptorBindingInfo binding = (VulkanDescriptorBindingInfo)descriptorInfo.Binding;

                // TODO: This loop is inefficient with multiple descriptor sets.
                if (binding.Set != descriptorSetIndex) {
                    continue;
                }
                
                bindings.Add(new DescriptorSetLayoutBinding(
                    binding: binding.Binding,
                    descriptorType: TranslateDescriptorType(descriptorInfo.DescriptorType),
                    descriptorCount: 1,
                    ShaderStageFlags.VertexBit | ShaderStageFlags.FragmentBit,
                    pImmutableSamplers: null
                ));
                
                descriptorCounts.TryAdd(descriptorInfo.DescriptorType, 0);
                descriptorCounts[descriptorInfo.DescriptorType]++;
            }

            DescriptorSetLayoutBinding[] bindingsArray = bindings.ToArray();
            fixed (DescriptorSetLayoutBinding* pLayoutBindings = bindingsArray) {
                DescriptorSetLayoutCreateInfo layoutCreateInfo = new(
                    StructureType.DescriptorSetLayoutCreateInfo,
                    pNext: null,
                    flags: DescriptorSetLayoutCreateFlags.None,
                    bindingCount: (uint)bindingsArray.Length,
                    pBindings: pLayoutBindings
                );
            
                vk.CreateDescriptorSetLayout(renderer.Device, in layoutCreateInfo, null, out _descriptorSetLayouts[descriptorSetIndex])
                    .AssertSuccess("Failed to create descriptor set layout");
                
                _descriptorCountsBySet[descriptorSetIndex] = descriptorCounts
                    .Select(kvp => (TranslateDescriptorType(kvp.Key), kvp.Value))
                    .ToArray();
            }
        }
        
        //
        // Create a pipeline layout for this shader
        //
        fixed (DescriptorSetLayout* pLayouts = _descriptorSetLayouts) {
            PipelineLayoutCreateInfo pipelineLayoutCreateInfo = new(
                StructureType.PipelineLayoutCreateInfo,
                pNext: null,
                PipelineLayoutCreateFlags.None,
                setLayoutCount: (uint)_descriptorSetLayouts.Length,
                pSetLayouts: pLayouts,
                pushConstantRangeCount: 0,
                pPushConstantRanges: null
            );
            vk.CreatePipelineLayout(renderer.Device, in pipelineLayoutCreateInfo, null, out PipelineLayout)
                .AssertSuccess("Failed to create pipeline layout");    
        }
        
    }

    public DescriptorSetLayout GetLayoutForDescriptorSet(int setIndex)
    {
        return _descriptorSetLayouts[setIndex];
    }

    /// <summary>
    /// NOTE: This method can be called after <see cref="Dispose"/>. This is critical to ensure that
    /// <see cref="VulkanDescriptorSet"/> can be finalized without leaking pool space in
    /// <see cref="VulkanRenderer.DescriptorPoolManager"/>
    /// </summary>
    /// <param name="setIndex"></param>
    /// <returns></returns>
    public ReadOnlySpan<(VkDescriptorType, int)> GetDescriptorCountsForDescriptorSet(int setIndex)
    {
        return _descriptorCountsBySet[setIndex];
    }
    

    private static VkDescriptorType TranslateDescriptorType(DescriptorType descriptorType)
    {
        return descriptorType switch {
            DescriptorType.UniformBuffer => VkDescriptorType.UniformBuffer,
            DescriptorType.ShaderStorageBuffer => VkDescriptorType.StorageBuffer,
            DescriptorType.Texture => VkDescriptorType.CombinedImageSampler, // TODO: Does this match the spirv that spirv-cross emits? Or do we have a separate texture/sampler emitted in the output shader, like for Metal?
            _ => throw new ArgumentOutOfRangeException(nameof(descriptorType), descriptorType, null)
        };
    }
    
    private unsafe void ReleaseUnmanagedResources()
    {
        // TODO: Deffered delete for everything here in case a command buffer is left in the recording state and has
        //  seen any of these handles.
        //  Maybe this is best implemented by making sure passes keep a reference to everything that has been recorded
        //  until rendering is finished? That makes the finalizers much easier to implement, and the Dispose() method
        //  could put the shader in a disposed state, and then add this shader object to a defered delete queue where
        //  we can assert that there are no outstanding recording command buffers.

        if (PipelineLayout.Handle != 0) {
            _renderer.Vk.DestroyPipelineLayout(_renderer.Device, PipelineLayout, null);
        }

        foreach (DescriptorSetLayout layout in _descriptorSetLayouts) {
            if (layout.Handle != 0) {
                _renderer.Vk.DestroyDescriptorSetLayout(_renderer.Device, layout, null);
            }    
        }
        
        if (ShaderModule.Handle != 0) {
            _renderer.Vk.DestroyShaderModule(_renderer.Device, ShaderModule, null);
        }
    }

    private bool _isDisposed;

    public void Dispose()
    {
        if (_isDisposed) {
            throw new ObjectDisposedException("this");
        }
        _isDisposed = true;
        
        ReleaseUnmanagedResources();
        GC.SuppressFinalize(this);
    }

    ~VulkanShaderBacking()
    {
        ReleaseUnmanagedResources();
    }
}