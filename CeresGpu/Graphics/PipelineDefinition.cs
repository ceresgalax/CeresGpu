namespace CeresGpu.Graphics
{
    public record PipelineDefinition
    {
        public bool Blend;
        public BlendEquation BlendEquation;
        public BlendFunction BlendFunction;
        public CullMode CullMode;
        public DepthStencilDefinition DepthStencil = new();
    }
}