namespace CeresGpu.Graphics
{
    public record DepthStencilDefinition
    {
        public CompareFunction DepthCompareFunction = CompareFunction.Always;
        public bool DepthWriteEnabled;
        public StencilDefinition BackFaceStencil = new();
        public StencilDefinition FrontFaceStencil = new();
    }
}