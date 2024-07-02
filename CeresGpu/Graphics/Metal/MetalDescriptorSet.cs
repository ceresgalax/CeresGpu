using System;
using System.Collections.Generic;
using CeresGpu.Graphics.Shaders;
using CeresGpu.MetalBinding;

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

        private readonly List<(DescriptorType, object, uint extraIndex)> _descriptors;
        
        public readonly ShaderStage Stage;
        public readonly uint BufferIndex; 
        public readonly IMetalBuffer ArgumentBuffer;
        private readonly MetalRenderer _renderer;
        private readonly uint _argumentBufferSize;
        private IntPtr _argumentEncoder;

        public MetalDescriptorSet(MetalRenderer renderer, IntPtr function, ShaderStage stage, int index, in DescriptorSetCreationHints hints)
        {
            _renderer = renderer;
            _descriptors = new List<(DescriptorType, object, uint)>(hints.DescriptorCount);
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
        
        private void SetDescriptor(int index, DescriptorType descriptorType, object resource, uint extraIndex = 0)
        {
            while (index >= _descriptors.Count) {
                _descriptors.Add((DescriptorType.Unset, string.Empty, 0));
            }
            _descriptors[index] = (descriptorType, resource, extraIndex);
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

            SetDescriptor(info.BindingIndex, DescriptorType.Texture, metalTexture, (uint)info.SamplerIndex);
        }

        public void SetSamplerDescriptor(ISampler sampler, in DescriptorInfo info)
        {
            if (sampler is not MetalSampler metalSampler) {
                throw new ArgumentException("Incompatible sampler", nameof(sampler));
            }
            
            SetDescriptor(info.SamplerIndex, DescriptorType.Sampler, metalSampler, (uint)info.BindingIndex);
        }

        private readonly HashSet<int> _texturesWithSetSamplers = new();
        
        public void UpdateArgumentBuffer(IntPtr renderCommandEncoder)
        {
            _texturesWithSetSamplers.Clear();
            for (int i = 0, ilen = _descriptors.Count; i < ilen; ++i) {
                (DescriptorType descriptorType, object resource, uint extraIndex) = _descriptors[i];
                if (descriptorType == DescriptorType.Sampler) {
                    _texturesWithSetSamplers.Add((int)extraIndex);
                }
            }
            
            uint stages = Stage == ShaderStage.Vertex ? 0b01u : 0b10u;
            
            // TODO: NEED TO KNOW THE DESCRIPTOR SET LAYOUT SO THAT WE CAN ALWAYS SET EVERYTHING.
            // ITS REALLY BAD TO SKIP ENCODING SPOTS IN THE ARGUMENT BUFFER.
            
            ArgumentBuffer.PrepareToUpdateExternally();
            MetalApi.metalbinding_set_argument_buffer(_argumentEncoder, ArgumentBuffer.GetHandleForCurrentFrame());
            for (int i = 0, ilen = _descriptors.Count; i < ilen; ++i) {
                (DescriptorType descriptorType, object resource, uint extraIndex) = _descriptors[i];
                switch (descriptorType) {
                    case DescriptorType.Buffer:
                        IMetalBuffer buffer = (IMetalBuffer)resource;
                        buffer.Commit();
                        MetalApi.metalbinding_encode_buffer_argument(_argumentEncoder, renderCommandEncoder, buffer.GetHandleForCurrentFrame(), 0, (uint)i, stages);
                        break;
                    case DescriptorType.Texture:
                        IntPtr handle = ((MetalTexture)resource).Handle;
                        // We _must_ encode a texture argument, I believe the argument buffer has an arbitrary value
                        // if not encoded, which can cause crashes / corruption in Metal when used.
                        // But even more importantly, we need CeresGPU to behave consistently across all graphics APIs.
                        
                        if (handle == IntPtr.Zero || !_texturesWithSetSamplers.Contains(i)) {
                            MetalApi.metalbinding_encode_texture_argument(_argumentEncoder, renderCommandEncoder, _renderer.FallbackTexture.Handle, (uint)i, stages);
                            MetalApi.metalbinding_encode_sampler_argument(_argumentEncoder, _renderer.FallbackSampler.Handle, (uint)extraIndex);
                        } else {
                            MetalApi.metalbinding_encode_texture_argument(_argumentEncoder, renderCommandEncoder, handle, (uint)i, stages);    
                        }
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