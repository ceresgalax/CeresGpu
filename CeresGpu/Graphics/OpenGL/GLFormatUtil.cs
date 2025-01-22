using System;
using CeresGL;

namespace CeresGpu.Graphics.OpenGL;

public static class GLFormatUtil
{
    
    public static (InternalFormat, PixelFormat, PixelType) GetGLFormats(this ColorFormat format)
    {
        return format switch {
            ColorFormat.R4G4_UNORM_PACK8 => throw new NotImplementedException()
            , ColorFormat.R4G4B4A4_UNORM_PACK16 => throw new NotImplementedException()
            , ColorFormat.B4G4R4A4_UNORM_PACK16 => throw new NotImplementedException()
            , ColorFormat.R5G6B5_UNORM_PACK16 => throw new NotImplementedException()
            , ColorFormat.B5G6R5_UNORM_PACK16 => throw new NotImplementedException()
            , ColorFormat.R5G5B5A1_UNORM_PACK16 => throw new NotImplementedException()
            , ColorFormat.B5G5R5A1_UNORM_PACK16 => throw new NotImplementedException()
            , ColorFormat.A1R5G5B5_UNORM_PACK16 => throw new NotImplementedException()
            , ColorFormat.R8_UNORM => (InternalFormat.R8, PixelFormat.RED, PixelType.UNSIGNED_BYTE)
            , ColorFormat.R8_SNORM => throw new NotImplementedException()
            , ColorFormat.R8_USCALED => throw new NotImplementedException()
            , ColorFormat.R8_SSCALED => throw new NotImplementedException()
            , ColorFormat.R8_UINT => throw new NotImplementedException()
            , ColorFormat.R8_SINT => throw new NotImplementedException()
            , ColorFormat.R8_SRGB => throw new NotImplementedException()
            , ColorFormat.R8G8_UNORM => throw new NotImplementedException()
            , ColorFormat.R8G8_SNORM => throw new NotImplementedException()
            , ColorFormat.R8G8_USCALED => throw new NotImplementedException()
            , ColorFormat.R8G8_SSCALED => throw new NotImplementedException()
            , ColorFormat.R8G8_UINT => throw new NotImplementedException()
            , ColorFormat.R8G8_SINT => throw new NotImplementedException()
            , ColorFormat.R8G8_SRGB => throw new NotImplementedException()
            
            // Removed - See comment in InputFormat.
            //, InputFormat.R8G8B8_UNORM => (InternalFormat.RGB, PixelFormat.RGB, PixelType.UNSIGNED_BYTE)
            //, InputFormat.R8G8B8_SNORM => throw new NotImplementedException()
            //, InputFormat.R8G8B8_USCALED => throw new NotImplementedException()
            //, InputFormat.R8G8B8_SSCALED => throw new NotImplementedException()
            //, InputFormat.R8G8B8_UINT => throw new NotImplementedException()
            //, InputFormat.R8G8B8_SINT => throw new NotImplementedException()
            //, InputFormat.R8G8B8_SRGB => throw new NotImplementedException()
            //, InputFormat.B8G8R8_UNORM => throw new NotImplementedException()
            //, InputFormat.B8G8R8_SNORM => throw new NotImplementedException()
            //, InputFormat.B8G8R8_USCALED => throw new NotImplementedException()
            //, InputFormat.B8G8R8_SSCALED => throw new NotImplementedException()
            //, InputFormat.B8G8R8_UINT => throw new NotImplementedException()
            //, InputFormat.B8G8R8_SINT => throw new NotImplementedException()
            //, InputFormat.B8G8R8_SRGB => throw new NotImplementedException()
            
            , ColorFormat.R8G8B8A8_UNORM => (InternalFormat.RGBA, PixelFormat.RGBA, PixelType.UNSIGNED_BYTE)
            , ColorFormat.R8G8B8A8_SNORM => throw new NotImplementedException()
            , ColorFormat.R8G8B8A8_USCALED => throw new NotImplementedException()
            , ColorFormat.R8G8B8A8_SSCALED => throw new NotImplementedException()
            , ColorFormat.R8G8B8A8_UINT => throw new NotImplementedException()
            , ColorFormat.R8G8B8A8_SINT => throw new NotImplementedException()
            , ColorFormat.R8G8B8A8_SRGB => throw new NotImplementedException()
            , ColorFormat.B8G8R8A8_UNORM => (InternalFormat.RGBA, PixelFormat.BGRA, PixelType.UNSIGNED_BYTE)
            , ColorFormat.B8G8R8A8_SNORM => throw new NotImplementedException()
            , ColorFormat.B8G8R8A8_USCALED => throw new NotImplementedException()
            , ColorFormat.B8G8R8A8_SSCALED => throw new NotImplementedException()
            , ColorFormat.B8G8R8A8_UINT => throw new NotImplementedException()
            , ColorFormat.B8G8R8A8_SINT => throw new NotImplementedException()
            , ColorFormat.B8G8R8A8_SRGB => throw new NotImplementedException()
            , ColorFormat.A8B8G8R8_UNORM_PACK32 => throw new NotImplementedException()
            , ColorFormat.A8B8G8R8_SNORM_PACK32 => throw new NotImplementedException()
            , ColorFormat.A8B8G8R8_USCALED_PACK32 => throw new NotImplementedException()
            , ColorFormat.A8B8G8R8_SSCALED_PACK32 => throw new NotImplementedException()
            , ColorFormat.A8B8G8R8_UINT_PACK32 => throw new NotImplementedException()
            , ColorFormat.A8B8G8R8_SINT_PACK32 => throw new NotImplementedException()
            , ColorFormat.A8B8G8R8_SRGB_PACK32 => throw new NotImplementedException()
            , ColorFormat.A2R10G10B10_UNORM_PACK32 => throw new NotImplementedException()
            , ColorFormat.A2R10G10B10_SNORM_PACK32 => throw new NotImplementedException()
            , ColorFormat.A2R10G10B10_USCALED_PACK32 => throw new NotImplementedException()
            , ColorFormat.A2R10G10B10_SSCALED_PACK32 => throw new NotImplementedException()
            , ColorFormat.A2R10G10B10_UINT_PACK32 => throw new NotImplementedException()
            , ColorFormat.A2R10G10B10_SINT_PACK32 => throw new NotImplementedException()
            , ColorFormat.A2B10G10R10_UNORM_PACK32 => throw new NotImplementedException()
            , ColorFormat.A2B10G10R10_SNORM_PACK32 => throw new NotImplementedException()
            , ColorFormat.A2B10G10R10_USCALED_PACK32 => throw new NotImplementedException()
            , ColorFormat.A2B10G10R10_SSCALED_PACK32 => throw new NotImplementedException()
            , ColorFormat.A2B10G10R10_UINT_PACK32 => throw new NotImplementedException()
            , ColorFormat.A2B10G10R10_SINT_PACK32 => throw new NotImplementedException()
            , ColorFormat.R16_UNORM => throw new NotImplementedException()
            , ColorFormat.R16_SNORM => throw new NotImplementedException()
            , ColorFormat.R16_USCALED => throw new NotImplementedException()
            , ColorFormat.R16_SSCALED => throw new NotImplementedException()
            , ColorFormat.R16_UINT => throw new NotImplementedException()
            , ColorFormat.R16_SINT => throw new NotImplementedException()
            , ColorFormat.R16_SFLOAT => throw new NotImplementedException()
            , ColorFormat.R16G16_UNORM => throw new NotImplementedException()
            , ColorFormat.R16G16_SNORM => throw new NotImplementedException()
            , ColorFormat.R16G16_USCALED => throw new NotImplementedException()
            , ColorFormat.R16G16_SSCALED => throw new NotImplementedException()
            , ColorFormat.R16G16_UINT => throw new NotImplementedException()
            , ColorFormat.R16G16_SINT => throw new NotImplementedException()
            , ColorFormat.R16G16_SFLOAT => throw new NotImplementedException()
            , ColorFormat.R16G16B16_UNORM => throw new NotImplementedException()
            , ColorFormat.R16G16B16_SNORM => throw new NotImplementedException()
            , ColorFormat.R16G16B16_USCALED => throw new NotImplementedException()
            , ColorFormat.R16G16B16_SSCALED => throw new NotImplementedException()
            , ColorFormat.R16G16B16_UINT => throw new NotImplementedException()
            , ColorFormat.R16G16B16_SINT => throw new NotImplementedException()
            , ColorFormat.R16G16B16_SFLOAT => throw new NotImplementedException()
            , ColorFormat.R16G16B16A16_UNORM => throw new NotImplementedException()
            , ColorFormat.R16G16B16A16_SNORM => throw new NotImplementedException()
            , ColorFormat.R16G16B16A16_USCALED => throw new NotImplementedException()
            , ColorFormat.R16G16B16A16_SSCALED => throw new NotImplementedException()
            , ColorFormat.R16G16B16A16_UINT => throw new NotImplementedException()
            , ColorFormat.R16G16B16A16_SINT => throw new NotImplementedException()
            , ColorFormat.R16G16B16A16_SFLOAT => throw new NotImplementedException()
            , ColorFormat.R32_UINT => throw new NotImplementedException()
            , ColorFormat.R32_SINT => throw new NotImplementedException()
            , ColorFormat.R32_SFLOAT => throw new NotImplementedException()
            , ColorFormat.R32G32_UINT => throw new NotImplementedException()
            , ColorFormat.R32G32_SINT => throw new NotImplementedException()
            , ColorFormat.R32G32_SFLOAT => throw new NotImplementedException()
            , ColorFormat.R32G32B32_UINT => throw new NotImplementedException()
            , ColorFormat.R32G32B32_SINT => throw new NotImplementedException()
            , ColorFormat.R32G32B32_SFLOAT => throw new NotImplementedException()
            , ColorFormat.R32G32B32A32_UINT => throw new NotImplementedException()
            , ColorFormat.R32G32B32A32_SINT => throw new NotImplementedException()
            , ColorFormat.R32G32B32A32_SFLOAT => throw new NotImplementedException()
            , ColorFormat.R64_UINT => throw new NotImplementedException()
            , ColorFormat.R64_SINT => throw new NotImplementedException()
            , ColorFormat.R64_SFLOAT => throw new NotImplementedException()
            , ColorFormat.R64G64_UINT => throw new NotImplementedException()
            , ColorFormat.R64G64_SINT => throw new NotImplementedException()
            , ColorFormat.R64G64_SFLOAT => throw new NotImplementedException()
            , ColorFormat.R64G64B64_UINT => throw new NotImplementedException()
            , ColorFormat.R64G64B64_SINT => throw new NotImplementedException()
            , ColorFormat.R64G64B64_SFLOAT => throw new NotImplementedException()
            , ColorFormat.R64G64B64A64_UINT => throw new NotImplementedException()
            , ColorFormat.R64G64B64A64_SINT => throw new NotImplementedException()
            , ColorFormat.R64G64B64A64_SFLOAT => throw new NotImplementedException()
            , ColorFormat.B10G11R11_UFLOAT_PACK32 => throw new NotImplementedException()
            , _ => throw new ArgumentOutOfRangeException(nameof(format), format, null)
        };
    }

