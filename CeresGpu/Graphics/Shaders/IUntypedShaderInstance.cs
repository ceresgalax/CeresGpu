using System;

namespace CeresGpu.Graphics.Shaders
{
    public interface IUntypedShaderInstance : IDisposable
    {
        public IShaderInstanceBacking Backing { get; }
        public ReadOnlySpan<IDescriptorSet> GetDescriptorSets();
    }
}