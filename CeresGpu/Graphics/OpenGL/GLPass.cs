using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using CeresGL;
using CeresGpu.Graphics.OpenGL.VirtualCommands;
using CeresGpu.Graphics.Shaders;

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

public sealed class GLPass : IGLPass, IPass 
{
    private readonly GLRenderer _renderer;
   
    private IGLPipeline? _currentPipeline;
    private object? _previousPipeline;
    private IUntypedShaderInstance? _shaderInstance;
    private GLShaderInstanceBacking? _shaderInstanceBacking;

    private readonly uint _attachmentWidth, _attachmentHeight;
        
    public ScissorRect CurrentDynamicScissor { get; }
    public Viewport CurrentDynamicViewport { get; }

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
        _attachmentWidth = framebuffer.Width;
        _attachmentHeight = framebuffer.Height;
        
        _commands.Add(new BeginPassCommand(passBacking, framebuffer));
        _commands.Add(new SetScissorCommand(new ScissorRect(0, 0, framebuffer.Width, framebuffer.Height), _attachmentHeight));
    }
        
    public void SetPipeline<TShader, TVertexBufferLayout>(
        IPipeline<TShader, TVertexBufferLayout> pipeline,
        IShaderInstance<TShader, TVertexBufferLayout> shaderInstance
    ) 
        where TShader : IShader
        where TVertexBufferLayout : IVertexBufferLayout<TShader>
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
        _shaderInstance = shaderInstance;
        _shaderInstanceBacking = shaderInstanceBacking;
            
        UpdateShaderInstance();
    }

    public void RefreshPipeline()
    {
        UpdateShaderInstance();
    }
        
    public void SetScissor(ScissorRect scissor)
    {
        _commands.Add(new SetScissorCommand(scissor, _attachmentHeight));
    }

    public void SetViewport(Viewport viewport)
    {
        _commands.Add(new SetViewportCommand(viewport, _attachmentHeight));
    }

    public void Draw(uint vertexCount, uint instanceCount, uint firstVertex, uint firstInstance)
    {
        _commands.Add(new DrawCommand((int) firstVertex, (int)vertexCount, (int)instanceCount, firstInstance));
    }

    public void DrawIndexedUshort(IBuffer<ushort> indexBuffer, uint indexCount, uint instanceCount, uint firstIndex, int vertexOffset, uint firstInstance)
    {
        if (indexBuffer is not IGLBuffer glIndexBuffer) {
            throw new ArgumentException("Incompatible buffer", nameof(indexBuffer));
        }
        glIndexBuffer.Commit();
        uint indexBufferOffset = (uint)Marshal.SizeOf<ushort>() * firstIndex;
        _commands.Add(new DrawIndexedCommand(glIndexBuffer, new IntPtr(indexBufferOffset), (int)indexCount, (int)instanceCount, vertexOffset, firstInstance));
    }

    public void DrawIndexedUint(IBuffer<uint> indexBuffer, uint indexCount, uint instanceCount, uint firstIndex, int vertexOffset, uint firstInstance)
    {
        if (indexBuffer is not IGLBuffer glIndexBuffer) {
            throw new ArgumentException("Incompatible buffer", nameof(indexBuffer));
        }
        glIndexBuffer.Commit();
        uint indexBufferOffset = (uint)Marshal.SizeOf<uint>() * firstIndex;
        _commands.Add(new DrawIndexedCommand(glIndexBuffer, new IntPtr(indexBufferOffset), (int)indexCount, (int)instanceCount, vertexOffset, firstInstance));
    }

    public void Finish()
    {
    }

    private void UpdateShaderInstance()
    {
        if (_shaderInstanceBacking == null || _shaderInstance == null || _currentPipeline == null) {
            throw new InvalidOperationException("Must call SetPipeline first!");
        }
        
        _commands.Add(new UpdateShaderInstanceCommand(_currentPipeline, _shaderInstanceBacking, _shaderInstance));
    }

    public void ExecuteCommands(GL gl)
    {
        foreach (IVirtualCommand command in _commands) {
            command.Execute(gl);
        }
    }
    
}