using CeresGL;
using CeresGpu.Graphics.Shaders;

namespace CeresGpu.Graphics.OpenGL.VirtualCommands;

public class SetPipelineCommand : IVirtualCommand
{
    private IGLPipeline? _pipeline;
    // private GLShaderInstanceBacking? _shaderInstanceBacking;
    // private IUntypedShaderInstance? _shaderInstance;

    public void Setup(IGLPipeline pipeline /* GLShaderInstanceBacking shaderInstanceBacking, IUntypedShaderInstance shaderInstance */)
    {
        _pipeline = pipeline;
        // _shaderInstance = shaderInstance;
        // _shaderInstanceBacking = shaderInstanceBacking;
    }

    public void Execute(GL gl)
    {
        //if (state.PreviousPipeline != _pipeline) {
            _pipeline!.Setup(gl);
            //state.PreviousPipeline = _pipeline;
        //}

        // state.CurrentPipeline = _pipeline;
        // state.ShaderInstanceBacking = _shaderInstanceBacking;
        // state.ShaderInstance = _shaderInstance;
    }
}