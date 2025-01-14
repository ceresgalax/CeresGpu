using System.Collections.Generic;
using CeresGL;

namespace CeresGpu.Graphics.OpenGL.VirtualCommands;

public interface IVirtualCommand
{
    void Execute(GL gl);
}

public class VirtualCommandBuffer
{
    public List<IVirtualCommand> Commands = [];
}