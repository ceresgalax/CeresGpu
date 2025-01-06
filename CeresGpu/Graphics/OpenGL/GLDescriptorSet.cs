using System;
using System.Collections.Generic;
using CeresGL;
using CeresGpu.Graphics.Shaders;

namespace CeresGpu.Graphics.OpenGL
{
    public class GLDescriptorSet : IDescriptorSet
    {
        enum DescriptorType
        {
            Unset,
            UniformBuffer,
            ShaderStorageBuffer,
            Texture
        }

        private readonly List<(DescriptorType, object, GLSampler? sampler)> _descriptors;
        
        private readonly GLRenderer _renderer;

        public GLDescriptorSet(GLRenderer renderer, in DescriptorSetCreationHints hints)
        {
            _renderer = renderer;
            _descriptors = new List<(DescriptorType, object, GLSampler? sampler)>(hints.DescriptorCount);
        }
        
        private void SetDescriptor(uint index, DescriptorType descriptorType, object resource, GLSampler? sampler = null)
        {
            while (index >= _descriptors.Count) {
                _descriptors.Add((DescriptorType.Unset, string.Empty, null));
            }
            _descriptors[(int)index] = (descriptorType, resource, sampler);
        }
        
        public void SetUniformBufferDescriptor<T>(IBuffer<T> buffer, in DescriptorInfo info) where T : unmanaged
        {
            if (buffer is not IGLBuffer glBuffer) {
                throw new ArgumentException("Incompatible buffer", nameof(buffer));
            }

            GLDescriptorBindingInfo binding = (GLDescriptorBindingInfo)info.Binding;
            SetDescriptor(binding.BindingIndex, DescriptorType.UniformBuffer, glBuffer);
        }

        public void SetShaderStorageBufferDescriptor<T>(IBuffer<T> buffer, in DescriptorInfo info) where T : unmanaged
        {
            if (buffer is not IGLBuffer glBuffer) {
                throw new ArgumentException("Incompatible buffer", nameof(buffer));
            }

            GLDescriptorBindingInfo binding = (GLDescriptorBindingInfo)info.Binding;
            SetDescriptor(binding.BindingIndex, DescriptorType.ShaderStorageBuffer, glBuffer);
        }

        public void SetTextureDescriptor(ITexture texture, in DescriptorInfo info)
        {
            if (texture is not GLTexture glTexture) {
                throw new ArgumentException("Incompatible buffer", nameof(texture));
            }
            
            GLDescriptorBindingInfo binding = (GLDescriptorBindingInfo)info.Binding;
            
            if (binding.BindingIndex < _descriptors.Count) {
                // Update existing
                var x = _descriptors[(int)binding.BindingIndex];
                x.Item2 = glTexture;
                _descriptors[(int)binding.BindingIndex] = x;
            } else {
                SetDescriptor(binding.BindingIndex, DescriptorType.Texture, glTexture);    
            }
        }

        public void SetSamplerDescriptor(ISampler sampler, in DescriptorInfo info)
        {
            if (sampler is not GLSampler glSampler) {
                throw new ArgumentException("Incompatible sampler", nameof(sampler));
            }

            GLDescriptorBindingInfo binding = (GLDescriptorBindingInfo)info.Binding;

            if (binding.BindingIndex < _descriptors.Count) {
                // Update existing
                var x = _descriptors[(int)binding.BindingIndex];
                x.sampler = glSampler;
                _descriptors[(int)binding.BindingIndex] = x;
            } else {
                SetDescriptor(binding.BindingIndex, DescriptorType.Texture, string.Empty, glSampler);    
            }
        }
        
        public void Apply()
        {
            GL gl = _renderer.GLProvider.Gl;
            
            // TODO: NEED TO KNOW THE DESCRIPTOR SET LAYOUT SO THAT WE CAN ALWAYS SET EVERYTHING.
            
            for (int i = 0, ilen = _descriptors.Count; i < ilen; ++i) {
                (DescriptorType descriptorType, object resource, GLSampler? sampler) = _descriptors[i];
                switch (descriptorType) {
                    case DescriptorType.UniformBuffer:
                        IGLBuffer uniformBuffer = (IGLBuffer)resource;
                        uniformBuffer.Commit();
                        gl.BindBufferBase(BufferTargetARB.UNIFORM_BUFFER, (uint)i, uniformBuffer.GetHandleForCurrentFrame());
                        break;
                    
                    case DescriptorType.ShaderStorageBuffer:
                        IGLBuffer storageBuffer = (IGLBuffer)resource;
                        storageBuffer.Commit();
                        gl.BindBufferBase(BufferTargetARB.SHADER_STORAGE_BUFFER, (uint)i, storageBuffer.GetHandleForCurrentFrame());
                        break;
                    
                    case DescriptorType.Texture:
                        gl.ActiveTexture((TextureUnit)((uint)TextureUnit.TEXTURE0 + i));
                        gl.Uniform1i(i, i);
                        GLTexture texture = (GLTexture)resource;

                        if (texture.Handle != 0) {
                            gl.BindTexture(TextureTarget.TEXTURE_2D, texture.Handle);
                        }
                        else {
                            gl.BindTexture(TextureTarget.TEXTURE_2D, _renderer.FallbackTexture.Handle);
                        }

                        if (sampler == null) {
                            gl.BindSampler((uint)i, _renderer.FallbackSampler.Handle);
                        } else {
                            gl.BindSampler((uint)i, sampler.Handle);    
                        }
                        break;
                }
            }
        }

        public void Dispose() { }
    }
}