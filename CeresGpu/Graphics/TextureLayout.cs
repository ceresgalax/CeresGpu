namespace CeresGpu.Graphics;

/// <summary>
/// Virtual texture layout types for <see cref="ITexture"/>s.
/// </summary>
public enum TextureLayout
{
    Undefined,
    General,
    ColorAttachmentOptimal,
    DepthStencilAttachmentOptimal,
    DepthStencilReadOnlyOptimal,
    ShaderReadOnlyOptimal,
    TransferSrcOptimal,
    TransferDstOptimal,
    Preinitialized
}