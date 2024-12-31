using System;
using System.Collections.Generic;
using Silk.NET.Vulkan;

namespace CeresGpu.Graphics.Vulkan;

public class VulkanPassBacking : IDisposable
{
    public RenderPass RenderPass { get; }
    
    public VulkanPassBacking(IRenderPass renderPass)
    {
        List<AttachmentDescription> attachmentDescriptions = [];
        ReadOnlySpan<ColorAttachment> colorAttachments = renderPass.ColorAttachments;
        for (int colorAttachmentIndex = 0; colorAttachmentIndex < colorAttachments.Length; ++colorAttachmentIndex) {
            ref readonly ColorAttachment colorAttachment = ref colorAttachments[colorAttachmentIndex];
            attachmentDescriptions.Add(new AttachmentDescription(
                flags: AttachmentDescriptionFlags.None,
                format: colorAttachment.Format.ToVkFormat(), 
            ));   
        }

        
        RenderPassCreateInfo createInfo = new(
            StructureType.RenderPassCreateInfo,
            pNext: null,
            flags: RenderPassCreateFlags.None,
            
        );

    }

    private void ReleaseUnmanagedResources()
    {
        // Vulkan RenderPass objects are easy to destroy, no need to defer deletion, as they are not tangled up with
        // command buffer encoding. Yay!
        
    }

    public void Dispose()
    {
        ReleaseUnmanagedResources();
        GC.SuppressFinalize(this);
    }

    ~VulkanPassBacking()
    {
        ReleaseUnmanagedResources();
    }
}