namespace CeresGpu.Graphics
{
    public record PipelineDefinition
    {
        public bool Blend;
        
        public BlendOp ColorBlendOp;
        public BlendOp AlphaBlendOp;
        
        public BlendFunction BlendFunction;
        public CullMode CullMode;
        public DepthStencilDefinition DepthStencil = new();
    }
}