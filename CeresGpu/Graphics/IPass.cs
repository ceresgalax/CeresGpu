using System;

namespace CeresGpu.Graphics
{
    public interface IPass<TRenderPass> : IPass, ICommandEncoder<TRenderPass>, IDisposable
        where TRenderPass : IRenderPass
    {
    }

    public interface IPass
    {
        void Finish();
    }
}