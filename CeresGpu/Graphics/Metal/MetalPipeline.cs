using System;
using CeresGpu.Graphics.Shaders;
using CeresGpu.MetalBinding;

namespace CeresGpu.Graphics.Metal
{
    public static class MetalBufferTableConstants
    {
        public const uint INDEX_ARGUMENT_BUFFER_0 = 0;
        public const uint INDEX_VERTEX_BUFFER_MAX = 30;
    }
    
    public sealed class MetalPipeline<TShader, TVertexBufferLayout> : IPipeline<TShader, TVertexBufferLayout> 
        where TShader : IShader
        where TVertexBufferLayout : IVertexBufferLayout<TShader>
    {
        public readonly CullMode CullMode;
        private IntPtr _pipelineState;
        private IntPtr _depthStencilState;

        public IntPtr Handle => _pipelineState;
        public IntPtr DepthStencilState => _depthStencilState;
        
        public MetalPipeline(MetalRenderer renderer, PipelineDefinition definition, IShader shader, TVertexBufferLayout vertexBufferLayout)
        {
            if (shader.Backing is not MetalShaderBacking backing) {
                throw new ArgumentException("Incompatible shader backing", nameof(shader));
            }

            CullMode = definition.CullMode;
            
            IntPtr rpd = MetalApi.metalbinding_new_rpd(renderer.Context);
            try {
                MetalApi.metalbinding_set_rpd_common(
                    descriptor: rpd,
                    blend: definition.Blend,
                    blendOp: TranslateBlendEquation(definition.BlendEquation),
                    sourceRgb: TranslateBlendFactor(definition.BlendFunction.SourceRGB),
                    destRgb: TranslateBlendFactor(definition.BlendFunction.DestinationRGB),
                    sourceAlpha: TranslateBlendFactor(definition.BlendFunction.SourceAlpha),
                    destAlpha: TranslateBlendFactor(definition.BlendFunction.DestinationAlpha)
                );
                MetalApi.metalbinding_set_rpd_functions(rpd, backing.VertexFunction, backing.FragmentFunction);
                
                SetupVertexDescriptor(renderer, rpd, shader, vertexBufferLayout);

                _pipelineState = MetalApi.metalbinding_new_pipeline_state(renderer.Context, rpd);
                if (_pipelineState == IntPtr.Zero) {
                    throw new InvalidOperationException("Failed to create pipeline: " + renderer.GetLastError());
                }
            } finally {
                MetalApi.metalbinding_release_rpd(rpd);
            }
            
            IntPtr frontFaceStencil = MakeStencilDescriptor(renderer, definition.DepthStencil.FrontFaceStencil);
            try {
                IntPtr backFaceStencil = MakeStencilDescriptor(renderer, definition.DepthStencil.BackFaceStencil);
                try {
                    IntPtr dsd = MetalApi.metalbinding_new_dsd(
                        depthCompareFunc: TranslateCompareFunction(definition.DepthStencil.DepthCompareFunction),
                        depthWriteEnabled: definition.DepthStencil.DepthWriteEnabled,
                        backFaceStencil: backFaceStencil,
                        frontFaceStencil: frontFaceStencil
                    );
                    try {
                        _depthStencilState = MetalApi.metalbinding_new_depth_stencil_state(renderer.Context, dsd);
                    } finally {
                        MetalApi.metalbinding_release_dsd(dsd);
                    }
                } finally {
                    MetalApi.metalbinding_release_stencil_descriptor(backFaceStencil);
                }
            } finally {
                MetalApi.metalbinding_release_stencil_descriptor(frontFaceStencil);
            }
            
        }

        private static void SetupVertexDescriptor(MetalRenderer renderer, IntPtr rpd, IShader shader, TVertexBufferLayout vertexBufferLayout)
        {
            IntPtr vertexDescriptor = MetalApi.metalbinding_new_vertex_descriptor(renderer.Context);
            try {

                ReadOnlySpan<VblAttributeDescriptor> vblAttributeDescriptors = vertexBufferLayout.AttributeDescriptors;
                ReadOnlySpan<VblBufferDescriptor> vblBufferDescriptors = vertexBufferLayout.BufferDescriptors;
                ReadOnlySpan<ShaderVertexAttributeDescriptor> shaderVads = shader.VertexAttributeDescriptors;
                
                foreach (ref readonly VblAttributeDescriptor vblAttributeDescriptor in vblAttributeDescriptors) {
                    if (vblAttributeDescriptor.AttributeIndex >= shaderVads.Length) {
                        // Uh oh! The vertex buffer layout refers to an attribute index that doesn't exist in the shader!
                        // TODO: Can this be recovered instead of throwing?
                        throw new InvalidOperationException("Vertex buffer layout refers to an attribute index that doesn't exist in the shader.");
                    }

                    ref readonly ShaderVertexAttributeDescriptor shaderVad = ref shaderVads[(int)vblAttributeDescriptor.AttributeIndex];
                    
                    MetalApi.metalbinding_set_vertex_descriptor_vad(
                        vertexDescriptor,
                        vblAttributeDescriptor.AttributeIndex,
                        TranslateVertexFormat(shaderVad.Format),
                        vblAttributeDescriptor.BufferOffset,
                        MetalBufferTableConstants.INDEX_VERTEX_BUFFER_MAX - vblAttributeDescriptor.BufferIndex
                    );
                }
                
                for (int i = 0, ilen = vblBufferDescriptors.Length; i < ilen; ++i) {
                    ref readonly VblBufferDescriptor vblBufferDescriptor = ref vblBufferDescriptors[i];
                    MetalApi.metalbinding_set_vertex_descriptor_vbl(vertexDescriptor, MetalBufferTableConstants.INDEX_VERTEX_BUFFER_MAX - (uint)i, TranslateStepFunction(vblBufferDescriptor.StepFunction), vblBufferDescriptor.Stride);
                }

                MetalApi.metalbinding_set_rpd_vertex_descriptor(rpd, vertexDescriptor);
            } finally {
                MetalApi.metalbinding_release_vertex_descriptor(vertexDescriptor);
            }
        }

        private static IntPtr MakeStencilDescriptor(MetalRenderer renderer, StencilDefinition definition)
        {
            return MetalApi.metalbinding_new_stencil_descriptor(
                context: renderer.Context,
                stencilFailOp: TranslateStencilOperation(definition.StencilFailureOperation),
                depthFailOp: TranslateStencilOperation(definition.DepthFailureOperation),
                passOp: TranslateStencilOperation(definition.DepthStencilPassOperation),
                stencilCompareFunc: TranslateCompareFunction(definition.StencilCompareFunction),
                readMask: definition.ReadMask,
                writeMask: definition.WriteMask
            );
        }

        private static MetalApi.MTLBlendOperation TranslateBlendEquation(BlendEquation eq)
        {
            return eq switch {
                BlendEquation.FUNC_ADD => MetalApi.MTLBlendOperation.Add
                , BlendEquation.MIN => MetalApi.MTLBlendOperation.Min
                , BlendEquation.MAX => MetalApi.MTLBlendOperation.Max
                , BlendEquation.FUNC_SUBTRACT => MetalApi.MTLBlendOperation.Subtract
                , BlendEquation.FUNC_REVERSE_SUBTRACT => MetalApi.MTLBlendOperation.ReverseSubtract
                , _ => throw new ArgumentOutOfRangeException(nameof(eq), eq, null)
            };
        }

        private static MetalApi.MTLBlendFactor TranslateBlendFactor(BlendingFactor factor)
        {
            return factor switch {
                BlendingFactor.ZERO => MetalApi.MTLBlendFactor.Zero
                , BlendingFactor.ONE => MetalApi.MTLBlendFactor.One
                , BlendingFactor.SRC_COLOR => MetalApi.MTLBlendFactor.SourceColor
                , BlendingFactor.ONE_MINUS_SRC_COLOR => MetalApi.MTLBlendFactor.OneMinusSourceColor
                , BlendingFactor.SRC_ALPHA => MetalApi.MTLBlendFactor.SourceAlpha
                , BlendingFactor.ONE_MINUS_SRC_ALPHA => MetalApi.MTLBlendFactor.OneMinusSourceAlpha
                , BlendingFactor.DST_ALPHA => MetalApi.MTLBlendFactor.DestinationAlpha
                , BlendingFactor.ONE_MINUS_DST_ALPHA => MetalApi.MTLBlendFactor.OneMinusDestinationAlpha
                , BlendingFactor.DST_COLOR => MetalApi.MTLBlendFactor.DestinationColor
                , BlendingFactor.ONE_MINUS_DST_COLOR => MetalApi.MTLBlendFactor.OneMinusDestinationColor
                , BlendingFactor.SRC_ALPHA_SATURATE => MetalApi.MTLBlendFactor.SourceAlphaSaturated
                , BlendingFactor.CONSTANT_COLOR => MetalApi.MTLBlendFactor.BlendColor
                , BlendingFactor.ONE_MINUS_CONSTANT_COLOR => MetalApi.MTLBlendFactor.OneMinusBlendColor
                , BlendingFactor.CONSTANT_ALPHA => MetalApi.MTLBlendFactor.BlendAlpha
                , BlendingFactor.ONE_MINUS_CONSTANT_ALPHA => MetalApi.MTLBlendFactor.OneMinusBlendAlpha
                , BlendingFactor.SRC1_ALPHA => MetalApi.MTLBlendFactor.Source1Alpha
                , BlendingFactor.SRC1_COLOR => MetalApi.MTLBlendFactor.Source1Color
                , BlendingFactor.ONE_MINUS_SRC1_COLOR => MetalApi.MTLBlendFactor.OneMinusSource1Color
                , BlendingFactor.ONE_MINUS_SRC1_ALPHA => MetalApi.MTLBlendFactor.OneMinusSource1Alpha
                , _ => throw new ArgumentOutOfRangeException(nameof(factor), factor, null)
            };
        }

        private static MetalApi.MTLStencilOperation TranslateStencilOperation(StencilOperation op)
        {
            return op switch {
                StencilOperation.Keep => MetalApi.MTLStencilOperation.Keep
                , StencilOperation.Zero => MetalApi.MTLStencilOperation.Zero
                , StencilOperation.Replace => MetalApi.MTLStencilOperation.Replace
                , StencilOperation.IncrementClamp => MetalApi.MTLStencilOperation.IncrementClamp
                , StencilOperation.DecrementClamp => MetalApi.MTLStencilOperation.DecrementClamp
                , StencilOperation.Invert => MetalApi.MTLStencilOperation.Invert
                , StencilOperation.IncrementWrap => MetalApi.MTLStencilOperation.IncrementWrap
                , StencilOperation.DecrementWrap => MetalApi.MTLStencilOperation.DecrementWrap
                , _ => throw new ArgumentOutOfRangeException(nameof(op), op, null)
            };
        }

        private static MetalApi.MTLCompareFunction TranslateCompareFunction(CompareFunction func)
        {
            return func switch {
                CompareFunction.Never => MetalApi.MTLCompareFunction.Never
                , CompareFunction.Less => MetalApi.MTLCompareFunction.Less
                , CompareFunction.Equal => MetalApi.MTLCompareFunction.Equal
                , CompareFunction.LessEqual => MetalApi.MTLCompareFunction.LessEqual
                , CompareFunction.Greater => MetalApi.MTLCompareFunction.Greater
                , CompareFunction.NotEqual => MetalApi.MTLCompareFunction.NotEqual
                , CompareFunction.GreaterEqual => MetalApi.MTLCompareFunction.GreaterEqual
                , CompareFunction.Always => MetalApi.MTLCompareFunction.Always
                , _ => throw new ArgumentOutOfRangeException(nameof(func), func, null)
            };
        }

        private static MetalApi.MTLVertexFormat TranslateVertexFormat(VertexFormat format)
        {
            return format switch {
                VertexFormat.Invalid => MetalApi.MTLVertexFormat.Invalid
                , VertexFormat.UChar2 => MetalApi.MTLVertexFormat.UChar2
                , VertexFormat.UChar3 => MetalApi.MTLVertexFormat.UChar3
                , VertexFormat.UChar4 => MetalApi.MTLVertexFormat.UChar4
                , VertexFormat.Char2 => MetalApi.MTLVertexFormat.Char2
                , VertexFormat.Char3 => MetalApi.MTLVertexFormat.Char3
                , VertexFormat.Char4 => MetalApi.MTLVertexFormat.Char4
                , VertexFormat.UChar2Normalized => MetalApi.MTLVertexFormat.UChar2Normalized
                , VertexFormat.UChar3Normalized => MetalApi.MTLVertexFormat.UChar3Normalized
                , VertexFormat.UChar4Normalized => MetalApi.MTLVertexFormat.UChar4Normalized
                , VertexFormat.Char2Normalized => MetalApi.MTLVertexFormat.Char2Normalized
                , VertexFormat.Char3Normalized => MetalApi.MTLVertexFormat.Char3Normalized
                , VertexFormat.Char4Normalized => MetalApi.MTLVertexFormat.Char4Normalized
                , VertexFormat.UShort2 => MetalApi.MTLVertexFormat.UShort2
                , VertexFormat.UShort3 => MetalApi.MTLVertexFormat.UShort3
                , VertexFormat.UShort4 => MetalApi.MTLVertexFormat.UShort4
                , VertexFormat.Short2 => MetalApi.MTLVertexFormat.Short2
                , VertexFormat.Short3 => MetalApi.MTLVertexFormat.Short3
                , VertexFormat.Short4 => MetalApi.MTLVertexFormat.Short4
                , VertexFormat.UShort2Normalized => MetalApi.MTLVertexFormat.UShort2Normalized
                , VertexFormat.UShort3Normalized => MetalApi.MTLVertexFormat.UShort3Normalized
                , VertexFormat.UShort4Normalized => MetalApi.MTLVertexFormat.UShort4Normalized
                , VertexFormat.Short2Normalized => MetalApi.MTLVertexFormat.Short2Normalized
                , VertexFormat.Short3Normalized => MetalApi.MTLVertexFormat.Short3Normalized
                , VertexFormat.Short4Normalized => MetalApi.MTLVertexFormat.Short4Normalized
                , VertexFormat.Half2 => MetalApi.MTLVertexFormat.Half2
                , VertexFormat.Half3 => MetalApi.MTLVertexFormat.Half3
                , VertexFormat.Half4 => MetalApi.MTLVertexFormat.Half4
                , VertexFormat.Float => MetalApi.MTLVertexFormat.Float
                , VertexFormat.Float2 => MetalApi.MTLVertexFormat.Float2
                , VertexFormat.Float3 => MetalApi.MTLVertexFormat.Float3
                , VertexFormat.Float4 => MetalApi.MTLVertexFormat.Float4
                , VertexFormat.Int => MetalApi.MTLVertexFormat.Int
                , VertexFormat.Int2 => MetalApi.MTLVertexFormat.Int2
                , VertexFormat.Int3 => MetalApi.MTLVertexFormat.Int3
                , VertexFormat.Int4 => MetalApi.MTLVertexFormat.Int4
                , VertexFormat.UInt => MetalApi.MTLVertexFormat.UInt
                , VertexFormat.UInt2 => MetalApi.MTLVertexFormat.UInt2
                , VertexFormat.UInt3 => MetalApi.MTLVertexFormat.UInt3
                , VertexFormat.UInt4 => MetalApi.MTLVertexFormat.UInt4
                , VertexFormat.Int1010102Normalized => MetalApi.MTLVertexFormat.Int1010102Normalized
                , VertexFormat.UInt1010102Normalized => MetalApi.MTLVertexFormat.UInt1010102Normalized
                , VertexFormat.UChar4Normalized_BGRA => MetalApi.MTLVertexFormat.UChar4Normalized_BGRA
                , VertexFormat.UChar => MetalApi.MTLVertexFormat.UChar
                , VertexFormat.Char => MetalApi.MTLVertexFormat.Char
                , VertexFormat.UCharNormalized => MetalApi.MTLVertexFormat.UCharNormalized
                , VertexFormat.CharNormalized => MetalApi.MTLVertexFormat.CharNormalized
                , VertexFormat.UShort => MetalApi.MTLVertexFormat.UShort
                , VertexFormat.Short => MetalApi.MTLVertexFormat.Short
                , VertexFormat.UShortNormalized => MetalApi.MTLVertexFormat.UShortNormalized
                , VertexFormat.ShortNormalized => MetalApi.MTLVertexFormat.ShortNormalized
                , VertexFormat.Half => MetalApi.MTLVertexFormat.Half
                , _ => throw new ArgumentOutOfRangeException(nameof(format), format, null)
            };
        }

        private static MetalApi.MTLVertexStepFunction TranslateStepFunction(VertexStepFunction func)
        {
            return func switch {
                VertexStepFunction.PerVertex => MetalApi.MTLVertexStepFunction.PerVertex
                , VertexStepFunction.PerInstance => MetalApi.MTLVertexStepFunction.PerInstance
                , _ => throw new ArgumentOutOfRangeException(nameof(func), func, null)
            };
        }

        private void ReleaseUnmanagedResources()
        {
            if (_depthStencilState != IntPtr.Zero) {
                MetalApi.metalbinding_release_depth_stencil_state(_depthStencilState);
                _depthStencilState = IntPtr.Zero;
            }
            if (_pipelineState != IntPtr.Zero) {
                MetalApi.metalbinding_release_pipeline_state(_pipelineState);
                _pipelineState = IntPtr.Zero;
            }
        }

        public void Dispose()
        {
            ReleaseUnmanagedResources();
            GC.SuppressFinalize(this);
        }

        ~MetalPipeline() {
            ReleaseUnmanagedResources();
        }
    }
}