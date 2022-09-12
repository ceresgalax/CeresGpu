using System;

namespace Metalancer.Graphics
{
    public interface IPass : ICommandEncoder, IDisposable
    {
        void Finish();
    }
}