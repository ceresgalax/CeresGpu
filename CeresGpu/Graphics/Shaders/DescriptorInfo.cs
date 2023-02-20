namespace CeresGpu.Graphics.Shaders
{
    public struct DescriptorInfo
    {
        public int BindingIndex;

        /// <summary>
        /// If the descriptor is for a texture, this index is the argument buffer index for it's related sampler.
        /// </summary>
        public int SamplerIndex;

        // TODO: Vulkan Descriptor data
    }
}