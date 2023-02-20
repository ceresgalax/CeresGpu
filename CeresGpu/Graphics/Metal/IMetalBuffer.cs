using System;

namespace CeresGpu.Graphics.Metal
{
    public interface IMetalBuffer
    {
        public IntPtr GetHandleForCurrentFrame();
        public void ThrowIfNotReadyForUse();
        public void PrepareToUpdateExternally();
    }
}