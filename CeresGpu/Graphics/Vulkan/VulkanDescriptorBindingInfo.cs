using CeresGpu.Graphics.Shaders;

namespace CeresGpu.Graphics.Vulkan;

public struct VulkanDescriptorBindingInfo : IDescriptorBindingInfo
{
    public required uint Set;
    public required uint Binding;
}