using System;

namespace CeresGpu.Graphics.Metal
{
    public interface IMetalBuffer : IBuffer, IDisposable
    {
        /// <summary>
        /// Returns the underlying buffer handle that represents the buffer contents for this frame.
        /// For example, StreamingBuffers are backed by multiple metal buffers, so that contents can be updated
        /// while previous frames are in flight.
        /// </summary>
        public IntPtr GetHandleForCurrentFrame();
        
        /// <summary>
        /// Called when the buffer is going to be updated outside CeresGPU.
        /// For example, encoding arguments into an argument buffer.
        /// This method is called to make sure the metal buffer actually exists and is ready for these external updates.
        /// </summary>
        public void PrepareToUpdateExternally();
    }
}