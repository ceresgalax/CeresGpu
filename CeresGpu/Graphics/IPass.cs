using System;

namespace CeresGpu.Graphics;

// public interface IPass<TRenderPass> : IPass, ICommandEncoder<TRenderPass>
//     where TRenderPass : IRenderPass
// {
// }
public interface IPass : ICommandEncoder
{
    void Finish();
}

// public interface IPass
// {
//     void Finish();
// }