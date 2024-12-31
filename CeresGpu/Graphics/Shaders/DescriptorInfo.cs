using System;

namespace CeresGpu.Graphics.Shaders
{
    public struct DescriptorInfo
    {
        public uint BindingIndex;

        /// <summary>
        /// If the descriptor is for a texture, this index is the argument buffer index for it's related sampler.
        /// </summary>
        public int SamplerIndex;

        // Note: More information may be required for Vulkan in order for it to properly set up it's descriptor sets.

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
}