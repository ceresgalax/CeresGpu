using System;

namespace CeresGpu.Graphics
{
    public interface IPass : ICommandEncoder, IDisposable
    {
        void Finish();
    }
}