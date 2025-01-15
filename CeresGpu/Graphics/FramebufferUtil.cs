using System;

namespace CeresGpu.Graphics;

static class FramebufferUtil
{
    public static void ValidateAttachments(in RenderPassDefinition passDefinition, ReadOnlySpan<IRenderTarget> colorAttachments, IRenderTarget? depthStencilAttachment, out uint width, out uint height, out bool matchesSwapchainSize)
    {
        if (colorAttachments.Length != passDefinition.ColorAttachments.Length) {
            throw new ArgumentOutOfRangeException(nameof(colorAttachments));
        }

        if (passDefinition.DepthStencilAttachment.HasValue != (depthStencilAttachment != null)) {
            throw new ArgumentOutOfRangeException(nameof(depthStencilAttachment));
        }

        uint currentWidth = 0;
        uint currentHeight = 0;

        bool hasCommittedOnFixedSize = false;
        bool isMatchingSwapchainSize = false;

        void UpdateSize(IRenderTarget target)
        {
            if (hasCommittedOnFixedSize && isMatchingSwapchainSize != target.MatchesSwapchainSize) {
                throw new ArgumentOutOfRangeException();
            }

            hasCommittedOnFixedSize = true;
            isMatchingSwapchainSize = target.MatchesSwapchainSize;

            uint targetWidth = target.Width;
            uint targetHeight = target.Height;
            
            if (currentWidth == 0) {
                if (targetWidth == 0 || targetHeight == 0) {
                    throw new ArgumentOutOfRangeException();
                }
                currentWidth = targetWidth;
                currentHeight = targetHeight;
                
            } else if (currentWidth != targetWidth || currentHeight != targetHeight) {
                throw new ArgumentOutOfRangeException(nameof(colorAttachments));
            }
        }
        
        foreach (IRenderTarget colorTarget in colorAttachments) {
            UpdateSize(colorTarget);
        }

        if (depthStencilAttachment != null) {
            UpdateSize(depthStencilAttachment);
        }

        width = currentWidth;
        height = currentHeight;
        matchesSwapchainSize = isMatchingSwapchainSize;
    }
    
}