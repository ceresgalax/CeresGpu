using CeresGpu.Graphics.Shaders;

namespace CeresGpu.Graphics;

public record struct SamplerDescription
{
    public MinMagFilter MinFilter;
    public MinMagFilter MagFilter;
    public SamplerAddressMode DepthAddressMode;
    public SamplerAddressMode WidthAddressMode;
    public SamplerAddressMode HeightAddressMode;
};