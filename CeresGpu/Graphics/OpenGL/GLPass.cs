using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using CeresGL;
using CeresGpu.Graphics.OpenGL.VirtualCommands;

namespace CeresGpu.Graphics.OpenGL;

public interface IGLPass
{
    IGLPass? Prev { get; set; }
    IGLPass? Next { get; set; }

    void ExecuteCommands(GL gl);
}

public class GLPassAnchor : IGLPass
{
    public IGLPass? Prev { get; set; }
    public IGLPass? Next { get; set; }
    
    public void ExecuteCommands(GL gl)
    {
        throw new NotSupportedException();
    }

    public void ResetAsFront(GLPassAnchor endAnchor)
    {
        Next = endAnchor;
        endAnchor.Prev = this;
    }
}

/// <summary>
/// State of a pass that can be read & write while executing virtual commands in a pass.
/// </summary>
public class GLPassState
{
}

public sealed class GLPass : PassEncoder, IGLPass 
{
    private readonly GLRenderer _renderer;
   
    private IGLPipeline? _currentPipeline;
    private GLShaderInstanceBacking? _shaderInstanceBacking;

    private readonly uint _attachmentWidth, _attachmentHeight;

    private readonly List<IVirtualCommand> _commands = [];
    
    public IGLPass? Prev { get; set; }
    public IGLPass? Next { get; set; }
    
    public void Remove()
    {
        if (Prev != null || Next != null) {
            if (Prev != null) {
                Prev.Next = Next;
            }
            if (Next != null) {
                Next.Prev = Prev;
            }
        }
    }
    
    public void InsertBefore(IGLPass other)
    {
        Prev = other.Prev;
        other.Prev = this;
        Next = other;
    }

    public void InsertAfter(IGLPass other)
    {
        Next = other.Next;
        other.Next = this;
        Prev = other;
    }

    public GLPass(GLRenderer renderer, GLPassBacking passBacking, GLFramebuffer framebuffer)
    {
        _renderer = renderer;
        framebuffer.GetSize(out _attachmentWidth, out _attachmentHeight);
        _commands.Add(new BeginPassCommand(passBacking, framebuffer));
    }
        
    protected override void SetPipelineImpl<TShader, TVertexBufferLayout>(
        IPipeline<TShader, TVertexBufferLayout> pipeline,
        IShaderInstance<TShader, TVertexBufferLayout> shaderInstance
    ) 
    {
        if (pipeline is not IGLPipeline glPipe) {
            throw new ArgumentException("Incompatible pipeline", nameof(pipeline));
        }
        if (shaderInstance.Backing is not GLShaderInstanceBacking shaderInstanceBacking) {
            throw new ArgumentException("Incompatible shader instance", nameof(shaderInstance));
        }

        var command = new SetPipelineCommand();
        command.Setup(glPipe);
        _commands.Add(command);
        
        _currentPipeline = glPipe;
        _shaderInstanceBacking = shaderInstanceBacking;
            
        UpdateShaderInstance();
    }

    protected override void RefreshPipelineImpl()
    {
        UpdateShaderInstance();
    }
        
    protected override void SetScissorImpl(ScissorRect scissor)
    {
        _commands.Add(new SetScissorCommand(scissor, _attachmentHeight));
    }

    protected override void SetViewportImpl(Viewport viewport)
    {
        _commands.Add(new SetViewportCommand(viewport, _attachmentHeight));
    }

    protected override void DrawImpl(uint vertexCount, uint instanceCount, uint firstVertex, uint firstInstance)
    {
        _commands.Add(new DrawCommand((int) firstVertex, (int)vertexCount, (int)instanceCount, firstInstance));
    }

    protected override void DrawIndexedUshortImpl(IBuffer<ushort> indexBuffer, uint indexCount, uint instanceCount, uint firstIndex, int vertexOffset, uint firstInstance)
    {
        if (indexBuffer is not IGLBuffer glIndexBuffer) {
            throw new ArgumentException("Incompatible buffer", nameof(indexBuffer));
        }
        uint indexBufferOffset = (uint)Marshal.SizeOf<ushort>() * firstIndex;
        _commands.Add(new DrawIndexedCommand(glIndexBuffer, new IntPtr(indexBufferOffset), (int)indexCount, (int)instanceCount, vertexOffset, firstInstance));
    }

    protected override void DrawIndexedUintImpl(IBuffer<uint> indexBuffer, uint indexCount, uint instanceCount, uint firstIndex, int vertexOffset, uint firstInstance)
    {
        if (indexBuffer is not IGLBuffer glIndexBuffer) {
            throw new ArgumentException("Incompatible buffer", nameof(indexBuffer));
        }
        uint indexBufferOffset = (uint)Marshal.SizeOf<uint>() * firstIndex;
        _commands.Add(new DrawIndexedCommand(glIndexBuffer, new IntPtr(indexBufferOffset), (int)indexCount, (int)instanceCount, vertexOffset, firstInstance));
    }

    private void UpdateShaderInstance()
    {
        if (_shaderInstanceBacking == null || CurrentShaderInstance == null || _currentPipeline == null) {
            throw new InvalidOperationException("Must call SetPipeline first!");
        }
        
        _commands.Add(new UpdateShaderInstanceCommand(_currentPipeline, _shaderInstanceBacking, CurrentShaderInstance));
    }

    public void ExecuteCommands(GL gl)
    {
        foreach (IVirtualCommand command in _commands) {
            command.Execute(gl);
        }
    }
    
}