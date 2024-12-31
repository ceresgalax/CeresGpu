using System;
using System.IO;
using CeresGpu.Graphics.Shaders;
using Silk.NET.Vulkan;
using DescriptorType = CeresGpu.Graphics.Shaders.DescriptorType;
using VkDescriptorType = Silk.NET.Vulkan.DescriptorType;

namespace CeresGpu.Graphics.Vulkan;

public class VulkanShaderBacking : IShaderBacking
{
    private readonly VulkanRenderer _renderer;
    public readonly ShaderModule ShaderModule; 
    public readonly DescriptorSetLayout DescriptorSetLayout;
    public readonly PipelineLayout PipelineLayout;
    
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
        
        //
        // Create the descriptor set layout for this shader
        //
        ReadOnlySpan<DescriptorInfo> descriptors = shader.GetDescriptors();
        DescriptorSetLayoutBinding[] bindings = new DescriptorSetLayoutBinding[descriptors.Length];
        for (int descriptorIndex = 0; descriptorIndex < descriptors.Length; ++descriptorIndex) {
            ref readonly DescriptorInfo descriptorInfo = ref descriptors[descriptorIndex];
            bindings[descriptorIndex] = new DescriptorSetLayoutBinding(
                binding: descriptorInfo.BindingIndex,
                descriptorType: TranslateDescriptorType(descriptorInfo.DescriptorType),
                descriptorCount: 1,
                ShaderStageFlags.VertexBit | ShaderStageFlags.FragmentBit,
                pImmutableSamplers: null
            );
        }

        fixed (DescriptorSetLayoutBinding* pLayoutBindings = bindings) {
            DescriptorSetLayoutCreateInfo layoutCreateInfo = new(
                StructureType.DescriptorSetLayoutCreateInfo,
                pNext: null,
                flags: DescriptorSetLayoutCreateFlags.None,
                bindingCount: (uint)bindings.Length,
                pBindings: pLayoutBindings
            );
            
            vk.CreateDescriptorSetLayout(renderer.Device, layoutCreateInfo, null, out DescriptorSetLayout)
                .AssertSuccess("Failed to create descriptor set layout");
        }
        
        //
        // Create a pipeline layout for this shader
        //
        DescriptorSetLayout descriptorSetLayout = DescriptorSetLayout;
        PipelineLayoutCreateInfo pipelineLayoutCreateInfo = new(
            StructureType.PipelineLayoutCreateInfo,
            pNext: null,
            PipelineLayoutCreateFlags.None,
            setLayoutCount: 1,
            pSetLayouts: &descriptorSetLayout,
            pushConstantRangeCount: 0,
            pPushConstantRanges: null
        );
        vk.CreatePipelineLayout(renderer.Device, in pipelineLayoutCreateInfo, null, out PipelineLayout)
            .AssertSuccess("Failed to create pipeline layout");
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
        
        if (DescriptorSetLayout.Handle != 0) {
            _renderer.Vk.DestroyDescriptorSetLayout(_renderer.Device, DescriptorSetLayout, null);
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