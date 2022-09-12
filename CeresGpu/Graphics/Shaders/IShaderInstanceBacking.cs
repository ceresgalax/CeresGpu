using System;

namespace Metalancer.Graphics
{
    public interface IShaderInstanceBacking : IDisposable
    {
        void SetVertexBuffer<T>(IBuffer<T> buffer, int index) where T : unmanaged;
    }
}