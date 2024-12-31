using System;
using CeresGpu.Graphics.Shaders;
using Silk.NET.Vulkan;
using VkBlendOp = Silk.NET.Vulkan.BlendOp;

namespace CeresGpu.Graphics.Vulkan;

public class VulkanPipeline<TShader, TVertexBufferLayout> : IPipeline<TShader, TVertexBufferLayout>
    where TShader : IShader
    where TVertexBufferLayout : IVertexBufferLayout<TShader>
{
    public readonly VulkanRenderer _renderer;
    
    public unsafe VulkanPipeline(VulkanRenderer renderer, PipelineDefinition definition, TShader shader, TVertexBufferLayout vertexBufferLayout)
    {
        _renderer = renderer;
        Vk vk = renderer.Vk;

        VulkanShaderBacking? shaderBacking = shader.Backing as VulkanShaderBacking;
        if (shaderBacking == null) {
            throw new ArgumentException("Shader backing is incompatible.", nameof(shader));
        }

        CreateVertexBindingAndAttributeDescriptions(vertexBufferLayout, shader.VertexAttributeDescriptors,
            out VertexInputBindingDescription[] vertexBindingDescriptions,
            out VertexInputAttributeDescription[] vertexAttributeDescriptions);

        PipelineColorBlendAttachmentState[] colorBlendAttachmentStates = CreateColorBlendAttachmentStates(definition);

        Span<DynamicState> dynamicStates = stackalloc DynamicState[] {
            DynamicState.Viewport,
            DynamicState.Scissor
        };
        
        fixed (VertexInputBindingDescription* pVertexBindingDescriptions = vertexBindingDescriptions)
        fixed (VertexInputAttributeDescription* pVertexAttributeDescriptions = vertexAttributeDescriptions) 
        fixed (PipelineColorBlendAttachmentState* pColorBlendAttachmentStates = colorBlendAttachmentStates)
        fixed (DynamicState* pDynamicStates = dynamicStates)
        fixed (byte* vertName = "vert"u8) // TODO: Make sure the shader gen linker exports the entry points in the linked binary as we expect.
        fixed (byte* fragName = "frag"u8) {

            Span<PipelineShaderStageCreateInfo> stages = stackalloc PipelineShaderStageCreateInfo[] {
                new PipelineShaderStageCreateInfo(
                    StructureType.PipelineShaderStageCreateInfo,
                    pNext: null,
                    flags: PipelineShaderStageCreateFlags.None,
                    stage: ShaderStageFlags.VertexBit,
                    module: shaderBacking.ShaderModule,
                    pName: vertName,
                    pSpecializationInfo: null
                ),
                new PipelineShaderStageCreateInfo(
                    StructureType.PipelineShaderStageCreateInfo,
                    pNext: null,
                    flags: PipelineShaderStageCreateFlags.None,
                    stage: ShaderStageFlags.FragmentBit,
                    module: shaderBacking.ShaderModule,
                    pName: fragName,
                    pSpecializationInfo: null
                )
            };
            
            PipelineInputAssemblyStateCreateInfo inputAssemblyStateCreateInfo = new(
                StructureType.PipelineInputAssemblyStateCreateInfo,
                pNext: null,
                flags: 0,
                PrimitiveTopology.TriangleList,
                primitiveRestartEnable: false
            );
            
            PipelineVertexInputStateCreateInfo vertexInputStateCreateInfo = new(
                StructureType.PipelineVertexInputStateCreateInfo,
                pNext: null,
                flags: 0,
                vertexBindingDescriptionCount: (uint)vertexBindingDescriptions.Length,
                pVertexBindingDescriptions: pVertexBindingDescriptions,
                vertexAttributeDescriptionCount: (uint)vertexAttributeDescriptions.Length,
                pVertexAttributeDescriptions: pVertexAttributeDescriptions
            );

            PipelineViewportStateCreateInfo viewportStateCreateInfo = new(
                StructureType.PipelineViewportStateCreateInfo,
                pNext: null,
                flags: 0
                // TODO: Specify viewport!! Or do we need to set up dynamic viewport? (changable via commands)
            );

            PipelineRasterizationStateCreateInfo rasterizationStateCreateInfo = new(
                StructureType.PipelineRasterizationStateCreateInfo,
                pNext: null,
                flags: 0,
                depthClampEnable: false,
                rasterizerDiscardEnable: false,
                PolygonMode.Fill,
                cullMode: definition.CullMode switch {
                    CullMode.Front => CullModeFlags.FrontBit,
                    CullMode.Back => CullModeFlags.BackBit,
                    _ => CullModeFlags.None
                },
                frontFace: FrontFace.Clockwise,
                depthBiasEnable: false,
                depthBiasConstantFactor: 0f,
                depthBiasClamp: 0f,
                depthBiasSlopeFactor: 0f,
                lineWidth: 1f
            );

            PipelineMultisampleStateCreateInfo multisampleStateCreateInfo = new(
                StructureType.PipelineMultisampleStateCreateInfo,
                pNext: null,
                flags: 0,
                rasterizationSamples: SampleCountFlags.None,
                sampleShadingEnable: false,
                minSampleShading: 0f,
                pSampleMask: null,
                alphaToCoverageEnable: false,
                alphaToOneEnable: false
            );

            PipelineDepthStencilStateCreateInfo depthStencilStateCreateInfo = new(
                StructureType.PipelineDepthStencilStateCreateInfo,
                pNext: null,
                flags: PipelineDepthStencilStateCreateFlags.None,
                depthTestEnable: definition.DepthStencil.DepthCompareFunction != CompareFunction.Never,
                depthWriteEnable: definition.DepthStencil.DepthWriteEnabled,
                depthCompareOp: definition.DepthStencil.DepthCompareFunction switch {
                    CompareFunction.Never => CompareOp.Never,
                    CompareFunction.Less => CompareOp.Less,
                    CompareFunction.Equal => CompareOp.Equal,
                    CompareFunction.LessEqual => CompareOp.LessOrEqual,
                    CompareFunction.Greater => CompareOp.Greater,
                    CompareFunction.NotEqual => CompareOp.NotEqual,
                    CompareFunction.GreaterEqual => CompareOp.GreaterOrEqual,
                    CompareFunction.Always => CompareOp.Always,
                    _ => throw new ArgumentOutOfRangeException(nameof(definition), $"depth compare function {definition.DepthStencil.DepthCompareFunction} is not implemented.")
                },
                depthBoundsTestEnable: false,
                stencilTestEnable: definition.DepthStencil.FrontFaceStencil.StencilCompareFunction != CompareFunction.Never 
                    && definition.DepthStencil.BackFaceStencil.StencilCompareFunction != CompareFunction.Never,
                front: TranslateStencilDefinition(in definition.DepthStencil.FrontFaceStencil),
                back: TranslateStencilDefinition(in definition.DepthStencil.BackFaceStencil),
                minDepthBounds: 0f,
                maxDepthBounds: 1f
            );

            PipelineColorBlendStateCreateInfo colorBlendStateCreateInfo = new(
                StructureType.PipelineColorBlendStateCreateInfo,
                pNext: null,
                flags: PipelineColorBlendStateCreateFlags.None,
                logicOpEnable: false,
                logicOp: LogicOp.Clear,
                attachmentCount: (uint)colorBlendAttachmentStates.Length,
                pAttachments: pColorBlendAttachmentStates
            );
            
            PipelineDynamicStateCreateInfo dynamicStateCreateInfo = new(
                StructureType.PipelineDynamicStateCreateInfo,
                pNext: null,
                flags: 0,
                dynamicStateCount: (uint)dynamicStates.Length,
                pDynamicStates: pDynamicStates
            );
            
            

            fixed (PipelineShaderStageCreateInfo* pStages = stages) {
                GraphicsPipelineCreateInfo createInfo = new(
                    StructureType.GraphicsPipelineCreateInfo,
                    pNext: null,
                    flags: PipelineCreateFlags.None,
                    stageCount: (uint)stages.Length,
                    pStages: pStages, 
                    pInputAssemblyState: &inputAssemblyStateCreateInfo,
                    pVertexInputState: &vertexInputStateCreateInfo,
                    pTessellationState: null,
                    pViewportState: &viewportStateCreateInfo,
                    pRasterizationState: &rasterizationStateCreateInfo,
                    pMultisampleState: &multisampleStateCreateInfo,
                    pDepthStencilState: &depthStencilStateCreateInfo,
                    pColorBlendState: &colorBlendStateCreateInfo,
                    pDynamicState: &dynamicStateCreateInfo,
                    layout: shaderBacking.PipelineLayout,
                    
                    
                    
            
                );
        
                //vk.CreateGraphicsPipelines(renderer.Device, default,  )}
            }
            
            
        }

        
        
       




    }

    private void CreateVertexBindingAndAttributeDescriptions(
        TVertexBufferLayout vertexBufferLayout,
        ReadOnlySpan<ShaderVertexAttributeDescriptor> shaderAttributes,
        out VertexInputBindingDescription[] bindingDescriptions,
        out VertexInputAttributeDescription[] attributeDescriptions
    )
    {
        ReadOnlySpan<VblBufferDescriptor> bufferDescriptors = vertexBufferLayout.BufferDescriptors;
        bindingDescriptions = new VertexInputBindingDescription[bufferDescriptors.Length];
        for (int bindingIndex = 0; bindingIndex < bufferDescriptors.Length; ++bindingIndex) {
            bindingDescriptions[bindingIndex] = TranslateVblBufferDescriptor(bufferDescriptors[bindingIndex], (uint)bindingIndex);
        }

        ReadOnlySpan<VblAttributeDescriptor> attributeDescriptors = vertexBufferLayout.AttributeDescriptors;
        attributeDescriptions = new VertexInputAttributeDescription[attributeDescriptors.Length];
        for (int attributeIndex = 0; attributeIndex < attributeDescriptions.Length; ++attributeIndex) {
            attributeDescriptions[attributeIndex] = TranslateVblAttributeDescriptor(attributeDescriptors[attributeIndex], shaderAttributes);
        }
    }

    private VertexInputBindingDescription TranslateVblBufferDescriptor(in VblBufferDescriptor descriptor, uint index)
    {
        return new VertexInputBindingDescription(binding: index, stride: descriptor.Stride, TranslateVertexStepFunction(descriptor.StepFunction));
    }

    private VertexInputAttributeDescription TranslateVblAttributeDescriptor(in VblAttributeDescriptor descriptor, ReadOnlySpan<ShaderVertexAttributeDescriptor> shaderAttributes)
    {
        ref readonly ShaderVertexAttributeDescriptor shaderAttribute = ref shaderAttributes[(int)descriptor.AttributeIndex];

        return new VertexInputAttributeDescription(
            location: descriptor.AttributeIndex,
            binding: descriptor.BufferIndex,
            format: TranslateVertexFormat(shaderAttribute.Format),
            offset: descriptor.BufferOffset
        );
    }

    private VertexInputRate TranslateVertexStepFunction(VertexStepFunction stepFunction)
    {
        return stepFunction switch {
            VertexStepFunction.PerVertex => VertexInputRate.Vertex,
            VertexStepFunction.PerInstance => VertexInputRate.Instance
        };
    }

    private Format TranslateVertexFormat(VertexFormat vertexFormat)
    {
        return vertexFormat switch {
            VertexFormat.Invalid => Format.Undefined,
            VertexFormat.UChar2 => Format.R8G8Uint,
            VertexFormat.UChar3 => Format.R8G8B8Uint,
            VertexFormat.UChar4 => Format.R8G8B8A8Uint,
            VertexFormat.Char2 => Format.R8G8Sint,
            VertexFormat.Char3 => Format.R8G8B8Sint,
            VertexFormat.Char4 => Format.R8G8B8A8Sint,
            VertexFormat.UChar2Normalized => Format.R8G8Unorm,
            VertexFormat.UChar3Normalized => Format.R8G8B8Unorm,
            VertexFormat.UChar4Normalized => Format.R8G8B8A8Unorm,
            VertexFormat.Char2Normalized => Format.R8G8SNorm,
            VertexFormat.Char3Normalized => Format.R8G8B8SNorm,
            VertexFormat.Char4Normalized => Format.R8G8B8A8SNorm,
            VertexFormat.UShort2 => Format.R16G16Uint,
            VertexFormat.UShort3 => Format.R16G16B16Uint,
            VertexFormat.UShort4 => Format.R16G16B16A16Uint,
            VertexFormat.Short2 => Format.R16G16Sint,
            VertexFormat.Short3 => Format.R16G16B16Sint,
            VertexFormat.Short4 => Format.R16G16B16A16Sint,
            VertexFormat.UShort2Normalized => Format.R16G16Unorm,
            VertexFormat.UShort3Normalized => Format.R16G16B16Unorm,
            VertexFormat.UShort4Normalized => Format.R16G16B16A16Unorm,
            VertexFormat.Short2Normalized => Format.R16G16SNorm,
            VertexFormat.Short3Normalized => Format.R16G16B16SNorm,
            VertexFormat.Short4Normalized => Format.R16G16B16A16SNorm,
            VertexFormat.Half2 => Format.R16G16Sfloat,
            VertexFormat.Half3 => Format.R16G16B16Sfloat,
            VertexFormat.Half4 => Format.R16G16B16A16Sfloat,
            VertexFormat.Float => Format.R32Sfloat,
            VertexFormat.Float2 => Format.R32G32Sfloat,
            VertexFormat.Float3 => Format.R32G32B32Sfloat,
            VertexFormat.Float4 => Format.R32G32B32A32Sfloat,
            VertexFormat.Int => Format.R32Sint,
            VertexFormat.Int2 => Format.R32G32Sint,
            VertexFormat.Int3 => Format.R32G32B32Sint,
            VertexFormat.Int4 => Format.R32G32B32A32Sint,
            VertexFormat.UInt => Format.R32Uint,
            VertexFormat.UInt2 => Format.R32G32Uint,
            VertexFormat.UInt3 => Format.R32G32B32Uint,
            VertexFormat.UInt4 => Format.R32G32B32A32Uint,
            VertexFormat.Int1010102Normalized => Format.A2B10G10R10SintPack32,
            VertexFormat.UInt1010102Normalized => Format.A2B10G10R10UintPack32,
            VertexFormat.UChar4Normalized_BGRA => Format.B8G8R8A8Unorm,
            VertexFormat.UChar => Format.R8Uint,
            VertexFormat.Char => Format.R8Sint,
            VertexFormat.UCharNormalized => Format.R8Unorm,
            VertexFormat.CharNormalized => Format.R8SNorm,
            VertexFormat.UShort => Format.R16Uint,
            VertexFormat.Short => Format.R16Sint,
            VertexFormat.UShortNormalized => Format.R16SNorm,
            VertexFormat.ShortNormalized => Format.R16Unorm,
            VertexFormat.Half => Format.R16Sfloat,
            _ => throw new ArgumentOutOfRangeException(nameof(vertexFormat), vertexFormat, null)
        };
    }

    private StencilOp TranslateStencilOperation(StencilOperation op)
    {
        return op switch {
            StencilOperation.Keep => StencilOp.Keep,
            StencilOperation.Zero => StencilOp.Zero,
            StencilOperation.Replace => StencilOp.Replace,
            StencilOperation.IncrementClamp => StencilOp.IncrementAndClamp,
            StencilOperation.DecrementClamp => StencilOp.DecrementAndClamp,
            StencilOperation.Invert => StencilOp.Invert,
            StencilOperation.IncrementWrap => StencilOp.IncrementAndWrap,
            StencilOperation.DecrementWrap => StencilOp.DecrementAndWrap,
            _ => throw new ArgumentOutOfRangeException(nameof(op), op, null)
        };
    }

    private StencilOpState TranslateStencilDefinition(in StencilDefinition def)
    {
        return new StencilOpState(
            failOp: TranslateStencilOperation(def.StencilFailureOperation),
            passOp: TranslateStencilOperation(def.DepthStencilPassOperation),
            depthFailOp: TranslateStencilOperation(def.DepthFailureOperation),
            compareOp: def.StencilCompareFunction switch {
                CompareFunction.Never => CompareOp.Never,
                CompareFunction.Less => CompareOp.Less,
                CompareFunction.Equal => CompareOp.Equal,
                CompareFunction.LessEqual => CompareOp.LessOrEqual,
                CompareFunction.Greater => CompareOp.Greater,
                CompareFunction.NotEqual => CompareOp.NotEqual,
                CompareFunction.GreaterEqual => CompareOp.GreaterOrEqual,
                CompareFunction.Always => CompareOp.Always,
                _ => throw new ArgumentOutOfRangeException(nameof(def), $"Unsupported CompareFunction {def.StencilCompareFunction}")
            },
            compareMask: def.ReadMask,
            writeMask: def.WriteMask,
            reference: 0 // TODO: Since CeresGPU's api is similar to Metal's API, Do we need to support dynamic stencil reference value setting (via commands?)
        );
    }

    private PipelineColorBlendAttachmentState[] CreateColorBlendAttachmentStates(PipelineDefinition def)
    {
        // CeresGpu Pipeline Definition only supports declaring a single color attachment for now. 
        return [
            new PipelineColorBlendAttachmentState(
                blendEnable: def.Blend,
                srcColorBlendFactor: TranslateBlendingFactor(def.BlendFunction.SourceRGB),
                dstColorBlendFactor: TranslateBlendingFactor(def.BlendFunction.DestinationRGB),
                colorBlendOp: TranslateBlendOp(def.ColorBlendOp),
                srcAlphaBlendFactor: TranslateBlendingFactor(def.BlendFunction.SourceAlpha),
                dstAlphaBlendFactor: TranslateBlendingFactor(def.BlendFunction.DestinationAlpha),
                alphaBlendOp: TranslateBlendOp(def.AlphaBlendOp),
                colorWriteMask: ColorComponentFlags.RBit | ColorComponentFlags.GBit | ColorComponentFlags.BBit | ColorComponentFlags.ABit
            )
        ];
    }

    private BlendFactor TranslateBlendingFactor(BlendingFactor factor)
    {
        return factor switch {
            BlendingFactor.ZERO => BlendFactor.Zero,
            BlendingFactor.ONE => BlendFactor.One,
            BlendingFactor.SRC_COLOR => BlendFactor.SrcColor,
            BlendingFactor.ONE_MINUS_SRC_COLOR => BlendFactor.OneMinusSrcColor,
            BlendingFactor.SRC_ALPHA => BlendFactor.SrcAlpha,
            BlendingFactor.ONE_MINUS_SRC_ALPHA => BlendFactor.OneMinusSrcAlpha,
            BlendingFactor.DST_ALPHA => BlendFactor.DstAlpha,
            BlendingFactor.ONE_MINUS_DST_ALPHA => BlendFactor.OneMinusDstAlpha,
            BlendingFactor.DST_COLOR => BlendFactor.DstColor,
            BlendingFactor.ONE_MINUS_DST_COLOR => BlendFactor.OneMinusDstColor,
            BlendingFactor.SRC_ALPHA_SATURATE => BlendFactor.SrcAlphaSaturate,
            BlendingFactor.CONSTANT_COLOR => BlendFactor.ConstantColor,
            BlendingFactor.ONE_MINUS_CONSTANT_COLOR => BlendFactor.OneMinusConstantColor,
            BlendingFactor.CONSTANT_ALPHA => BlendFactor.ConstantAlpha,
            BlendingFactor.ONE_MINUS_CONSTANT_ALPHA => BlendFactor.OneMinusConstantAlpha,
            BlendingFactor.SRC1_ALPHA => BlendFactor.Src1Alpha,
            BlendingFactor.SRC1_COLOR => BlendFactor.Src1Color,
            BlendingFactor.ONE_MINUS_SRC1_COLOR => BlendFactor.OneMinusSrc1Color,
            BlendingFactor.ONE_MINUS_SRC1_ALPHA => BlendFactor.OneMinusSrc1Alpha,
            _ => throw new ArgumentOutOfRangeException(nameof(factor), factor, null)
        };
    }

    private VkBlendOp TranslateBlendOp(BlendOp op)
    {
        return op switch {
            BlendOp.ADD => VkBlendOp.Add,
            BlendOp.MIN => VkBlendOp.Min,
            BlendOp.MAX => VkBlendOp.Max,
            BlendOp.SUBTRACT => VkBlendOp.Subtract,
            BlendOp.REVERSE_SUBTRACT => VkBlendOp.ReverseSubtract,
            _ => throw new ArgumentOutOfRangeException(nameof(op), op, null)
        };
    }
    
    private unsafe void ReleaseUnmanagedResources()
    {
        // TODO: Destroy pipeline layout, but must be defered in case there are currently any command buffers that
        //  have seen this pipeline layout and are still in the recording state (See Valid Usage of Vulkan Spec)
        
        if (_pipelineLayout.Handle != 0) {
            _renderer.Vk.DestroyPipelineLayout(_renderer.Device, _pipelineLayout, null);
        }
        
    }

    public void Dispose()
    {
        ReleaseUnmanagedResources();
        GC.SuppressFinalize(this);
    }

    ~VulkanPipeline()
    {
        ReleaseUnmanagedResources();
    }
}