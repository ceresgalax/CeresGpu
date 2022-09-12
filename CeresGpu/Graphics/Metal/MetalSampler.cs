using System;
using Metalancer.MetalBinding;

namespace Metalancer.Graphics.Metal
{
    public sealed class MetalSampler : IDisposable
    {
        private IntPtr _sampler;

        public IntPtr Handle => _sampler;
        
        public MetalSampler(MetalRenderer renderer)
        {
            _sampler = MetalApi.metalbinding_create_sampler(renderer.Context);
        }

        private void ReleaseUnmanagedResources()
        {
            if (_sampler != IntPtr.Zero) {
                MetalApi.metalbinding_release_sampler(_sampler);
                _sampler = IntPtr.Zero;
            }
        }

        public void Dispose()
        {
            ReleaseUnmanagedResources();
            GC.SuppressFinalize(this);
        }

        ~MetalSampler() {
            ReleaseUnmanagedResources();
        }
    }
}