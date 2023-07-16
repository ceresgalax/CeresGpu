using System;

namespace CeresGpu.Graphics.Shaders
{
    public interface IDescriptorSet : IDisposable
    {
        void SetUniformBufferDescriptor<T>(IBuffer<T> buffer, in DescriptorInfo info) where T : unmanaged;
        void SetShaderStorageBufferDescriptor<T>(IBuffer<T> buffer, in DescriptorInfo info) where T : unmanaged;
        void SetTextureDescriptor(ITexture texture, in DescriptorInfo info);
        
        // NOTE: On future Push Constant support:
        // Push constants will likely be supported with the following method prototype:
        // `SetPushConstantsDescriptor<T>(in DescriptorInfo info)`
        //
        // This will be supported by a companion method in the command encoder: SetPushConstants<T>(T data)
        //
        // * For Metal, the BindingInfo.BindingIndex will be used to encode the buffer index that the push constant data is being
        //   encoded to. ( Which may just always be zero, since GLSL, our source shading language, doesn't support 
        //   multiple push constant blocks? (per stage at least))
        // * For Vulkan, BindingInfo.BindingIndex will be ignored. There is just a single blob of push constants data in
        //   vulkan.
        //   The Vulkan impl of command encoder will be responsible for setting the data into the correct offset if we need
        //   to support multiple push constants? (Although I don't think GLSL supports this in the same stage.)
        // * OpenGL Impl will likely just manage a buffer behind the scenes.
        //   However I'm curious how spirv-cross will transpile the shaders to OpenGL compatible spirv.. Will it 
        //   transpile to use a uniform buffer? Or maybe it will use traiditional glsl/OpenGL uniforms? Can be it be tuned in spirv cross arguments?
    }
}