using CeresGL;
using CeresGpu.Graphics.Shaders;

namespace CeresGpu.Graphics.OpenGL.VirtualCommands;

public class UpdateShaderInstanceCommand(GLShaderInstanceBacking shaderInstanceBacking) : IVirtualCommand
{
    public void Execute(GL gl)
    {
        shaderInstanceBacking.UpdateBoundVao();
    }
}