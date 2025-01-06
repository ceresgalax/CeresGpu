using System;
using System.Collections.Generic;
using Silk.NET.Vulkan;

namespace CeresGpu.Graphics.Vulkan;

public class VulkanPassBacking : IDisposable
{
    private readonly VulkanRenderer _renderer;

    public readonly RenderPassDefinition Definition;
    public readonly RenderPass RenderPass;
    
    public unsafe VulkanPassBacking(VulkanRenderer renderer, RenderPassDefinition passDefinition)
    {
        _renderer = renderer;
        Definition = passDefinition;
        
        List<AttachmentDescription> attachmentDescriptions = [];
        List<AttachmentReference> colorReferences = [];
        AttachmentReference depthStencilReference;
        
        ReadOnlySpan<ColorAttachment> colorAttachments = passDefinition.ColorAttachments;
        for (int attachmentIndex = 0; attachmentIndex < colorAttachments.Length; ++attachmentIndex) {
            ref readonly ColorAttachment colorAttachment = ref colorAttachments[attachmentIndex];
            
            uint vkAttachmentIndex = (uint)attachmentDescriptions.Count;
            
            attachmentDescriptions.Add(new AttachmentDescription(
                flags: AttachmentDescriptionFlags.None,
                format: colorAttachment.Format.ToVkFormat(),
                samples: SampleCountFlags.Count1Bit,
                loadOp: TranslateLoadAction(colorAttachment.LoadAction),
                storeOp: AttachmentStoreOp.Store,
                stencilLoadOp: AttachmentLoadOp.DontCare, // This attachment won't be used as stencil, so we don't care.
                stencilStoreOp: AttachmentStoreOp.DontCare,
                initialLayout: ImageLayout.ColorAttachmentOptimal, 
                finalLayout: ImageLayout.PresentSrcKhr
            ));
            
            // TODO: Again, layout probably needs to be exposed by the CeresGPU api.
            colorReferences.Add(new AttachmentReference(vkAttachmentIndex, ImageLayout.ColorAttachmentOptimal));
        }

        if (passDefinition.DepthStencilAttachment.HasValue) {
            DepthStencilAttachment attachment = passDefinition.DepthStencilAttachment.Value;
            
            uint vkAttachmentIndex = (uint)attachmentDescriptions.Count;
            
            attachmentDescriptions.Add(new AttachmentDescription(
                flags: AttachmentDescriptionFlags.None,
                format: attachment.Format.ToVkFormat(),
                samples: SampleCountFlags.Count1Bit,
                loadOp: TranslateLoadAction(attachment.LoadAction),
                storeOp: AttachmentStoreOp.Store,
                stencilLoadOp: TranslateLoadAction(attachment.LoadAction), // TODO: Based on format, we might not have a stencil component, and could set the load/store based on if this is present or not.
                stencilStoreOp: AttachmentStoreOp.Store,  // TODO: Same as above
                initialLayout: ImageLayout.DepthStencilAttachmentOptimal, 
                finalLayout: ImageLayout.DepthStencilAttachmentOptimal
            ));
            
            depthStencilReference = new AttachmentReference(vkAttachmentIndex, ImageLayout.DepthStencilAttachmentOptimal);
        } else {
            depthStencilReference.Attachment = Vk.AttachmentUnused;
        }
        
        AttachmentDescription[] attachmentDescriptionsArray = attachmentDescriptions.ToArray();
        AttachmentReference[] colorReferencesArray = colorReferences.ToArray();

        fixed (AttachmentReference* pColorReferences = colorReferencesArray) {

            SubpassDescription subpassDescription = new SubpassDescription(
                flags: SubpassDescriptionFlags.None,
                pipelineBindPoint: PipelineBindPoint.Graphics,
                inputAttachmentCount: 0,
                pInputAttachments: null,
                colorAttachmentCount: (uint)colorReferencesArray.Length,
                pColorAttachments: pColorReferences,
                // No resolve attachments, otherwise we'd have a corresponding resolve attachment for each color attachment
                pResolveAttachments: null,
                pDepthStencilAttachment: &depthStencilReference,
                preserveAttachmentCount: 0,
                pPreserveAttachments: null
            );
            
            fixed (AttachmentDescription* pAttachmentDescriptions = attachmentDescriptionsArray) {
                RenderPassCreateInfo createInfo = new(
                    StructureType.RenderPassCreateInfo,
                    pNext: null,
                    flags: RenderPassCreateFlags.None,
                    attachmentCount: (uint)attachmentDescriptionsArray.Length,
                    pAttachments: pAttachmentDescriptions,
                    subpassCount: 1,
                    pSubpasses: &subpassDescription,
                    dependencyCount: 0,
                    pDependencies: null
                );

                renderer.Vk.CreateRenderPass(renderer.Device, in createInfo, null, out RenderPass)
                    .AssertSuccess("Failed to create renderpass");
            }
        }

    }

    private static AttachmentLoadOp TranslateLoadAction(LoadAction action)
    {
        return action switch {
            LoadAction.Load => AttachmentLoadOp.Load,
            LoadAction.Clear => AttachmentLoadOp.Clear,
            LoadAction.DontCare => AttachmentLoadOp.DontCare,
            _ => throw new ArgumentOutOfRangeException(nameof(action), action, null)
        };
    }

    private unsafe void ReleaseUnmanagedResources()
    {
        // Vulkan RenderPass objects are easy to destroy, no need to defer deletion, as they are not tangled up with
        // command buffer encoding. Yay!
        _renderer.Vk.DestroyRenderPass(_renderer.Device, RenderPass, null);
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