    public static InternalFormat ToGLInternalFormat(this DepthStencilFormat format)
    {
        return format switch {
            DepthStencilFormat.D16_UNORM => InternalFormat.DEPTH_COMPONENT16,
            //DepthStencilFormat.X8D24_UNORM_PACK32 => throw new NotImplementedException(),
            DepthStencilFormat.D32_SFLOAT => InternalFormat.DEPTH_COMPONENT32F,
            DepthStencilFormat.S8_UINT => InternalFormat.STENCIL_INDEX8,
            //DepthStencilFormat.D16_UNORM_S8_UINT => throw new NotImplementedException(),
            DepthStencilFormat.D24_UNORM_S8_UINT => InternalFormat.DEPTH24_STENCIL8,
            DepthStencilFormat.D32_SFLOAT_S8_UINT => InternalFormat.DEPTH32F_STENCIL8,
            _ => throw new ArgumentOutOfRangeException(nameof(format), format, null)
        };
    }

    public static FramebufferAttachment GetAttachmentPointBasedOnFormat(this DepthStencilFormat format)
    {
        return format switch {
            DepthStencilFormat.D16_UNORM => FramebufferAttachment.DEPTH_ATTACHMENT,
            DepthStencilFormat.D32_SFLOAT => FramebufferAttachment.DEPTH_ATTACHMENT,
            DepthStencilFormat.S8_UINT => FramebufferAttachment.STENCIL_ATTACHMENT,
            DepthStencilFormat.D24_UNORM_S8_UINT => FramebufferAttachment.DEPTH_STENCIL_ATTACHMENT,
            DepthStencilFormat.D32_SFLOAT_S8_UINT => FramebufferAttachment.DEPTH_STENCIL_ATTACHMENT,
            _ => throw new ArgumentOutOfRangeException(nameof(format), format, null)
        };
    }
    
}