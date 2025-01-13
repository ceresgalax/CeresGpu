using System;
using System.Runtime.InteropServices;
using CeresGL;
using CeresGpu.Graphics.Shaders;

namespace CeresGpu.Graphics.OpenGL
{
    public sealed class GLPass : IPass 
    {
        private readonly GLRenderer _renderer;

        private IGLPipeline? _currentPipeline;
        private object? _previousPipeline;
        private IUntypedShaderInstance? _shaderInstance;
        private GLShaderInstanceBacking? _shaderInstanceBacking;

        private readonly uint _attachmentWidth, _attachmentHeight;
        
        public ScissorRect CurrentDynamicScissor { get; }
        public Viewport CurrentDynamicViewport { get; }

        public GLPass(GLRenderer renderer, uint attachmentWidth, uint attachmentHeight)
        {
            _renderer = renderer;
            _attachmentWidth = attachmentWidth;
            _attachmentHeight = attachmentHeight;
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

            if (_previousPipeline != pipeline) {
                GL gl = _renderer.GLProvider.Gl;
                glPipe.Setup(gl);
                _previousPipeline = pipeline;
            }

            _currentPipeline = glPipe;
            _shaderInstance = shaderInstance;
            _shaderInstanceBacking = shaderInstanceBacking;
            
            UpdateShaderInstance();
        }

        public void RefreshPipeline()
        {
            UpdateShaderInstance();
        }
        
        private void CheckCurrent()
        {
            throw new NotImplementedException();
            // if (_renderer.CurrentPass != this) {
            //     throw new InvalidOperationException("Pass is no longer current");
            // }
        }

        public void SetScissor(ScissorRect scissor)
        {
            CheckCurrent();
            GL gl = _renderer.GLProvider.Gl;
            // OpenGL scissor coords originate from bottom-left, CeresGPU scissor coords originate from top-left.
            int y = (int)_attachmentHeight - scissor.Y - (int)scissor.Height;
            gl.Scissor(scissor.X, y, (int)scissor.Width, (int)scissor.Height);
        }

        public void SetViewport(Viewport viewport)
        {
            CheckCurrent();
            GL gl = _renderer.GLProvider.Gl;
            // OpenGL viewport coords originate from bottom-left, CeresGPU viewport coords originate from top-left.
            float y = _attachmentHeight - viewport.Y - viewport.Height;
            gl.ViewportIndexedf(0, viewport.X, y, viewport.Width, viewport.Height);
        }

        public void Draw(uint vertexCount, uint instanceCount, uint firstVertex, uint firstInstance)
        {
            CheckCurrent();
            GL gl = _renderer.GLProvider.Gl;
            gl.DrawArraysInstancedBaseInstance(PrimitiveType.TRIANGLES, (int)firstVertex, (int)vertexCount, (int)instanceCount, firstInstance);
        }

        public void DrawIndexedUshort(IBuffer<ushort> indexBuffer, uint indexCount, uint instanceCount, uint firstIndex, int vertexOffset, uint firstInstance)
        {
            CheckCurrent();
            if (indexBuffer is not IGLBuffer glIndexBuffer) {
                throw new ArgumentException("Incompatible buffer", nameof(indexBuffer));
            }
            GL gl = _renderer.GLProvider.Gl;
            glIndexBuffer.Commit();
            gl.BindBuffer(BufferTargetARB.ELEMENT_ARRAY_BUFFER, glIndexBuffer.GetHandleForCurrentFrame());
            uint indexBufferOffset = (uint)Marshal.SizeOf<ushort>() * firstIndex;
            gl.glDrawElementsInstancedBaseVertexBaseInstance((uint)PrimitiveType.TRIANGLES, (int)indexCount, (uint)DrawElementsType.UNSIGNED_SHORT, new IntPtr(indexBufferOffset), (int)instanceCount, vertexOffset, firstInstance);
        }

        public void DrawIndexedUint(IBuffer<uint> indexBuffer, uint indexCount, uint instanceCount, uint firstIndex, int vertexOffset, uint firstInstance)
        {
            CheckCurrent();
            if (indexBuffer is not IGLBuffer glIndexBuffer) {
                throw new ArgumentException("Incompatible buffer", nameof(indexBuffer));
            }
            GL gl = _renderer.GLProvider.Gl;
            glIndexBuffer.Commit();
            gl.BindBuffer(BufferTargetARB.ELEMENT_ARRAY_BUFFER, glIndexBuffer.GetHandleForCurrentFrame());
            uint indexBufferOffset = (uint)Marshal.SizeOf<uint>() * firstIndex;
            gl.glDrawElementsInstancedBaseVertexBaseInstance((uint)PrimitiveType.TRIANGLES, (int)indexCount, (uint)DrawElementsType.UNSIGNED_INT, new IntPtr(indexBufferOffset), (int)instanceCount, vertexOffset, firstInstance);
        }

        public void Finish()
        {
            _renderer.FinishPass();
        }
        
        private void UpdateShaderInstance()
        {
            if (_shaderInstanceBacking == null || _shaderInstance == null || _currentPipeline == null) {
                throw new InvalidOperationException("Must call SetPipeline first!");
            }

            _shaderInstanceBacking.PrepareAndBindVertexArrayObject(_currentPipeline.VertexBufferLayout, _shaderInstance.VertexBufferAdapter);
            _shaderInstanceBacking.UpdateBoundVao();
        }
    }
}