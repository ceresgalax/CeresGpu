using System;

namespace Metalancer.Graphics.Shaders
{
    public interface IUntypedShaderInstance
    {
        public IShaderInstanceBacking Backing { get; }
        public ReadOnlySpan<IDescriptorSet> GetDescriptorSets();
    }
}