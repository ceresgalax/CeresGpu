using System;

namespace CeresGpu.Graphics.Metal;

public interface IMetalRenderTarget
{
    IntPtr GetCurrentFrameDrawable();
}