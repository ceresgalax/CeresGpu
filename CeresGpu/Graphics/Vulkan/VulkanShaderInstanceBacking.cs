namespace CeresGpu.Graphics.Vulkan;

public sealed class VulkanShaderInstanceBacking(VulkanShaderBacking shader) : IShaderInstanceBacking
{
    public readonly VulkanShaderBacking Shader = shader;
    
    public void Dispose()
    {
    }
}