using System;

namespace CeresGpu.Graphics;

public interface IPass : ICommandEncoder
{
    // TODO: Is this needed anymore?
    void Finish();
}