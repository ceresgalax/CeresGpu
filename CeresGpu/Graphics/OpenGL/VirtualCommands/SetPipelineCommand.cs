using CeresGL;

namespace CeresGpu.Graphics.OpenGL.VirtualCommands;

public class SetPipelineCommand : IVirtualCommand
{
    private IGLPipeline? _pipeline;
    private GLShaderInstanceBacking? _shaderInstanceBacking;
    private IUntypedVertexBufferAdapter? _vertexBufferAdapter;

    public void Setup(IGLPipeline pipeline, GLShaderInstanceBacking shaderInstanceBacking, IUntypedVertexBufferAdapter vertexBufferAdapter)
    {
        _pipeline = pipeline;
        _shaderInstanceBacking = shaderInstanceBacking;
        _vertexBufferAdapter = vertexBufferAdapter;
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
        
        _shaderInstanceBacking!.PrepareAndBindVertexArrayObject(_pipeline.VertexBufferLayout, _vertexBufferAdapter!);
    }
}