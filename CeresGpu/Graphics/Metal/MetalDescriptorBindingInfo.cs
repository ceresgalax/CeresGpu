using CeresGpu.Graphics.Shaders;

namespace CeresGpu.Graphics.Metal;

public struct MetalDescriptorBindingInfo : IDescriptorBindingInfo
{
    /// <summary>
    /// The argument buffer index of this resource.
    /// </summary>
    public required uint BindingIndex;
    
    /// <summary>
    /// If the descriptor is for a texture, this index is the argument buffer index for it's related sampler.
    /// </summary>
    public uint SamplerIndex;
}