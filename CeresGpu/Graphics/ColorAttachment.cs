using System.Numerics;

namespace CeresGpu.Graphics;

public struct ColorAttachment
{
    public ITexture? Texture;
    public LoadAction LoadAction;
    public Vector4 ClearColor;
}