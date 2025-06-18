using System;

namespace CeresGpu.Graphics
{
    public interface IBuffer<T> : IDisposable where T : unmanaged
    {
        /// <summary>
        /// Count of T elements that the buffer has been allocated to store.
        /// </summary>
        uint Count { get; }

        void Allocate(uint elementCount);
        
        
    }
}