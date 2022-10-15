using System;
using CeresGpu.Graphics;
using Metalancer.MetalBinding;

namespace Metalancer.Graphics.Metal
{
    public sealed class MetalSampler : IDisposable
    {
        private IntPtr _sampler;

        public IntPtr Handle => _sampler;
        
        public MetalSampler(MetalRenderer renderer, MinMagFilter min, MinMagFilter mag)
        {
            _sampler = MetalApi.metalbinding_create_sampler(renderer.Context,
                TranslateMinMagFilter(min),
                TranslateMinMagFilter(mag),
                MetalApi.MTLSamplerMipFilter.NotMipmapped,
                normalizedCoordinates: true,
                supportArgumentBuffers: true);
        }

        private MetalApi.MTLSamplerMinMagFilter TranslateMinMagFilter(MinMagFilter filter)
        {
            return filter switch {
                MinMagFilter.Nearest => MetalApi.MTLSamplerMinMagFilter.Nearest
                , MinMagFilter.Linear => MetalApi.MTLSamplerMinMagFilter.Linear
                , _ => throw new ArgumentOutOfRangeException(nameof(filter), filter, null)
            };
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