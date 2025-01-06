using System;

namespace CeresGpu.Graphics.Shaders;

public interface IDescriptorBindingInfo
{
}

public struct DescriptorInfo
{
    public IDescriptorBindingInfo Binding;

    /// <summary>
    /// What type of descriptor should be used. This is meant to be used by shader introspection tooling.
    ///
    /// Not used by CeresGpu itself.
    /// </summary>
    public DescriptorType DescriptorType;

    /// <summary>
    /// The generated type which the generated descriptor set method in the ShaderInstance accepts.
    /// This type is configured to match the expected buffer layout.
    /// This is meant to be used by shader introspection tooling.
    ///
    /// Not used by CeresGpu itself.
    /// </summary>
    public Type? BufferType;

    /// <summary>
    /// The name of the descriptor in the shader. Meant for use by shader introspection.
    ///
    /// Not used by CeresGpu itself.
    /// </summary>
    public string? Name;

    public string? Hint;

}