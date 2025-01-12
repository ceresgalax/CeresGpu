using System;
using System.Numerics;

namespace CeresGpu.Graphics;

public interface IFramebuffer : IDisposable
{
    void SetColorAttachmentProperties(int index, Vector4 clearColor);
    void SetDepthStencilAttachmentProperties(double clearDepth, uint clearStencil);
}
