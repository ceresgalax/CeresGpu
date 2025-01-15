using System;
using CeresGL;

namespace CeresGpu.Graphics.OpenGL.VirtualCommands;

public class SetScissorCommand(ScissorRect scissor, uint _attachmentHeight) : IVirtualCommand
{
    public void Execute(GL gl)
    {
        // OpenGL scissor coords originate from bottom-left, CeresGPU scissor coords originate from top-left.
        int y = (int)_attachmentHeight - scissor.Y - (int)scissor.Height;
        gl.Scissor(scissor.X, y, (int)scissor.Width, (int)scissor.Height);
    }
}

public class SetViewportCommand(Viewport viewport, uint _attachmentHeight) : IVirtualCommand
{
    public void Execute(GL gl)
    {
        // OpenGL viewport coords originate from bottom-left, CeresGPU viewport coords originate from top-left.
        float y = _attachmentHeight - viewport.Y - viewport.Height;
        gl.Viewport((int)MathF.Round(viewport.X), (int)MathF.Round(y), (int)MathF.Round(viewport.Width), (int)MathF.Round(viewport.Height)); 
    }
}