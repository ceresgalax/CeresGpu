using System;
using CeresGL;
using CeresGpu.Graphics.Shaders;

namespace CeresGpu.Graphics.OpenGL
{
    public class GLPipeline<TShader, TVertexBufferLayout> : IGLPipeline, IPipeline<TShader, TVertexBufferLayout> 
        where TShader : IShader
        where TVertexBufferLayout : IVertexBufferLayout<TShader>
    {
        private readonly PipelineDefinition _definition;
        private readonly TShader _shader;
        
        public GLPipeline(PipelineDefinition definition, TShader shader, TVertexBufferLayout vertexBufferLayout)
        {
            _definition = definition;
            _shader = shader;
        }

        public void Dispose() { }
        
        public void Setup(GL gl)
        {
            PipelineDefinition def = _definition;
            
            // public bool Blend;
            SetCap(gl, EnableCap.BLEND, def.Blend);
            
            // public BlendEquation BlendEquation;
            gl.BlendEquation(TranslateBlendEquation(def.BlendEquation));
            
            // public BlendFunction BlendFunction;
            gl.BlendFuncSeparate(
                sfactorRGB: TranslateBlendingFactor(def.BlendFunction.SourceRGB),
                dfactorRGB: TranslateBlendingFactor(def.BlendFunction.DestinationRGB),
                sfactorAlpha: TranslateBlendingFactor(def.BlendFunction.SourceAlpha),
                dfactorAlpha: TranslateBlendingFactor(def.BlendFunction.DestinationAlpha)
            );
            
            // public CullMode CullMode;
            SetCap(gl, EnableCap.CULL_FACE, def.CullMode != CullMode.None);
            gl.CullFace(def.CullMode == CullMode.Front ? CullFaceMode.FRONT : CullFaceMode.BACK);
            
            // public DepthStencilDefinition DepthStencil = new();
            // ->
            // public CompareFunction DepthCompareFunction = CompareFunction.Always;
            DepthStencilDefinition ddef = def.DepthStencil;
            gl.DepthFunc(TranslateToDepthFunction(ddef.DepthCompareFunction));
            
            // public bool DepthWriteEnabled;
            gl.DepthMask(ddef.DepthWriteEnabled);

            // public StencilDefinition BackFaceStencil = new();
            // public StencilDefinition FrontFaceStencil = new()
            // ->
            SetupStencilDef(gl, ddef.BackFaceStencil, StencilFaceDirection.BACK);
            SetupStencilDef(gl, ddef.FrontFaceStencil, StencilFaceDirection.FRONT);

            if (_shader.Backing is not GLShaderBacking shaderBacking) {
                throw new InvalidOperationException("Invalid shader backing");
            }

            gl.glUseProgram(shaderBacking.Program);
        }

        private static void SetupStencilDef(GL gl, StencilDefinition def, StencilFaceDirection face)
        {
            // public StencilOperation StencilFailureOperation;
            // public StencilOperation DepthFailureOperation;
            // public StencilOperation DepthStencilPassOperation;
            
            gl.StencilOpSeparate(face,
                sfail: TranslateToStencilOp(def.StencilFailureOperation),
                dpfail: TranslateToStencilOp(def.DepthFailureOperation),
                dppass: TranslateToStencilOp(def.DepthStencilPassOperation)
            );
            
            // public CompareFunction StencilCompareFunction = CompareFunction.Always;
            // public uint ReadMask;
            // public uint WriteMask
            // TODO: IPass doesn't expose setting reference stencil values yet, so use 0
            gl.StencilFuncSeparate(face, TranslateToStencilFunction(def.StencilCompareFunction), 0, def.ReadMask);
            gl.StencilMaskSeparate(face, def.WriteMask);
        }
        
        private static void SetCap(GL gl, EnableCap cap, bool value)
        {
            if (value) {
                gl.Enable(cap);
            } else {
                gl.Disable(cap);
            }
        }

        private static BlendEquationModeEXT TranslateBlendEquation(BlendEquation equ)
        {
            return equ switch {
                BlendEquation.FUNC_ADD => BlendEquationModeEXT.FUNC_ADD
                , BlendEquation.MIN => BlendEquationModeEXT.MIN
                , BlendEquation.MAX => BlendEquationModeEXT.MAX
                , BlendEquation.FUNC_SUBTRACT => BlendEquationModeEXT.FUNC_SUBTRACT
                , BlendEquation.FUNC_REVERSE_SUBTRACT => BlendEquationModeEXT.FUNC_REVERSE_SUBTRACT
                , _ => throw new ArgumentOutOfRangeException(nameof(equ), equ, null)
            };
        }

        private static CeresGL.BlendingFactor TranslateBlendingFactor(BlendingFactor factor)
        {
            return factor switch {
                BlendingFactor.ZERO => CeresGL.BlendingFactor.ZERO
                , BlendingFactor.ONE => CeresGL.BlendingFactor.ONE
                , BlendingFactor.SRC_COLOR => CeresGL.BlendingFactor.SRC_COLOR
                , BlendingFactor.ONE_MINUS_SRC_COLOR => CeresGL.BlendingFactor.ONE_MINUS_SRC_COLOR
                , BlendingFactor.SRC_ALPHA => CeresGL.BlendingFactor.SRC_ALPHA
                , BlendingFactor.ONE_MINUS_SRC_ALPHA => CeresGL.BlendingFactor.ONE_MINUS_SRC_ALPHA
                , BlendingFactor.DST_ALPHA => CeresGL.BlendingFactor.DST_ALPHA
                , BlendingFactor.ONE_MINUS_DST_ALPHA => CeresGL.BlendingFactor.ONE_MINUS_DST_ALPHA
                , BlendingFactor.DST_COLOR => CeresGL.BlendingFactor.DST_COLOR
                , BlendingFactor.ONE_MINUS_DST_COLOR => CeresGL.BlendingFactor.ONE_MINUS_DST_COLOR
                , BlendingFactor.SRC_ALPHA_SATURATE => CeresGL.BlendingFactor.SRC_ALPHA_SATURATE
                , BlendingFactor.CONSTANT_COLOR => CeresGL.BlendingFactor.CONSTANT_COLOR
                , BlendingFactor.ONE_MINUS_CONSTANT_COLOR => CeresGL.BlendingFactor.ONE_MINUS_CONSTANT_COLOR
                , BlendingFactor.CONSTANT_ALPHA => CeresGL.BlendingFactor.CONSTANT_ALPHA
                , BlendingFactor.ONE_MINUS_CONSTANT_ALPHA => CeresGL.BlendingFactor.ONE_MINUS_CONSTANT_ALPHA
                , BlendingFactor.SRC1_ALPHA => CeresGL.BlendingFactor.SRC1_ALPHA
                , BlendingFactor.SRC1_COLOR => CeresGL.BlendingFactor.SRC1_COLOR
                , BlendingFactor.ONE_MINUS_SRC1_COLOR => CeresGL.BlendingFactor.ONE_MINUS_SRC1_COLOR
                , BlendingFactor.ONE_MINUS_SRC1_ALPHA => CeresGL.BlendingFactor.ONE_MINUS_SRC1_ALPHA
                , _ => throw new ArgumentOutOfRangeException(nameof(factor), factor, null)
            };
        }

        private static DepthFunction TranslateToDepthFunction(CompareFunction compareFunction)
        {
            return compareFunction switch {
                CompareFunction.Never => DepthFunction.NEVER
                , CompareFunction.Less => DepthFunction.LESS  
                , CompareFunction.Equal => DepthFunction.EQUAL
                , CompareFunction.LessEqual => DepthFunction.LEQUAL
                , CompareFunction.Greater => DepthFunction.GREATER
                , CompareFunction.NotEqual => DepthFunction.NOTEQUAL
                , CompareFunction.GreaterEqual => DepthFunction.GEQUAL
                , CompareFunction.Always => DepthFunction.ALWAYS
                , _ => throw new ArgumentOutOfRangeException(nameof(compareFunction), compareFunction, null)
            };
        }

        private static StencilFunction TranslateToStencilFunction(CompareFunction compareFunction)
        {
            return compareFunction switch {
                CompareFunction.Never => StencilFunction.NEVER
                , CompareFunction.Less => StencilFunction.LESS  
                , CompareFunction.Equal => StencilFunction.EQUAL
                , CompareFunction.LessEqual => StencilFunction.LEQUAL
                , CompareFunction.Greater => StencilFunction.GREATER
                , CompareFunction.NotEqual => StencilFunction.NOTEQUAL
                , CompareFunction.GreaterEqual => StencilFunction.GEQUAL
                , CompareFunction.Always => StencilFunction.ALWAYS
                , _ => throw new ArgumentOutOfRangeException(nameof(compareFunction), compareFunction, null)
            };
        }

        private static StencilOp TranslateToStencilOp(StencilOperation op)
        {
            return op switch {
                StencilOperation.Keep => StencilOp.KEEP
                , StencilOperation.Zero => StencilOp.ZERO
                , StencilOperation.Replace => StencilOp.REPLACE
                , StencilOperation.IncrementClamp => StencilOp.INCR
                , StencilOperation.DecrementClamp => StencilOp.DECR
                , StencilOperation.Invert => StencilOp.INVERT
                , StencilOperation.IncrementWrap => StencilOp.INCR_WRAP
                , StencilOperation.DecrementWrap => StencilOp.DECR_WRAP
                , _ => throw new ArgumentOutOfRangeException(nameof(op), op, null)
            };
        }

    }
}