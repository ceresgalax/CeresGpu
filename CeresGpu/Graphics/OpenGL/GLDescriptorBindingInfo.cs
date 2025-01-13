using CeresGpu.Graphics.Shaders;

namespace CeresGpu.Graphics.OpenGL;

public struct GLDescriptorBindingInfo : IDescriptorBindingInfo
{
    /// <summary>
    /// The uniform location of this descriptor in the glsl shader.
    /// </summary>
    public required uint Location;
}