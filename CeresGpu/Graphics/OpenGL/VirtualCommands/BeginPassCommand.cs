using System;
using System.Numerics;
using CeresGL;

namespace CeresGpu.Graphics.OpenGL.VirtualCommands;

public class BeginPassCommand(GLPassBacking passBacking, GLFramebuffer framebuffer) : IVirtualCommand
{
    public void Execute(GL gl)
    {
        framebuffer.GetSize(out uint width, out uint height);
        gl.Viewport(0, 0, (int)width, (int)height);
        gl.Scissor(0, 0, (int)width, (int)height);
        
        gl.BindFramebuffer(FramebufferTarget.DRAW_FRAMEBUFFER, framebuffer.FramebufferHandle);

        for (int i = 0; i < framebuffer.ColorAttachments.Length; ++i) {
            if (passBacking.Definition.ColorAttachments[i].LoadAction == LoadAction.Clear) {
                gl.DrawBuffer(DrawBufferMode.COLOR_ATTACHMENT0 + (uint)i);
                Vector4 clearColor = framebuffer.ColorAttachments[i].ClearColor;
                gl.ClearColor(clearColor.X, clearColor.Y, clearColor.Z, clearColor.W);
                gl.Clear(ClearBufferMask.COLOR_BUFFER_BIT);
            }
        }

        gl.DrawBuffer(DrawBufferMode.BACK);
        
        if (framebuffer.DepthStencilAttachment != null) {
            if (passBacking.Definition.DepthStencilAttachment?.LoadAction == LoadAction.Clear) {
                gl.ClearDepth(framebuffer.DepthClearValue);
                gl.ClearStencil((int)framebuffer.StencilClearValue);
            }
        }
    }
}