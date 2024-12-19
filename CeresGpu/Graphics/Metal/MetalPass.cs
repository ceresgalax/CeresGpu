using System;
using System.Diagnostics;
using System.Numerics;
using System.Runtime.InteropServices;
using CeresGpu.Graphics.Shaders;
using CeresGpu.MetalBinding;

namespace CeresGpu.Graphics.Metal
{
    public sealed class MetalPass : IPass
    {
        private readonly MetalRenderer _renderer;
        private IntPtr _encoder;
        
        public ScissorRect CurrentDynamicScissor { get; private set;  }
        public Viewport CurrentDynamicViewport { get; private set; }

        private IUntypedShaderInstance? _shaderInstance;
        private MetalShaderInstanceBacking? _shaderInstanceBacking;
        private bool _instanceUpdated;

        public MetalPass(MetalRenderer renderer, IntPtr commandBuffer, IntPtr passDescriptor)
        {
            Debug.Assert(commandBuffer != IntPtr.Zero);
            Debug.Assert(passDescriptor != IntPtr.Zero);
            
            _renderer = renderer;
            _encoder = MetalApi.metalbinding_new_command_encoder(commandBuffer, passDescriptor);
        }
        
        private void ReleaseUnmanagedResources()
        {
            if (_encoder != IntPtr.Zero) {
                MetalApi.metalbinding_release_command_encoder(_encoder);
                _encoder = IntPtr.Zero;
            }
        }

        public void Dispose()
        {
            if (_renderer.CurrentPass == this) {
                Finish();
            }
            
            ReleaseUnmanagedResources();
            GC.SuppressFinalize(this);
        }

        ~MetalPass() {
            ReleaseUnmanagedResources();
        }

        private void CheckCurrent()
        {
            if (_renderer.CurrentPass != this) {
                throw new InvalidOperationException("Pass is no longer current");
            }
        }

        private object? _previousPipeline;

        public void SetPipeline<TShader, TVertexBufferLayout>(
            IPipeline<TShader, TVertexBufferLayout> pipeline,
            IShaderInstance<TShader, TVertexBufferLayout> shaderInstance
        )
            where TShader : IShader
            where TVertexBufferLayout : IVertexBufferLayout<TShader> 
        {
            CheckCurrent();
            
            if (pipeline is not MetalPipeline<TShader, TVertexBufferLayout> metalPipeline) {
                throw new ArgumentException("Incompatible pipeline", nameof(pipeline));
            }
            if (shaderInstance.Backing is not MetalShaderInstanceBacking shaderInstanceBacking) {
                throw new ArgumentException("Incompatible shader instance", nameof(shaderInstance));
            }
            
            if (_previousPipeline != pipeline) {
                MetalApi.metalbinding_command_encoder_set_pipeline(_encoder, metalPipeline.Handle);
                _previousPipeline = pipeline;
            }
            
            foreach (IDescriptorSet set in shaderInstance.GetDescriptorSets()) {
                MetalDescriptorSet metalSet = (MetalDescriptorSet)set;
                
                metalSet.ArgumentBuffer.PrepareToUpdateExternally();

                switch (metalSet.Stage) {
                    case ShaderStage.Vertex:
                        MetalApi.metalbinding_command_encoder_set_vertex_buffer(_encoder, metalSet.ArgumentBuffer.GetHandleForCurrentFrame(), 0, metalSet.BufferIndex);
                        break;
                    case ShaderStage.Fragment:
                        MetalApi.metalbinding_command_encoder_set_fragment_buffer(_encoder, metalSet.ArgumentBuffer.GetHandleForCurrentFrame(), 0, metalSet.BufferIndex);
                        break;
                }
            }

            _shaderInstance = shaderInstance;
            _shaderInstanceBacking = shaderInstanceBacking;
            _instanceUpdated = false;

            MetalApi.MTLCullMode cullMode = metalPipeline.CullMode switch {
                CullMode.None => MetalApi.MTLCullMode.None
                , CullMode.Front => MetalApi.MTLCullMode.Front
                , CullMode.Back => MetalApi.MTLCullMode.Back
                , _ => throw new ArgumentOutOfRangeException()
            };

            MetalApi.metalbinding_command_encoder_set_cull_mode(_encoder, cullMode);
            
            MetalApi.metalbinding_command_encoder_set_dss(_encoder, metalPipeline.DepthStencilState);
        }

