using CeresGL;
using CeresGpu.Graphics.Shaders;

namespace CeresGpu.Graphics.OpenGL.VirtualCommands;

public class UpdateShaderInstanceCommand(IGLPipeline pipeline, GLShaderInstanceBacking shaderInstanceBacking, IUntypedShaderInstance shaderInstance) : IVirtualCommand
{
    public void Execute(GL gl)
    {
        shaderInstanceBacking.PrepareAndBindVertexArrayObject(pipeline.VertexBufferLayout, shaderInstance.VertexBufferAdapter);
        shaderInstanceBacking.UpdateBoundVao();
    }
}