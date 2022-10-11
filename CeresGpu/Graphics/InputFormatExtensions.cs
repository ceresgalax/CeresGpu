using System;
using Metalancer.Graphics;

namespace CeresGpu.Graphics
{
    public static class InputFormatExtensions
    {
        public static int GetBytesPerPixel(this InputFormat format)
        {
            return format switch {
                InputFormat.R4G4_UNORM_PACK8 => 1
                , InputFormat.R4G4B4A4_UNORM_PACK16 => 2
                , InputFormat.B4G4R4A4_UNORM_PACK16 => 2
                , InputFormat.R5G6B5_UNORM_PACK16 => 2
                , InputFormat.B5G6R5_UNORM_PACK16 => 2
                , InputFormat.R5G5B5A1_UNORM_PACK16 => 2
                , InputFormat.B5G5R5A1_UNORM_PACK16 => 2
                , InputFormat.A1R5G5B5_UNORM_PACK16 => 2
                , InputFormat.R8_UNORM => 1
                , InputFormat.R8_SNORM => 1
                , InputFormat.R8_USCALED => 1
                , InputFormat.R8_SSCALED => 1
                , InputFormat.R8_UINT => 1
                , InputFormat.R8_SINT => 1
                , InputFormat.R8_SRGB => 1
                , InputFormat.R8G8_UNORM => 2
                , InputFormat.R8G8_SNORM => 2
                , InputFormat.R8G8_USCALED => 2
                , InputFormat.R8G8_SSCALED => 2
                , InputFormat.R8G8_UINT => 2
                , InputFormat.R8G8_SINT => 2
                , InputFormat.R8G8_SRGB => 2
                , InputFormat.R8G8B8_UNORM => 3
                , InputFormat.R8G8B8_SNORM => 3
                , InputFormat.R8G8B8_USCALED => 3
                , InputFormat.R8G8B8_SSCALED => 3
                , InputFormat.R8G8B8_UINT => 3
                , InputFormat.R8G8B8_SINT => 3
                , InputFormat.R8G8B8_SRGB => 3
                , InputFormat.B8G8R8_UNORM => 3
                , InputFormat.B8G8R8_SNORM => 3
                , InputFormat.B8G8R8_USCALED => 3
                , InputFormat.B8G8R8_SSCALED => 3
                , InputFormat.B8G8R8_UINT => 3
                , InputFormat.B8G8R8_SINT => 3
                , InputFormat.B8G8R8_SRGB => 3
                , InputFormat.R8G8B8A8_UNORM => 4
                , InputFormat.R8G8B8A8_SNORM => 4
                , InputFormat.R8G8B8A8_USCALED => 4
                , InputFormat.R8G8B8A8_SSCALED => 4
                , InputFormat.R8G8B8A8_UINT => 4
                , InputFormat.R8G8B8A8_SINT => 4
                , InputFormat.R8G8B8A8_SRGB => 4
                , InputFormat.B8G8R8A8_UNORM => 4
                , InputFormat.B8G8R8A8_SNORM => 4
                , InputFormat.B8G8R8A8_USCALED => 4
                , InputFormat.B8G8R8A8_SSCALED => 4
                , InputFormat.B8G8R8A8_UINT => 4
                , InputFormat.B8G8R8A8_SINT => 4
                , InputFormat.B8G8R8A8_SRGB => 4
                , InputFormat.A8B8G8R8_UNORM_PACK32 => 4
                , InputFormat.A8B8G8R8_SNORM_PACK32 => 4
                , InputFormat.A8B8G8R8_USCALED_PACK32 => 4
                , InputFormat.A8B8G8R8_SSCALED_PACK32 => 4
                , InputFormat.A8B8G8R8_UINT_PACK32 => 4
                , InputFormat.A8B8G8R8_SINT_PACK32 => 4
                , InputFormat.A8B8G8R8_SRGB_PACK32 => 4
                , InputFormat.A2R10G10B10_UNORM_PACK32 => 4
                , InputFormat.A2R10G10B10_SNORM_PACK32 => 4
                , InputFormat.A2R10G10B10_USCALED_PACK32 => 4
                , InputFormat.A2R10G10B10_SSCALED_PACK32 => 4
                , InputFormat.A2R10G10B10_UINT_PACK32 => 4
                , InputFormat.A2R10G10B10_SINT_PACK32 => 4
                , InputFormat.A2B10G10R10_UNORM_PACK32 => 4
                , InputFormat.A2B10G10R10_SNORM_PACK32 => 4
                , InputFormat.A2B10G10R10_USCALED_PACK32 => 4
                , InputFormat.A2B10G10R10_SSCALED_PACK32 => 4
                , InputFormat.A2B10G10R10_UINT_PACK32 => 4
                , InputFormat.A2B10G10R10_SINT_PACK32 => 4
                , InputFormat.R16_UNORM => 2
                , InputFormat.R16_SNORM => 2
                , InputFormat.R16_USCALED => 2
                , InputFormat.R16_SSCALED => 2
                , InputFormat.R16_UINT => 2
                , InputFormat.R16_SINT => 2
                , InputFormat.R16_SFLOAT => 2
                , InputFormat.R16G16_UNORM => 4
                , InputFormat.R16G16_SNORM => 4
                , InputFormat.R16G16_USCALED => 4
                , InputFormat.R16G16_SSCALED => 4
                , InputFormat.R16G16_UINT => 4
                , InputFormat.R16G16_SINT => 4
                , InputFormat.R16G16_SFLOAT => 4
                , InputFormat.R16G16B16_UNORM => 6
                , InputFormat.R16G16B16_SNORM => 6
                , InputFormat.R16G16B16_USCALED => 6
                , InputFormat.R16G16B16_SSCALED => 6
                , InputFormat.R16G16B16_UINT => 6
                , InputFormat.R16G16B16_SINT => 6
                , InputFormat.R16G16B16_SFLOAT => 6
                , InputFormat.R16G16B16A16_UNORM => 8
                , InputFormat.R16G16B16A16_SNORM => 8
                , InputFormat.R16G16B16A16_USCALED => 8
                , InputFormat.R16G16B16A16_SSCALED => 8
                , InputFormat.R16G16B16A16_UINT => 8
                , InputFormat.R16G16B16A16_SINT => 8
                , InputFormat.R16G16B16A16_SFLOAT => 8
                , InputFormat.R32_UINT => 4
                , InputFormat.R32_SINT => 4
                , InputFormat.R32_SFLOAT => 4
                , InputFormat.R32G32_UINT => 8
                , InputFormat.R32G32_SINT => 8
                , InputFormat.R32G32_SFLOAT => 8
                , InputFormat.R32G32B32_UINT => 12
                , InputFormat.R32G32B32_SINT => 12
                , InputFormat.R32G32B32_SFLOAT => 12
                , InputFormat.R32G32B32A32_UINT => 16
                , InputFormat.R32G32B32A32_SINT => 16
                , InputFormat.R32G32B32A32_SFLOAT => 16
                , InputFormat.R64_UINT => 8
                , InputFormat.R64_SINT => 8
                , InputFormat.R64_SFLOAT => 8
                , InputFormat.R64G64_UINT => 16
                , InputFormat.R64G64_SINT => 16
                , InputFormat.R64G64_SFLOAT => 16
                , InputFormat.R64G64B64_UINT => 24
                , InputFormat.R64G64B64_SINT => 24
                , InputFormat.R64G64B64_SFLOAT => 24
                , InputFormat.R64G64B64A64_UINT => 32
                , InputFormat.R64G64B64A64_SINT => 32
                , InputFormat.R64G64B64A64_SFLOAT => 32
                , InputFormat.B10G11R11_UFLOAT_PACK32 => 4
                , _ => throw new ArgumentOutOfRangeException(nameof(format), format, null)
            };
        }
    }
}