        public void SetScissor(ScissorRect scissor)
        {
            CheckCurrent();
            MetalApi.metalbinding_command_encoder_set_scissor(_encoder, scissor.X, scissor.Y, scissor.Width, scissor.Height);
            CurrentDynamicScissor = scissor;
        }

        public void SetViewport(Viewport viewport)
        {
            CheckCurrent();
            MetalApi.metalbinding_command_encoder_set_viewport(_encoder, viewport.X, viewport.Y, viewport.Width, viewport.Height);
            CurrentDynamicViewport = viewport;
        }

        public void Draw(uint vertexCount, uint instanceCount, uint firstVertex, uint firstInstance)
        {
            CheckCurrent();
            UpdateShaderInstance();
            MetalApi.metalbinding_command_encoder_draw(_encoder, vertexCount, instanceCount, firstVertex, firstInstance);
        }

        public void DrawIndexedUshort(IBuffer<ushort> indexBuffer, uint indexCount, uint instanceCount, uint firstIndex, uint vertexOffset, uint firstInstance)
        {
            CheckCurrent();
            UpdateShaderInstance();
            if (indexBuffer is not IMetalBuffer metalBuffer) {
                throw new ArgumentException("Incompatible buffer", nameof(indexBuffer));
            }

            if (indexCount == 0) {
                return;
            }

            uint indexBufferOffset = (uint)Marshal.SizeOf<ushort>() * firstIndex;
            
            MetalApi.metalbinding_command_encoder_draw_indexed(_encoder, MetalApi.MTLIndexType.UInt16, 
                metalBuffer.GetHandleForCurrentFrame(), indexCount, instanceCount, indexBufferOffset, vertexOffset, firstInstance);
        }

        public void DrawIndexedUint(IBuffer<uint> indexBuffer, uint indexCount, uint instanceCount, uint firstIndex, uint vertexOffset, uint firstInstance)
        {
            CheckCurrent();
            UpdateShaderInstance();
            if (indexBuffer is not IMetalBuffer metalBuffer) {
                throw new ArgumentException("Incompatible buffer", nameof(indexBuffer));
            }
            
            uint indexBufferOffset = (uint)Marshal.SizeOf<uint>() * firstIndex;
            
            MetalApi.metalbinding_command_encoder_draw_indexed(_encoder, MetalApi.MTLIndexType.UInt32, 
                metalBuffer.GetHandleForCurrentFrame(), indexCount, instanceCount, indexBufferOffset, vertexOffset, firstInstance);
        }
        
        public void Clear(Viewport rect, Vector4 color)
        {
            _renderer.ClearRenderer.Clear(this, rect, color);
        }

        private void UpdateShaderInstance()
        {
            if (_instanceUpdated) {
                return;
            }
            
            if (_shaderInstanceBacking == null || _shaderInstance == null) {
                throw new InvalidOperationException("No shader instance set. Must call SetPipeline first!");
            }

            ReadOnlySpan<object> vertexBuffers = _shaderInstance.VertexBufferAdapter.VertexBuffers;
            
            for (int i = 0, ilen = vertexBuffers.Length; i < ilen; ++i) {
                if (vertexBuffers[i] is IMetalBuffer buffer) {
                    buffer.Commit();
                    MetalApi.metalbinding_command_encoder_set_vertex_buffer(_encoder, buffer.GetHandleForCurrentFrame(), 0, MetalBufferTableConstants.INDEX_VERTEX_BUFFER_MAX - (uint)i);    
                } else {
                    throw new InvalidOperationException($"Buffer returned by vertex buffer adapter at index {i} is not compatible with MetalPass.");
                }
            }
            
            foreach (IDescriptorSet set in _shaderInstance.GetDescriptorSets()) {
                MetalDescriptorSet metalSet = (MetalDescriptorSet)set;
                metalSet.UpdateArgumentBuffer(_encoder);
            }
            

            _instanceUpdated = true;
        }
        
        public void Finish()
        {
            CheckCurrent();
            MetalApi.metalbinding_command_encoder_end_encoding(_encoder);
            _renderer.FinishPass();
        }

    }
}