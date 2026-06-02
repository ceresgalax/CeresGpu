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
    /// The value of the id attribute applied to this resource's pointer in the MSL argument buffer structure
    /// If null, this means that the output metal shader doesn't actually use this resource, and the generated MSL
    /// argument buffer structure doesn't contain a pointer to this resource.
    /// </summary>
    public required uint? BufferId;
    
    /// <summary>
    /// If the descriptor is for a texture, this index is the function's argument buffer index for it's related sampler.
    /// If null, this means that the output metal shader doesn't actually use this resource, and the generated MSL
    /// argument buffer structure doesn't contain a pointer to this resource.
    /// </summary>
    public uint? SamplerBufferId;
}