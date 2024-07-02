using System;
using System.Numerics;
using System.Runtime.InteropServices;
using CeresGL;
using CeresGpu.Graphics.Shaders;

namespace CeresGpu.Graphics.OpenGL
{
    public sealed class GLPass : IPass
    {
        private GLRenderer _renderer;

        private object? _previousPipeline;
        private IUntypedShaderInstance? _shaderInstance;
        private GLShaderInstanceBacking? _shaderInstanceBacking;
        private bool _instanceUpdated;

        private readonly uint _attachmentWidth, _attachmentHeight;
        
        public ScissorRect CurrentDynamicScissor { get; }
        public Viewport CurrentDynamicViewport { get; }

        public GLPass(GLRenderer renderer, uint attachmentWidth, uint attachmentHeight)
        {
            _renderer = renderer;
            _attachmentWidth = attachmentWidth;
            _attachmentHeight = attachmentHeight;
        }
        
        public void SetPipeline<ShaderT>(IPipeline<ShaderT> pipeline, IShaderInstance<ShaderT> shaderInstance) where ShaderT : IShader
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
            
            _shaderInstance = shaderInstance;
            _shaderInstanceBacking = shaderInstanceBacking;
            _instanceUpdated = false;
        }
        
        private void CheckCurrent()
        {
            if (_renderer.CurrentPass != this) {
                throw new InvalidOperationException("Pass is no longer current");
            }
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
            uint y = _attachmentHeight - viewport.Y - viewport.Height;
            gl.Viewport((int)viewport.X, (int)y, (int)viewport.Width, (int)viewport.Height);
        }

        public void Draw(uint vertexCount, uint instanceCount, uint firstVertex, uint firstInstance)
        {
            CheckCurrent();
            UpdateShaderInstance();
            GL gl = _renderer.GLProvider.Gl;
            gl.DrawArraysInstancedBaseInstance(PrimitiveType.TRIANGLES, (int)firstVertex, (int)vertexCount, (int)instanceCount, firstInstance);
        }

        public void DrawIndexedUshort(IBuffer<ushort> indexBuffer, uint indexCount, uint instanceCount, uint firstIndex, uint vertexOffset, uint firstInstance)
        {
            CheckCurrent();
            UpdateShaderInstance();
            if (indexBuffer is not IGLBuffer glIndexBuffer) {
                throw new ArgumentException("Incompatible buffer", nameof(indexBuffer));
            }
            GL gl = _renderer.GLProvider.Gl;
            glIndexBuffer.Commit();
            gl.BindBuffer(BufferTargetARB.ELEMENT_ARRAY_BUFFER, glIndexBuffer.GetHandleForCurrentFrame());
            uint indexBufferOffset = (uint)Marshal.SizeOf<ushort>() * firstIndex;
            gl.glDrawElementsInstancedBaseVertexBaseInstance((uint)PrimitiveType.TRIANGLES, (int)indexCount, (uint)DrawElementsType.UNSIGNED_SHORT, new IntPtr(indexBufferOffset), (int)instanceCount, (int)vertexOffset, firstInstance);
        }

        public void DrawIndexedUint(IBuffer<uint> indexBuffer, uint indexCount, uint instanceCount, uint firstIndex, uint vertexOffset, uint firstInstance)
        {
            CheckCurrent();
            UpdateShaderInstance();
            if (indexBuffer is not IGLBuffer glIndexBuffer) {
                throw new ArgumentException("Incompatible buffer", nameof(indexBuffer));
            }
            GL gl = _renderer.GLProvider.Gl;
            glIndexBuffer.Commit();
            gl.BindBuffer(BufferTargetARB.ELEMENT_ARRAY_BUFFER, glIndexBuffer.GetHandleForCurrentFrame());
            uint indexBufferOffset = (uint)Marshal.SizeOf<uint>() * firstIndex;
            gl.glDrawElementsInstancedBaseVertexBaseInstance((uint)PrimitiveType.TRIANGLES, (int)indexCount, (uint)DrawElementsType.UNSIGNED_INT, new IntPtr(indexBufferOffset), (int)instanceCount, (int)vertexOffset, firstInstance);
        }

        // public void Clear(Viewport rect, Vector4 color)
        // {
        //     CheckCurrent();
        //     GL gl = _renderer.GLProvider.Gl;
        //     gl.Viewport((int)rect.X, (int)rect.Y, (int)rect.Width, (int)rect.Height);
        //     gl.ClearColor(color.X, color.Y, color.Z, color.W);
        //     gl.Clear(ClearBufferMask.COLOR_BUFFER_BIT);
        // }

        public void Finish()
        {
            _renderer.FinishPass();
        }
        
        private void UpdateShaderInstance()
        {
            if (_instanceUpdated) {
                return;
            }
            
            if (_shaderInstanceBacking == null) {
                throw new InvalidOperationException("No shader instance set. Must call SetPipeline first!");
            }

            _shaderInstanceBacking.PrepareAndBindVertexArrayObject();

            if (_shaderInstance != null) {
                foreach (IDescriptorSet set in _shaderInstance.GetDescriptorSets()) {
                    GLDescriptorSet glSet = (GLDescriptorSet)set;
                    glSet.Apply();
                }
            }

            _instanceUpdated = true;
        }

        public void Dispose()
        {
        }
    }
}