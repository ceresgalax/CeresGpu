
using System;
using System.Collections.Generic;

namespace CeresGpu.Graphics.Metal
{
    public sealed class MetalShaderInstanceBacking : IShaderInstanceBacking
    {
        public readonly List<IMetalBuffer?> VertexBuffers;

        public MetalShaderInstanceBacking(int vertexBufferCountHint)
        {
            VertexBuffers = new List<IMetalBuffer?>(vertexBufferCountHint);
        }

        public void Dispose() { }
        
        public void SetVertexBuffer<T>(IBuffer<T> buffer, int index) where T : unmanaged
        {
            if (buffer is not IMetalBuffer metalBuffer) {
                throw new ArgumentException("Incompatible buffer", nameof(buffer));
            }
            while (index >= VertexBuffers.Count) {
                VertexBuffers.Add(null);
            }
            VertexBuffers[index] = metalBuffer;
        }
    }
}