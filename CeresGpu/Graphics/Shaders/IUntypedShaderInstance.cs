using System;

namespace CeresGpu.Graphics.Shaders
{
    public interface IUntypedShaderInstance
    {
        public IShaderInstanceBacking Backing { get; }
        public ReadOnlySpan<IDescriptorSet> GetDescriptorSets();
    }
}