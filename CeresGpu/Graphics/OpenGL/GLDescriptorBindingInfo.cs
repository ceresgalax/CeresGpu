using CeresGpu.Graphics.Shaders;

namespace CeresGpu.Graphics.OpenGL;

public struct GLDescriptorBindingInfo : IDescriptorBindingInfo
{
    /// <summary>
    /// The binding index of this resource.
    /// </summary>
    public required uint BindingIndex;
}