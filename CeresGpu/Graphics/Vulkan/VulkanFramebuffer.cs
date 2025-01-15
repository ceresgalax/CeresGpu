using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Silk.NET.Vulkan;
using ArgumentOutOfRangeException = System.ArgumentOutOfRangeException;

namespace CeresGpu.Graphics.Vulkan;


struct HashableFramebufferPermutation : IEquatable<HashableFramebufferPermutation>
{
    private readonly int[] _indices = [];
    private readonly int _hashCode;

    public HashableFramebufferPermutation(int[] indices)
    {
        _indices = indices;

        uint hashCode = 0;
        for (int i = 0; i < _indices.Length; ++i) {
            hashCode = (hashCode * 397) ^ (uint)_indices[i];
        }

        _hashCode = (int)hashCode;
    }

    public bool Equals(HashableFramebufferPermutation other)
    {
        if (_indices.Length != other._indices.Length) {
            return false;
        }
        
        for (int i = 0; i < _indices.Length; ++i) {
            if (_indices[i] != other._indices[i]) {
                return false;
            }
        }

        return true;
    }

    public override int GetHashCode()
    {
        return _hashCode;
    }
}

public class VulkanFramebuffer : IFramebuffer
{
    private record struct ColorAttachment(IVulkanRenderTarget? RenderTarget, Vector4 ClearColor);
    
    private readonly VulkanRenderer _renderer;
    private readonly VulkanPassBacking _passBacking;
    
    private readonly ColorAttachment[] _colorAttachments;
    private readonly IVulkanRenderTarget? _depthStencilAttachment;
    
    private double _depthClearValue;
    private uint _stencilClearValue;
    
    private readonly Dictionary<HashableFramebufferPermutation, Framebuffer> _framebuffers = [];
    
    private readonly int[] _reusedCurrentFrameIndices;
    
    public uint Width { get; private set; }
    public uint Height { get; private set; }
    
    public VulkanFramebuffer(VulkanRenderer renderer, VulkanPassBacking passBacking, ReadOnlySpan<IRenderTarget> colorAttachments, IRenderTarget? depthStencilAttachment)
    {
        _renderer = renderer;
        _passBacking = passBacking;
        
        FramebufferUtil.ValidateAttachments(in passBacking.Definition, colorAttachments, depthStencilAttachment, out uint width, out uint height, out _);
        
        _colorAttachments = new ColorAttachment[passBacking.Definition.ColorAttachments.Length];

        _reusedCurrentFrameIndices = new int[passBacking.Definition.ColorAttachments.Length +
                                             (passBacking.Definition.DepthStencilAttachment == null ? 0 : 1)];
        
        for (int i = 0; i < _colorAttachments.Length; ++i) {
            if (colorAttachments[i] is not IVulkanRenderTarget vulkanRenderTarget) {
                throw new ArgumentOutOfRangeException(nameof(colorAttachments));
            }
            _colorAttachments[i].RenderTarget = vulkanRenderTarget;
        }

        if (depthStencilAttachment != null) {
            if (depthStencilAttachment is not IVulkanRenderTarget vulkanRenderTarget) {
                throw new ArgumentException(nameof(depthStencilAttachment));
            }
            _depthStencilAttachment = vulkanRenderTarget;
        }
        
        foreach (int[] indices in CalculatePossibleFramebufferPermutations()) {
            _framebuffers[new HashableFramebufferPermutation(indices)] = CreateFramebuffer(indices, width, height);
        }

        Width = width;
        Height = height;
    }

