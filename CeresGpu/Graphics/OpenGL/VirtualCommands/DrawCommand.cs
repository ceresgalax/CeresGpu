using System;
using CeresGL;

namespace CeresGpu.Graphics.OpenGL.VirtualCommands;

public class DrawCommand(int firstVertex, int vertexCount, int instanceCount, uint firstInstance) : IVirtualCommand
{
    public void Execute(GL gl)
    {
        gl.DrawArraysInstancedBaseInstance(PrimitiveType.TRIANGLES, firstVertex, vertexCount, instanceCount, firstInstance);
    }
}

public class DrawIndexedCommand(IGLBuffer indexBuffer, IntPtr indexBufferByteOffset, int indexCount, int instanceCount, int vertexOffset, uint firstInstance) : IVirtualCommand
{
    public void Execute(GL gl)
    {
        gl.BindBuffer(BufferTargetARB.ELEMENT_ARRAY_BUFFER, indexBuffer.GetHandleForCurrentFrame());
        gl.glDrawElementsInstancedBaseVertexBaseInstance((uint)PrimitiveType.TRIANGLES, indexCount, (uint)DrawElementsType.UNSIGNED_SHORT, indexBufferByteOffset, instanceCount, vertexOffset, firstInstance);
    }
}