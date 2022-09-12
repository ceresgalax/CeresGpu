using CeresGL;

namespace Metalancer.Graphics.OpenGL
{
    public sealed class StaticGLBuffer<T> : GLBuffer<T> where T : unmanaged
    {
        public StaticGLBuffer(IGLProvider glProvider) : base(glProvider)
        {
        }

        protected override BufferUsageARB GetBufferUsage()
        {
            return BufferUsageARB.STATIC_DRAW;
        }
    }
}