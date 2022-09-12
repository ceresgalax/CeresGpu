namespace Metalancer.Graphics
{
    public record StencilDefinition
    {
        public StencilOperation StencilFailureOperation;
        public StencilOperation DepthFailureOperation;
        public StencilOperation DepthStencilPassOperation;
        public CompareFunction StencilCompareFunction = CompareFunction.Always;
        public uint ReadMask;
        public uint WriteMask;
    };
}