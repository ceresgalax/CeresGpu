using System;
using System.Numerics;
using Silk.NET.Vulkan;

namespace CeresGpu.Graphics.Vulkan;

public class VulkanFramebuffer : IMutableFramebuffer
    //where TRenderPass : IRenderPass
{
    private record struct ColorAttachment(IVulkanTexture? Texture, Vector4 ClearColor);
    
    private readonly VulkanRenderer _renderer;
    private readonly VulkanPassBacking _passBacking;
    
    private readonly ColorAttachment[] _colorAttachments;
    private IVulkanTexture? _depthStencilAttachment;
    private double _depthClearValue;
    private uint _stencilClearValue;

    private readonly Framebuffer[] _framebuffers;
    
    private uint _currentWidth;
    private uint _currentHeight;

    private uint _targetWidth;
    private uint _targetHeight;

    private bool _needNewFramebuffers;
    
    public uint Width => _currentWidth;
    public uint Height => _currentHeight;
    
    public unsafe VulkanFramebuffer(VulkanRenderer renderer, VulkanPassBacking passBacking)
    {
        _renderer = renderer;
        _passBacking = passBacking;
        
        _colorAttachments = new ColorAttachment[passBacking.Definition.ColorAttachments.Length];

        _framebuffers = new Framebuffer[renderer.FrameCount];
        
        _needNewFramebuffers = true;
    }


    public void Setup(uint width, uint height)
    {
        _targetWidth = width;
        _targetHeight = height;

        for (int i = 0; i < _colorAttachments.Length; ++i) {
            _colorAttachments[i].Texture = null;
        }
        _depthStencilAttachment = null;

        _needNewFramebuffers = true;
    }
    
    public void SetColorAttachment(int index, ITexture texture, Vector4 clearColor)
    {
        if (texture is not IVulkanTexture vulkanTexture) {
            throw new ArgumentException("Texture must be a VulkanTexture", nameof(texture));
        }
        
        // TODO: Throw if incompatible with RenderPassDefinition?
        _colorAttachments[index] = new ColorAttachment(vulkanTexture, clearColor);
    }

    public void SetDepthStencilAttachment(ITexture texture, double clearDepth, uint clearStencil)
    {
        if (texture is not IVulkanTexture vulkanTexture) {
            throw new ArgumentException("Texture must be a VulkanTexture", nameof(texture));
        }
        
        // TODO: Throw if incompatible with RenderPassDefinition?
        _depthStencilAttachment = vulkanTexture;
        _depthClearValue = clearDepth;
        _stencilClearValue = clearStencil;
    }

    public Framebuffer GetFramebuffer()
    {
        Vk vk = _renderer.Vk;
        
        if (_needNewFramebuffers) {
            // bye bye old framebuffers
            foreach (Framebuffer framebuffer in _framebuffers) {
                if (framebuffer.Handle == 0) {
                    continue;
                }
                unsafe {
                    vk.DestroyFramebuffer(_renderer.Device, framebuffer, null);
                }
            }
            
            _currentWidth = _targetWidth;
            _currentHeight = _targetHeight;
        }

        if (_framebuffers[_renderer.WorkingFrame].Handle == 0) {
            // Need ourselves a framebuffer

            int numAttachmentViews = _passBacking.Definition.ColorAttachments.Length +
                                     (_passBacking.Definition.DepthStencilAttachment == null ? 0 : 1);
            ImageView[] attachmentViews = new ImageView[numAttachmentViews];
            for (int colorIndex = 0; colorIndex < _passBacking.Definition.ColorAttachments.Length; ++colorIndex) {
                // TODO: Should we assert earlier that all color attachments have been set before we attempt to 
                //  use this framebuffer again after calling any of the mutating methods? 
                attachmentViews[colorIndex] = _colorAttachments[colorIndex].Texture?.GetFramebufferView() ?? default;
            }

            if (_passBacking.Definition.DepthStencilAttachment != null) {
                // TODO: Should we assert earlier that the depth stencil attachment has been set before we attempt to 
                //  use this framebuffer again after calling any of the mutating methods? 
                attachmentViews[^1] = _depthStencilAttachment?.GetFramebufferView() ?? default;
            }
            
            unsafe {
                fixed (ImageView* pAttachmentViews = attachmentViews) {
                    FramebufferCreateInfo createInfo = new(
                        sType: StructureType.FramebufferCreateInfo,
                        pNext: null,
                        flags: FramebufferCreateFlags.None,
                        renderPass: _passBacking.RenderPass,
                        attachmentCount: (uint)attachmentViews.Length,
                        pAttachments: pAttachmentViews,
                        width: _currentWidth,
                        height: _currentHeight,
                        layers: 1
                    );
                    vk.CreateFramebuffer(_renderer.Device, &createInfo, null, out Framebuffer framebuffer)
                        .AssertSuccess("Failed to create framebuffer for current working frame.");
                    
                    _framebuffers[_renderer.WorkingFrame] = framebuffer;
                }
            }
        }

        return _framebuffers[_renderer.WorkingFrame];
    }

    public ClearColorValue GetClearColor(int colorAttachmentIndex)
    {
        Vector4 clearColor = _colorAttachments[colorAttachmentIndex].ClearColor;
        switch (_passBacking.Definition.ColorAttachments[colorAttachmentIndex].Format) {
            case ColorFormat.R4G4_UNORM_PACK8:
            case ColorFormat.R4G4B4A4_UNORM_PACK16:
            case ColorFormat.B4G4R4A4_UNORM_PACK16:
            case ColorFormat.R5G6B5_UNORM_PACK16:
            case ColorFormat.B5G6R5_UNORM_PACK16:
            case ColorFormat.R5G5B5A1_UNORM_PACK16:
            case ColorFormat.B5G5R5A1_UNORM_PACK16:
            case ColorFormat.A1R5G5B5_UNORM_PACK16:
            case ColorFormat.R8_UNORM:
            case ColorFormat.R8_SNORM:
            case ColorFormat.R8_USCALED:
            case ColorFormat.R8_SSCALED:
            case ColorFormat.R8_SRGB:
            case ColorFormat.R8G8_UNORM:
            case ColorFormat.R8G8_SNORM:
            case ColorFormat.R8G8_USCALED:
            case ColorFormat.R8G8_SSCALED:
            case ColorFormat.R8G8_SRGB:
            case ColorFormat.R8G8B8A8_UNORM:
            case ColorFormat.R8G8B8A8_SNORM:
            case ColorFormat.R8G8B8A8_USCALED:
            case ColorFormat.R8G8B8A8_SSCALED:
            case ColorFormat.R8G8B8A8_SRGB:
            case ColorFormat.B8G8R8A8_UNORM:
            case ColorFormat.B8G8R8A8_SNORM:
            case ColorFormat.B8G8R8A8_USCALED:
            case ColorFormat.B8G8R8A8_SSCALED:
            case ColorFormat.B8G8R8A8_SRGB:
            case ColorFormat.A8B8G8R8_UNORM_PACK32:
            case ColorFormat.A8B8G8R8_SNORM_PACK32:
            case ColorFormat.A8B8G8R8_USCALED_PACK32:
            case ColorFormat.A8B8G8R8_SSCALED_PACK32:
            case ColorFormat.A8B8G8R8_SRGB_PACK32:
            case ColorFormat.A2R10G10B10_UNORM_PACK32:
            case ColorFormat.A2R10G10B10_SNORM_PACK32:
            case ColorFormat.A2R10G10B10_USCALED_PACK32:
            case ColorFormat.A2R10G10B10_SSCALED_PACK32:
            case ColorFormat.A2B10G10R10_UNORM_PACK32:
            case ColorFormat.A2B10G10R10_SNORM_PACK32:
            case ColorFormat.A2B10G10R10_USCALED_PACK32:
            case ColorFormat.A2B10G10R10_SSCALED_PACK32:
            case ColorFormat.R16_UNORM:
            case ColorFormat.R16_SNORM:
            case ColorFormat.R16_USCALED:
            case ColorFormat.R16_SSCALED:
            case ColorFormat.R16_SFLOAT:
            case ColorFormat.R16G16_UNORM:
            case ColorFormat.R16G16_SNORM:
            case ColorFormat.R16G16_USCALED:
            case ColorFormat.R16G16_SSCALED:
            case ColorFormat.R16G16_SFLOAT:
            case ColorFormat.R16G16B16_UNORM:
            case ColorFormat.R16G16B16_SNORM:
            case ColorFormat.R16G16B16_USCALED:
            case ColorFormat.R16G16B16_SSCALED:
            case ColorFormat.R16G16B16_SFLOAT:
            case ColorFormat.R16G16B16A16_UNORM:
            case ColorFormat.R16G16B16A16_SNORM:
            case ColorFormat.R16G16B16A16_USCALED:
            case ColorFormat.R16G16B16A16_SSCALED:
            case ColorFormat.R16G16B16A16_SFLOAT:
            case ColorFormat.R32_SFLOAT:
            case ColorFormat.R32G32_SFLOAT:
            case ColorFormat.R32G32B32_SFLOAT:
            case ColorFormat.R32G32B32A32_SFLOAT:
            case ColorFormat.R64_SFLOAT:
            case ColorFormat.R64G64_SFLOAT:
            case ColorFormat.R64G64B64_SFLOAT:
            case ColorFormat.R64G64B64A64_SFLOAT:
            case ColorFormat.B10G11R11_UFLOAT_PACK32:
                return new ClearColorValue(clearColor.X, clearColor.Y, clearColor.Z, clearColor.W);
            
            case ColorFormat.R8_UINT:
            case ColorFormat.R8G8_UINT:
            case ColorFormat.R8G8B8A8_UINT:
            case ColorFormat.B8G8R8A8_UINT:
            case ColorFormat.A8B8G8R8_UINT_PACK32:
            case ColorFormat.A2R10G10B10_UINT_PACK32:
            case ColorFormat.A2B10G10R10_UINT_PACK32:
            case ColorFormat.R16_UINT:
            case ColorFormat.R16G16_UINT:
            case ColorFormat.R16G16B16_UINT:
            case ColorFormat.R16G16B16A16_UINT:
            case ColorFormat.R32_UINT:
            case ColorFormat.R32G32_UINT:
            case ColorFormat.R32G32B32_UINT:
            case ColorFormat.R32G32B32A32_UINT:
            case ColorFormat.R64_UINT:
            case ColorFormat.R64G64_UINT:
            case ColorFormat.R64G64B64_UINT:
            case ColorFormat.R64G64B64A64_UINT:
                return new ClearColorValue(null, null, null, null,
                    (int)clearColor.X, (int)clearColor.Y, (int)clearColor.Z, (int)clearColor.W);
            
            case ColorFormat.R8_SINT:
            case ColorFormat.R8G8_SINT:
            case ColorFormat.R8G8B8A8_SINT:
            case ColorFormat.B8G8R8A8_SINT:
            case ColorFormat.A8B8G8R8_SINT_PACK32:
            case ColorFormat.A2R10G10B10_SINT_PACK32:
            case ColorFormat.A2B10G10R10_SINT_PACK32:
            case ColorFormat.R16_SINT:
            case ColorFormat.R16G16_SINT:
            case ColorFormat.R16G16B16_SINT:
            case ColorFormat.R16G16B16A16_SINT:
            case ColorFormat.R32_SINT:
            case ColorFormat.R32G32_SINT:
            case ColorFormat.R32G32B32_SINT:
            case ColorFormat.R32G32B32A32_SINT:
            case ColorFormat.R64_SINT:
            case ColorFormat.R64G64_SINT:
            case ColorFormat.R64G64B64_SINT:
            case ColorFormat.R64G64B64A64_SINT:
                return new ClearColorValue(
                    null, null, null, null,
                    null, null, null, null,
                    (uint)clearColor.X, (uint)clearColor.Y, (uint)clearColor.Z, (uint)clearColor.W);
                    
            default:
                throw new ArgumentOutOfRangeException();
        }
    }

    public ClearDepthStencilValue GetClearDepthStencil()
    {
        return new ClearDepthStencilValue((float)_depthClearValue, _stencilClearValue);
    }
    
    private void ReleaseUnmanagedResources()
    {
        // TODO release unmanaged resources here
    }

    public void Dispose()
    {
        ReleaseUnmanagedResources();
        GC.SuppressFinalize(this);
    }

    ~VulkanFramebuffer()
    {
        ReleaseUnmanagedResources();
    }
}