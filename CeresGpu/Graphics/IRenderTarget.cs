using System;

namespace CeresGpu.Graphics;

public interface IRenderTarget : ISampleable, IDisposable
{
    uint Width { get; }
    uint Height { get; }
}