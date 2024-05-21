using System;
using CeresGpu.MetalBinding;

namespace CeresGpu.Graphics.Metal
{
    public sealed class MetalSampler : ISampler
    {
        private IntPtr _sampler;

        public IntPtr Handle => _sampler;
        
        public MetalSampler(MetalRenderer renderer, in SamplerDescription description)
        {
            _sampler = MetalApi.metalbinding_create_sampler(renderer.Context,
                TranslateMinMagFilter(description.MinFilter),
                TranslateMinMagFilter(description.MinFilter),
                MetalApi.MTLSamplerMipFilter.NotMipmapped,
                TranslateAddressMode(description.DepthAddressMode),
                TranslateAddressMode(description.WidthAddressMode),
                TranslateAddressMode(description.HeightAddressMode),
                normalizedCoordinates: true,
                supportArgumentBuffers: true
            );
        }

        private MetalApi.MTLSamplerMinMagFilter TranslateMinMagFilter(MinMagFilter filter)
        {
            return filter switch {
                MinMagFilter.Nearest => MetalApi.MTLSamplerMinMagFilter.Nearest
                , MinMagFilter.Linear => MetalApi.MTLSamplerMinMagFilter.Linear
                , _ => throw new ArgumentOutOfRangeException(nameof(filter), filter, null)
            };
        }
        
        private MetalApi.MTLSamplerAddressMode TranslateAddressMode(SamplerAddressMode mode)
        {
            return mode switch {
                SamplerAddressMode.ClampToEdge => MetalApi.MTLSamplerAddressMode.ClampToEdge,
                SamplerAddressMode.Repeat => MetalApi.MTLSamplerAddressMode.Repeat,
                SamplerAddressMode.MirrorRepeat => MetalApi.MTLSamplerAddressMode.MirrorRepeat,
                _ => throw new ArgumentOutOfRangeException(nameof(mode), mode, null)
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