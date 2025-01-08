using System;
using Silk.NET.Vulkan;
using VkSamplerAddressMode = Silk.NET.Vulkan.SamplerAddressMode;

namespace CeresGpu.Graphics.Vulkan;

public sealed class VulkanSampler : ISampler, IDeferredDisposable
{
    private readonly VulkanRenderer _renderer;
    public readonly Sampler Sampler;
    
    public unsafe VulkanSampler(VulkanRenderer renderer, in SamplerDescription description)
    {
        _renderer = renderer;

        SamplerCreateInfo createInfo = new (
            sType: StructureType.SamplerCreateInfo,
            pNext: null,
            flags: SamplerCreateFlags.None,
            magFilter: TranslateMinMagFilter(description.MagFilter),
            minFilter: TranslateMinMagFilter(description.MinFilter),
            mipmapMode: SamplerMipmapMode.Nearest,
            addressModeU: TranslateAddressMode(description.WidthAddressMode),
            addressModeV: TranslateAddressMode(description.HeightAddressMode),
            addressModeW: TranslateAddressMode(description.DepthAddressMode),
            mipLodBias: 0f,
            anisotropyEnable: false,
            maxAnisotropy: 1f,
            compareEnable: false,
            compareOp: CompareOp.Never,
            minLod: 0f,
            maxLod: float.MaxValue,
            borderColor:BorderColor.FloatTransparentBlack,
            unnormalizedCoordinates: false
        );
        renderer.Vk.CreateSampler(renderer.Device, in createInfo, null, out Sampler)
            .AssertSuccess("Failed to create sampler");
    }
    
    unsafe void IDeferredDisposable.DeferredDispose()
    {
        _renderer.Vk.DestroySampler(_renderer.Device, Sampler, null);
    }
    
    private void ReleaseUnmanagedResources()
    {
        _renderer.DeferDisposal(this);
    }

    private bool _isDisposed;
    
    public void Dispose()
    {
        if (_isDisposed) {
            throw new ObjectDisposedException(null);
        }
        _isDisposed = true;
        
        ReleaseUnmanagedResources();
        GC.SuppressFinalize(this);
    }

    ~VulkanSampler()
    {
        ReleaseUnmanagedResources();
    }


    public static Filter TranslateMinMagFilter(MinMagFilter filter)
    {
        return filter switch {
            MinMagFilter.Nearest => Filter.Nearest,
            MinMagFilter.Linear => Filter.Linear,
            _ => throw new ArgumentOutOfRangeException(nameof(filter), filter, null)
        };
    }

    public static VkSamplerAddressMode TranslateAddressMode(SamplerAddressMode mode)
    {
        return mode switch {
            SamplerAddressMode.ClampToEdge => VkSamplerAddressMode.ClampToEdge,
            SamplerAddressMode.Repeat => VkSamplerAddressMode.Repeat,
            SamplerAddressMode.MirrorRepeat => VkSamplerAddressMode.MirroredRepeat,
            _ => throw new ArgumentOutOfRangeException(nameof(mode), mode, null)
        };
    }
}