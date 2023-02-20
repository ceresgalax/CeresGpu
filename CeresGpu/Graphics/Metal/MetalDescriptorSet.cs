using System;
using System.Collections.Generic;
using CeresGpu.Graphics.Shaders;
using Metalancer.MetalBinding;

namespace CeresGpu.Graphics.Metal
{
    public sealed class MetalDescriptorSet : IDescriptorSet
    {
        enum DescriptorType
        {
            Unset,
            Buffer,
            Texture,
            Sampler
        }

        private readonly List<(DescriptorType, object)> _descriptors;
        
        public readonly ShaderStage Stage;
        public readonly uint BufferIndex; 
        public readonly IMetalBuffer ArgumentBuffer;
        private readonly MetalRenderer _renderer;
        private readonly uint _argumentBufferSize;
        private IntPtr _argumentEncoder;

        public MetalDescriptorSet(MetalRenderer renderer, IntPtr function, ShaderStage stage, int index, in DescriptorSetCreationHints hints)
        {
            _renderer = renderer;
            _descriptors = new List<(DescriptorType, object)>(hints.DescriptorCount);
            Stage = stage;
            BufferIndex = MetalBufferTableConstants.INDEX_ARGUMENT_BUFFER_0 + (uint)index;
            _argumentEncoder = MetalApi.metalbinding_new_argument_encoder(function, BufferIndex); 
            _argumentBufferSize = MetalApi.metalbinding_get_argument_buffer_size(_argumentEncoder);
            ArgumentBuffer = (IMetalBuffer)renderer.CreateStreamingBuffer<byte>((int)_argumentBufferSize);
        }
        
        private void ReleaseUnmanagedResources()
        {
            if (_argumentEncoder != IntPtr.Zero) {
                MetalApi.metalbinding_release_argument_encoder(_argumentEncoder);
                _argumentEncoder = IntPtr.Zero;
            }
        }

        public void Dispose()
        {
            ReleaseUnmanagedResources();
            GC.SuppressFinalize(this);
        }

        ~MetalDescriptorSet() {
            ReleaseUnmanagedResources();
        }
        
        private void SetDescriptor(int index, DescriptorType descriptorType, object resource)
        {
            while (index >= _descriptors.Count) {
                _descriptors.Add((DescriptorType.Unset, string.Empty));
            }
            _descriptors[index] = (descriptorType, resource);
        }
        
        public void SetUniformBufferDescriptor<T>(IBuffer<T> buffer, in DescriptorInfo info) where T : unmanaged
        {
            if (buffer is not IMetalBuffer metalBuffer) {
                throw new ArgumentException("Incompatible buffer", nameof(buffer));
            }
            
            SetDescriptor(info.BindingIndex, DescriptorType.Buffer, metalBuffer);
        }

        public void SetShaderStorageBufferDescriptor<T>(IBuffer<T> buffer, in DescriptorInfo info) where T : unmanaged
        {
            // SSBOs are encoded into argument buffers the same way UBOs are.
            SetUniformBufferDescriptor(buffer, info);
        }

        public void SetTextureDescriptor(ITexture texture, in DescriptorInfo info)
        {
            if (texture is not MetalTexture metalTexture) {
                throw new ArgumentException("Incompatible texture", nameof(texture));
            }

            SetDescriptor(info.BindingIndex, DescriptorType.Texture, metalTexture);
            MetalSampler sampler = _renderer.Samplers.GetSampler(metalTexture.MinFilter, metalTexture.MagFilter);
            SetDescriptor(info.SamplerIndex, DescriptorType.Sampler, sampler);
        }

        public void UpdateArgumentBuffer(IntPtr renderCommandEncoder)
        {
            uint stages = Stage == ShaderStage.Vertex ? 0b01u : 0b10u;
            
            ArgumentBuffer.PrepareToUpdateExternally();
            MetalApi.metalbinding_set_argument_buffer(_argumentEncoder, ArgumentBuffer.GetHandleForCurrentFrame());
            for (int i = 0, ilen = _descriptors.Count; i < ilen; ++i) {
                (DescriptorType descriptorType, object resource) = _descriptors[i];
                switch (descriptorType) {
                    case DescriptorType.Buffer:
                        IMetalBuffer buffer = (IMetalBuffer)resource;
                        buffer.ThrowIfNotReadyForUse();
                        MetalApi.metalbinding_encode_buffer_argument(_argumentEncoder, renderCommandEncoder, buffer.GetHandleForCurrentFrame(), 0, (uint)i, stages);
                        break;
                    case DescriptorType.Texture:
                        IntPtr handle = ((MetalTexture)resource).Handle;
                        // We _must_ encode a texture argument, I believe the argument buffer has an arbitrary value
                        // if not encoded, which can cause crashes / corruption in Metal when used.
                        if (handle == IntPtr.Zero) {
                            // TODO: Keep an actual fallback texture around instead of throwing? 
                            // I'm scared of a complicated exception 'exploit' where handling this exception leaves the
                            // argument buffer in an incompelte state, then is somehow used again. :/
                            throw new InvalidOperationException("Texture has either not been set or has been disposed.");
                        }
                        MetalApi.metalbinding_encode_texture_argument(_argumentEncoder, renderCommandEncoder, handle, (uint)i, stages);
                        break;
                    case DescriptorType.Sampler:
                        MetalApi.metalbinding_encode_sampler_argument(_argumentEncoder, ((MetalSampler)resource).Handle, (uint)i);
                        break;
                }
            }
            MetalApi.metalbinding_buffer_did_modify_range(ArgumentBuffer.GetHandleForCurrentFrame(), 0, _argumentBufferSize);
            //MetalApi.metalbinding_dump_buffer(ArgumentBuffer.GetHandleForCurrentFrame());
        }

    }
}