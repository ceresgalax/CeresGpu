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
            Texture,
            Sampler
        }

        private readonly List<(DescriptorType, object, uint extraIndex)> _descriptors;
        
        private readonly GLRenderer _renderer;

        public GLDescriptorSet(GLRenderer renderer, in DescriptorSetCreationHints hints)
        {
            _renderer = renderer;
            _descriptors = new List<(DescriptorType, object, uint)>(hints.DescriptorCount);
        }
        
        private void SetDescriptor(uint index, DescriptorType descriptorType, object resource, uint extraIndex = 0)
        {
            while (index >= _descriptors.Count) {
                _descriptors.Add((DescriptorType.Unset, string.Empty, 0));
            }
            _descriptors[(int)index] = (descriptorType, resource, extraIndex);
        }
        
        public void SetUniformBufferDescriptor<T>(IBuffer<T> buffer, in DescriptorInfo info) where T : unmanaged
        {
            if (buffer is not IGLBuffer glBuffer) {
                throw new ArgumentException("Incompatible buffer", nameof(buffer));
            }
            
            SetDescriptor(info.BindingIndex, DescriptorType.UniformBuffer, glBuffer);

            // GL gl = _glProvider.Gl;
            // // gl.BindBuffer(BufferTargetARB.UNIFORM_BUFFER, glBuffer.Handle);
            // gl.BindBufferBase(BufferTargetARB.UNIFORM_BUFFER, (uint)info.BindingIndex, glBuffer.Handle);
        }

        public void SetShaderStorageBufferDescriptor<T>(IBuffer<T> buffer, in DescriptorInfo info) where T : unmanaged
        {
            if (buffer is not IGLBuffer glBuffer) {
                throw new ArgumentException("Incompatible buffer", nameof(buffer));
            }

            SetDescriptor(info.BindingIndex, DescriptorType.ShaderStorageBuffer, glBuffer);
        }

        public void SetTextureDescriptor(ITexture texture, in DescriptorInfo info)
        {
            if (texture is not GLTexture glTexture) {
                throw new ArgumentException("Incompatible buffer", nameof(texture));
            }
            
            SetDescriptor(info.BindingIndex, DescriptorType.Texture, glTexture);
        }

        public void SetSamplerDescriptor(ISampler sampler, in DescriptorInfo info)
        {
            if (sampler is not GLSampler glSampler) {
                throw new ArgumentException("Incompatible sampler", nameof(sampler));
            }
            
            SetDescriptor(info.SamplerIndex, DescriptorType.Sampler, glSampler, (uint)info.BindingIndex);
        }

        private readonly HashSet<int> _texturesWithSetSamplers = new();
        
        public void Apply()
        {
            GL gl = _renderer.GLProvider.Gl;
            
            _texturesWithSetSamplers.Clear();
            for (int i = 0, ilen = _descriptors.Count; i < ilen; ++i) {
                (DescriptorType descriptorType, object resource, uint extraIndex) = _descriptors[i];
                if (descriptorType == DescriptorType.Sampler) {
                    _texturesWithSetSamplers.Add((int)extraIndex);
                }
            }
            
            // TODO: NEED TO KNOW THE DESCRIPTOR SET LAYOUT SO THAT WE CAN ALWAYS SET EVERYTHING.
            
            for (int i = 0, ilen = _descriptors.Count; i < ilen; ++i) {
                (DescriptorType descriptorType, object resource, uint extraIndex) = _descriptors[i];
                switch (descriptorType) {
                    case DescriptorType.UniformBuffer:
                        // gl.BindBuffer(BufferTargetARB.UNIFORM_BUFFER, glBuffer.Handle);
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
                        
                        if (!_texturesWithSetSamplers.Contains(i)) {
                            gl.BindSampler((uint)i, _renderer.FallbackSampler.Handle);
                        }
                        break;
                    
                    case DescriptorType.Sampler:
                        GLSampler sampler = (GLSampler)resource;
                        gl.BindSampler(extraIndex, sampler.Handle);
                        break;
                }
            }
        }

        public void Dispose() { }
    }
}