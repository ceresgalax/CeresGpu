using System;
using Silk.NET.Vulkan;

namespace CeresGpu.Graphics.Vulkan;

public class VulkanMemoryHelper
{
    private readonly VulkanRenderer _renderer;
    private readonly PhysicalDeviceMemoryProperties _physicalDeviceMemoryProperties;
    
    public unsafe VulkanMemoryHelper(VulkanRenderer renderer)
    {
        _renderer = renderer;
        Vk vk = renderer.Vk;
        vk.GetPhysicalDeviceMemoryProperties(_renderer.PhysicalDevice, out _physicalDeviceMemoryProperties);
    }

    public bool FindMemoryType(uint allowedIndexBits, MemoryPropertyFlags requiredProperties, out uint foundIndex)
    {
        // Vulkan's memory properties ordering requirement enables a simple search loop to select the desired memory type.
        
        for (uint memoryTypeIndex = 0; memoryTypeIndex < _physicalDeviceMemoryProperties.MemoryTypeCount; ++memoryTypeIndex) {
            if ((allowedIndexBits & (1 << (int)memoryTypeIndex)) == 0) {
                // Index is not contained in allowedIndexBits.
                continue;
            }
            
            ref readonly MemoryType memoryType = ref _physicalDeviceMemoryProperties.MemoryTypes[(int)memoryTypeIndex];

            if ((memoryType.PropertyFlags & requiredProperties) == requiredProperties) {
                foundIndex = memoryTypeIndex;
                return true;
            }
        }

        foundIndex = 0;
        return false;
    }
}