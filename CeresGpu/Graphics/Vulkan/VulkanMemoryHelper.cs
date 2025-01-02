using Silk.NET.Vulkan;

namespace CeresGpu.Graphics.Vulkan;

public class VulkanMemoryHelper
{
    private readonly VulkanRenderer _renderer;
    
    public unsafe VulkanMemoryHelper(VulkanRenderer renderer)
    {
        _renderer = renderer;
        
        Vk vk = renderer.Vk;
        
        vk.GetPhysicalDeviceMemoryProperties(_renderer.PhysicalDevice, out PhysicalDeviceMemoryProperties properties);
        
        
    }
}