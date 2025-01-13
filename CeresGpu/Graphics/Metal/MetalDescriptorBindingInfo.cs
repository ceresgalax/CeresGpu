using CeresGpu.Graphics.Shaders;

namespace CeresGpu.Graphics.Metal;

public struct MetalDescriptorBindingInfo : IDescriptorBindingInfo
{
    /// <summary>
    /// The metal shader argument buffer index of the argument buffer this descriptor is encoded in. 
    /// </summary>
    public required uint FunctionArgumentBufferIndex;

    /// <summary>
    /// The CeresGPU IShader argument buffer index of the argument buffer this descriptor is encoded in. 
    /// </summary>
    public required int AbstractedBufferIndex;
    
    /// <summary>
    /// The Metal impl uses different argument buffers per shader stage.
    /// 
    /// </summary>
    public required ShaderStage Stage;
    
    /// <summary>
    /// The id of the argument buffer field containing this resources.
    /// </summary>
    public required uint BufferId;
    
    /// <summary>
    /// If the descriptor is for a texture, this index is the function's argument buffer index for it's related sampler.
    /// </summary>
    public uint SamplerBufferId;
}