namespace CeresGpu.Graphics.Vulkan;

public static class AlignUtil
{
    public static ulong AlignUp(ulong value, ulong alignment)
    {
        return (value + alignment - 1) & ~(alignment - 1);
    }

    public static ulong AlignDown(ulong value, ulong alignment)
    {
        return value & ~(alignment - 1);
    }
}