    private Framebuffer CreateFramebuffer(int[] indices, uint width, uint height)
    {
        int numAttachmentViews = _passBacking.Definition.ColorAttachments.Length +
                                 (_passBacking.Definition.DepthStencilAttachment == null ? 0 : 1);
        ImageView[] attachmentViews = new ImageView[numAttachmentViews];
        for (int colorIndex = 0; colorIndex < _passBacking.Definition.ColorAttachments.Length; ++colorIndex) {
            attachmentViews[colorIndex] = _colorAttachments[colorIndex].RenderTarget?.GetImageView(indices[colorIndex]) ?? default;
        }

        if (_passBacking.Definition.DepthStencilAttachment != null) {
            attachmentViews[^1] = _depthStencilAttachment?.GetImageView(indices[^1]) ?? default;
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
                    width: width,
                    height: height,
                    layers: 1
                );
                _renderer.Vk.CreateFramebuffer(_renderer.Device, &createInfo, null, out Framebuffer framebuffer)
                    .AssertSuccess("Failed to create framebuffer");
                return framebuffer;
            }
        }
    }
    
    public void SetColorAttachmentProperties(int index, Vector4 clearColor)
    {
        _colorAttachments[index].ClearColor = clearColor;
    }

    public void SetDepthStencilAttachmentProperties(double clearDepth, uint clearStencil)
    {
        _depthClearValue = clearDepth;
        _stencilClearValue = clearStencil;
    }


    private IEnumerable<int[]> CalculatePossibleFramebufferPermutations()
    {
        List<int> staticAttachmentIndices = [];
        List<int> workingFrameVaryingIndices = [];
        List<int> fullyVaryingIndices = [];
        
        for (int i = 0; i < _colorAttachments.Length; ++i) {
            IVulkanRenderTarget? target = _colorAttachments[i].RenderTarget;
            if (target?.IsBufferedByWorkingFrame ?? true) {
                workingFrameVaryingIndices.Add(i);
            } else {
                fullyVaryingIndices.Add(i);
            }
        }

        if (_depthStencilAttachment != null) {
            if (_depthStencilAttachment.IsBufferedByWorkingFrame) {
                workingFrameVaryingIndices.Add(_colorAttachments.Length);
            } else {
                fullyVaryingIndices.Add(_colorAttachments.Length);
            }
        }

        int[] attachmentMapping = staticAttachmentIndices
            .Concat(workingFrameVaryingIndices)
            .Concat(fullyVaryingIndices)
            .ToArray();
        
        // Base on static attachment indices first
        IEnumerable<int[]> permutations = [Enumerable.Repeat(0, staticAttachmentIndices.Count).ToArray()];
        
        //Now add in working frame varying indices
        if (workingFrameVaryingIndices.Count > 0) {
            permutations = MakePermutation(permutations, workingFrameVaryingIndices.Count, _renderer.FrameCount);    
        }
        
        // Now add in full varying indices
        for (int i = 0; i < fullyVaryingIndices.Count; ++i) {
            // This assumes we always have swapchain images == FrameCount.
            permutations = MakePermutation(permutations, 1, _renderer.FrameCount);
        }
        
        // Now map all the columns back to correspond with their attachment indices.
        return permutations.Select(x => Remap(x, attachmentMapping));
    }

    private int[] Remap(int[] indices, int[] mappings)
    {
        if (indices.Length != mappings.Length) {
            throw new ArgumentOutOfRangeException(nameof(indices));
        }
        return mappings.Select(x => indices[x]).ToArray();
    }

    private IEnumerable<int[]> MakePermutation(IEnumerable<int[]> basedOn, int numElements, int numPermutations)
    {
        for (int i = 0; i < numPermutations; ++i) {
            foreach (int[] set in basedOn) {
                yield return set.Concat(Enumerable.Repeat(i, numElements)).ToArray();
            }    
        }
    }
    
    public Framebuffer GetFramebuffer()
    {
        for (int i = 0; i < _colorAttachments.Length; ++i) {
            _reusedCurrentFrameIndices[i] = _colorAttachments[i].RenderTarget?.ImageViewIndexForCurrentFrame ?? 0;
        }

        if (_depthStencilAttachment != null) {
            _reusedCurrentFrameIndices[^1] = _depthStencilAttachment.ImageViewIndexForCurrentFrame;
        }
        
        return _framebuffers[new HashableFramebufferPermutation(_reusedCurrentFrameIndices)];
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