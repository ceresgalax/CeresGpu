using System;

namespace Metalancer.Graphics.Shaders
{
    public interface IDescriptorSet : IDisposable
    {
        void SetUniformBufferDescriptor<T>(IBuffer<T> buffer, in DescriptorInfo info) where T : unmanaged;
        void SetShaderStorageBufferDescriptor<T>(IBuffer<T> buffer, in DescriptorInfo info) where T : unmanaged;
        void SetTextureDescriptor(ITexture texture, in DescriptorInfo info);
        // TODO: Set Push Constants Resource?
    }
}