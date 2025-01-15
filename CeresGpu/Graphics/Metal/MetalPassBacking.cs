namespace CeresGpu.Graphics.Metal;

public sealed class MetalPassBacking
{
    public readonly RenderPassDefinition Definition;

    public MetalPassBacking(RenderPassDefinition definition)
    {
        Definition = definition;
    }
}