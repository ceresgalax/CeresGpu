using System;
using System.Collections.Generic;
using CeresGpu.Graphics.Shaders;

namespace CeresGpu.Graphics.OpenGL
{
    public sealed class GLShaderInstanceBacking : IShaderInstanceBacking
    {
        private readonly GLRenderer _renderer;
        private readonly IShader _shader;

        //private readonly List<uint> _prevVertexBufferHandles;
        //private readonly List<IGLBuffer?> _currentVertexBuffers;

        //private uint _vao;
        private readonly VertexArray[] _vaos;
        
        public GLShaderInstanceBacking(GLRenderer renderer, IShader shader)
        {
            _renderer = renderer;
            _shader = shader;
            
            IGLProvider provider = renderer.GLProvider;
            
            _vaos = new VertexArray[renderer.WorkingFrameCount];
            for (int i = 0, ilen = _vaos.Length; i < ilen; ++i) {
                _vaos[i] = new VertexArray(provider);
            }
            //_prevVertexBufferHandles = new(vertexBufferCountHint);
            //_currentVertexBuffers = new(vertexBufferCountHint);
        }
        
        // public void SetVertexBuffer<T>(IBuffer<T> buffer, int index) where T : unmanaged
        // {
        //     if (buffer is not IGLBuffer glBuffer) {
        //         throw new ArgumentException("Incompatible buffer", nameof(buffer));
        //     }
        //     
        //     while (index >= _currentVertexBuffers.Count) {
        //         _currentVertexBuffers.Add(null);
        //     }
        //     _currentVertexBuffers[index] = glBuffer;
        // }

        public void PrepareAndBindVertexArrayObject(IVertexBufferLayout layout, IUntypedVertexBufferAdapter adapter)
        {
            VertexArray vao = _vaos[_renderer.WorkingFrame];
            
            // Note: This will throw a cast exception if any of the buffers are not a GLBuffer.
            // Which is correct, mixing buffers meant for different renderer types is bad.
            // TODO: However, maybe we could surface this issue a bit more gracefully?
            foreach (IGLBuffer? buffer in adapter.VertexBuffers) {
                buffer?.Commit();
            }

            vao.RecreateIfNecesaryAndBind(_shader, layout, adapter);
        }

        public void Dispose()
        {
            foreach (VertexArray vao in _vaos) {
                vao.Dispose();
            }
            _shader.Dispose();
        }

    }
}