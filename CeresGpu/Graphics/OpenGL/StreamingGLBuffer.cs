using CeresGL;

namespace CeresGpu.Graphics.OpenGL
{
    public sealed class StreamingGLBuffer<T> : GLBuffer<T> where T : unmanaged
    {
        public StreamingGLBuffer(IGLProvider glProvider) : base(glProvider) { }
        
        protected override BufferUsageARB GetBufferUsage()
        {
            return BufferUsageARB.STREAM_DRAW;
        }
    }
}