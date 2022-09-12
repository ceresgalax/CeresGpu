using System;

namespace Metalancer.Graphics.Metal
{
    public interface IMetalBuffer
    {
        public IntPtr GetHandleForCurrentFrame();
        public void ThrowIfNotReadyForUse();
        public void PrepareToUpdateExternally();
    }
}