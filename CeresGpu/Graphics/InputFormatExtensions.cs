using System;
using CeresGpu.Graphics;

namespace CeresGpu.Graphics
{
    public static class InputFormatExtensions
    {
        public static int GetBytesPerPixel(this ColorFormat format)
        {
            return format switch {
                ColorFormat.R4G4_UNORM_PACK8 => 1
                , ColorFormat.R4G4B4A4_UNORM_PACK16 => 2
                , ColorFormat.B4G4R4A4_UNORM_PACK16 => 2
                , ColorFormat.R5G6B5_UNORM_PACK16 => 2
                , ColorFormat.B5G6R5_UNORM_PACK16 => 2
                , ColorFormat.R5G5B5A1_UNORM_PACK16 => 2
                , ColorFormat.B5G5R5A1_UNORM_PACK16 => 2
                , ColorFormat.A1R5G5B5_UNORM_PACK16 => 2
                , ColorFormat.R8_UNORM => 1
                , ColorFormat.R8_SNORM => 1
                , ColorFormat.R8_USCALED => 1
                , ColorFormat.R8_SSCALED => 1
                , ColorFormat.R8_UINT => 1
                , ColorFormat.R8_SINT => 1
                , ColorFormat.R8_SRGB => 1
                , ColorFormat.R8G8_UNORM => 2
                , ColorFormat.R8G8_SNORM => 2
                , ColorFormat.R8G8_USCALED => 2
                , ColorFormat.R8G8_SSCALED => 2
                , ColorFormat.R8G8_UINT => 2
                , ColorFormat.R8G8_SINT => 2
                , ColorFormat.R8G8_SRGB => 2
                
                // Removed - See comment in InputFormat.
                //, InputFormat.R8G8B8_UNORM => 3
                //, InputFormat.R8G8B8_SNORM => 3
                //, InputFormat.R8G8B8_USCALED => 3
                //, InputFormat.R8G8B8_SSCALED => 3
                //, InputFormat.R8G8B8_UINT => 3
                //, InputFormat.R8G8B8_SINT => 3
                //, InputFormat.R8G8B8_SRGB => 3
                //, InputFormat.B8G8R8_UNORM => 3
                //, InputFormat.B8G8R8_SNORM => 3
                //, InputFormat.B8G8R8_USCALED => 3
                //, InputFormat.B8G8R8_SSCALED => 3
                //, InputFormat.B8G8R8_UINT => 3
                //, InputFormat.B8G8R8_SINT => 3
                //, InputFormat.B8G8R8_SRGB => 3
                
                , ColorFormat.R8G8B8A8_UNORM => 4
                , ColorFormat.R8G8B8A8_SNORM => 4
                , ColorFormat.R8G8B8A8_USCALED => 4
                , ColorFormat.R8G8B8A8_SSCALED => 4
                , ColorFormat.R8G8B8A8_UINT => 4
                , ColorFormat.R8G8B8A8_SINT => 4
                , ColorFormat.R8G8B8A8_SRGB => 4
                , ColorFormat.B8G8R8A8_UNORM => 4
                , ColorFormat.B8G8R8A8_SNORM => 4
                , ColorFormat.B8G8R8A8_USCALED => 4
                , ColorFormat.B8G8R8A8_SSCALED => 4
                , ColorFormat.B8G8R8A8_UINT => 4
                , ColorFormat.B8G8R8A8_SINT => 4
                , ColorFormat.B8G8R8A8_SRGB => 4
                , ColorFormat.A8B8G8R8_UNORM_PACK32 => 4
                , ColorFormat.A8B8G8R8_SNORM_PACK32 => 4
                , ColorFormat.A8B8G8R8_USCALED_PACK32 => 4
                , ColorFormat.A8B8G8R8_SSCALED_PACK32 => 4
                , ColorFormat.A8B8G8R8_UINT_PACK32 => 4
                , ColorFormat.A8B8G8R8_SINT_PACK32 => 4
                , ColorFormat.A8B8G8R8_SRGB_PACK32 => 4
                , ColorFormat.A2R10G10B10_UNORM_PACK32 => 4
                , ColorFormat.A2R10G10B10_SNORM_PACK32 => 4
                , ColorFormat.A2R10G10B10_USCALED_PACK32 => 4
                , ColorFormat.A2R10G10B10_SSCALED_PACK32 => 4
                , ColorFormat.A2R10G10B10_UINT_PACK32 => 4
                , ColorFormat.A2R10G10B10_SINT_PACK32 => 4
                , ColorFormat.A2B10G10R10_UNORM_PACK32 => 4
                , ColorFormat.A2B10G10R10_SNORM_PACK32 => 4
                , ColorFormat.A2B10G10R10_USCALED_PACK32 => 4
                , ColorFormat.A2B10G10R10_SSCALED_PACK32 => 4
                , ColorFormat.A2B10G10R10_UINT_PACK32 => 4
                , ColorFormat.A2B10G10R10_SINT_PACK32 => 4
                , ColorFormat.R16_UNORM => 2
                , ColorFormat.R16_SNORM => 2
                , ColorFormat.R16_USCALED => 2
                , ColorFormat.R16_SSCALED => 2
                , ColorFormat.R16_UINT => 2
                , ColorFormat.R16_SINT => 2
                , ColorFormat.R16_SFLOAT => 2
                , ColorFormat.R16G16_UNORM => 4
                , ColorFormat.R16G16_SNORM => 4
                , ColorFormat.R16G16_USCALED => 4
                , ColorFormat.R16G16_SSCALED => 4
                , ColorFormat.R16G16_UINT => 4
                , ColorFormat.R16G16_SINT => 4
                , ColorFormat.R16G16_SFLOAT => 4
                , ColorFormat.R16G16B16_UNORM => 6
                , ColorFormat.R16G16B16_SNORM => 6
                , ColorFormat.R16G16B16_USCALED => 6
                , ColorFormat.R16G16B16_SSCALED => 6
                , ColorFormat.R16G16B16_UINT => 6
                , ColorFormat.R16G16B16_SINT => 6
                , ColorFormat.R16G16B16_SFLOAT => 6
                , ColorFormat.R16G16B16A16_UNORM => 8
                , ColorFormat.R16G16B16A16_SNORM => 8
                , ColorFormat.R16G16B16A16_USCALED => 8
                , ColorFormat.R16G16B16A16_SSCALED => 8
                , ColorFormat.R16G16B16A16_UINT => 8
                , ColorFormat.R16G16B16A16_SINT => 8
                , ColorFormat.R16G16B16A16_SFLOAT => 8
                , ColorFormat.R32_UINT => 4
                , ColorFormat.R32_SINT => 4
                , ColorFormat.R32_SFLOAT => 4
                , ColorFormat.R32G32_UINT => 8
                , ColorFormat.R32G32_SINT => 8
                , ColorFormat.R32G32_SFLOAT => 8
                , ColorFormat.R32G32B32_UINT => 12
                , ColorFormat.R32G32B32_SINT => 12
                , ColorFormat.R32G32B32_SFLOAT => 12
                , ColorFormat.R32G32B32A32_UINT => 16
                , ColorFormat.R32G32B32A32_SINT => 16
                , ColorFormat.R32G32B32A32_SFLOAT => 16
                , ColorFormat.R64_UINT => 8
                , ColorFormat.R64_SINT => 8
                , ColorFormat.R64_SFLOAT => 8
                , ColorFormat.R64G64_UINT => 16
                , ColorFormat.R64G64_SINT => 16
                , ColorFormat.R64G64_SFLOAT => 16
                , ColorFormat.R64G64B64_UINT => 24
                , ColorFormat.R64G64B64_SINT => 24
                , ColorFormat.R64G64B64_SFLOAT => 24
                , ColorFormat.R64G64B64A64_UINT => 32
                , ColorFormat.R64G64B64A64_SINT => 32
                , ColorFormat.R64G64B64A64_SFLOAT => 32
                , ColorFormat.B10G11R11_UFLOAT_PACK32 => 4
                , _ => throw new ArgumentOutOfRangeException(nameof(format), format, null)
            };
        }
    }
}