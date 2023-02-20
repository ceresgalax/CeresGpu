using System;
using System.Numerics;
using System.Runtime.InteropServices;
using CeresGL;
using CeresGpu.Graphics;
using Buffer = System.Buffer;
using PixelFormat = CeresGL.PixelFormat;

namespace CeresGpu.Graphics.OpenGL
{
    public sealed class GLTexture : ITexture
    {
        private readonly IGLProvider _glProvider;
        
        private uint _texture;
        private uint _pixelUnpackBuffer;

        private uint _width;
        private uint _height;

        private IntPtr _weakHandle;

        public GLTexture(IGLProvider glProvider)
        {
            _glProvider = glProvider;
            _weakHandle = GCHandle.ToIntPtr(GCHandle.Alloc(this, GCHandleType.Weak));
        }
        
        public uint Handle => _texture;
        public uint Width => _width;
        public uint Height => _height;

        public Vector2 Size => new Vector2(_width, _height);
        public IntPtr WeakHandle => _weakHandle;

        public void Set(ReadOnlySpan<byte> data, uint width, uint height, InputFormat format)
        {
            IntPtr mappedBufferAddress = IntPtr.Zero;
            uint size = width * height * (uint)format.GetBytesPerPixel();
            
            _glProvider.DoOnContextThread(gl => {
                if (_texture == 0) {
                    uint[] textures = new uint[1];
                    gl.GenTextures(textures.Length, textures);
                    gl.BindTexture(TextureTarget.TEXTURE_2D, textures[0]);
                    gl.TexParameteri(TextureTarget.TEXTURE_2D, TextureParameterName.TEXTURE_WRAP_S, (int)TextureWrapMode.CLAMP_TO_EDGE);
                    gl.TexParameteri(TextureTarget.TEXTURE_2D, TextureParameterName.TEXTURE_WRAP_T, (int)TextureWrapMode.CLAMP_TO_EDGE);
                    gl.TexParameteri(TextureTarget.TEXTURE_2D, TextureParameterName.TEXTURE_MIN_FILTER, (int)TextureMinFilter.NEAREST);
                    gl.TexParameteri(TextureTarget.TEXTURE_2D, TextureParameterName.TEXTURE_MAG_FILTER, (int)TextureMinFilter.NEAREST);
                    _texture = textures[0];
                }
            
                if (_pixelUnpackBuffer == 0) {
                    uint[] buffers = new uint[1];
                    gl.GenBuffers(buffers.Length, buffers);
                    _pixelUnpackBuffer = buffers[0];
                }
                
                gl.BindBuffer(BufferTargetARB.PIXEL_UNPACK_BUFFER, _pixelUnpackBuffer);
                gl.BufferData(BufferTargetARB.PIXEL_UNPACK_BUFFER, size, BufferUsageARB.STATIC_DRAW); // TODO: Is STATIC_DRAW the best hint for Pixel Unpack Buffers?
                
                mappedBufferAddress = gl.MapBuffer(BufferTargetARB.PIXEL_UNPACK_BUFFER, BufferAccessARB.WRITE_ONLY);
            });
            
            if (mappedBufferAddress == IntPtr.Zero) {
                throw new InvalidOperationException("Failed to map buffer");
            }

            _width = width;
            _height = height;
            
            unsafe {
                fixed (byte* p = &data.GetPinnableReference()) {
                    Buffer.MemoryCopy(p, (void*)mappedBufferAddress, size, size);
                }
            }
            
            _glProvider.DoOnContextThread(gl => {
                gl.BindTexture(TextureTarget.TEXTURE_2D, _texture);
                gl.BindBuffer(BufferTargetARB.PIXEL_UNPACK_BUFFER, _pixelUnpackBuffer);
                gl.UnmapBuffer(BufferTargetARB.PIXEL_UNPACK_BUFFER);
                (InternalFormat internalFormat, PixelFormat pixelFormat, PixelType pixelType) = GetFormats(format);
                gl.TexImage2DPixelBuffer(TextureTarget.TEXTURE_2D, 0, internalFormat, (int)width, (int)height, 0, pixelFormat, pixelType, 0);
            });
        }

        public void SetFilter(MinMagFilter min, MinMagFilter mag)
        {
            // TODO: Move this validation to be common for all impl types.
            if (_texture == 0) {
                throw new InvalidOperationException("Filter cannot be set before setting texture.");
            }
            
            _glProvider.DoOnContextThread(gl => {
                gl.BindTexture(TextureTarget.TEXTURE_2D, _texture);
                gl.TexParameteri(TextureTarget.TEXTURE_2D, TextureParameterName.TEXTURE_MIN_FILTER, (int)TranslateMinFilter(min));
                gl.TexParameteri(TextureTarget.TEXTURE_2D, TextureParameterName.TEXTURE_MAG_FILTER, (int)TranslateMagFilter(mag));
            });
        }

        private TextureMinFilter TranslateMinFilter(MinMagFilter filter)
        {
            return filter switch {
                MinMagFilter.Nearest => TextureMinFilter.NEAREST
                , MinMagFilter.Linear => TextureMinFilter.LINEAR
                , _ => throw new ArgumentOutOfRangeException(nameof(filter), filter, null)
            };
        }
        
        private TextureMagFilter TranslateMagFilter(MinMagFilter filter)
        {
            return filter switch {
                MinMagFilter.Nearest => TextureMagFilter.NEAREST
                , MinMagFilter.Linear => TextureMagFilter.LINEAR
                , _ => throw new ArgumentOutOfRangeException(nameof(filter), filter, null)
            };
        }

        private (InternalFormat, PixelFormat, PixelType) GetFormats(InputFormat format)
        {
            return format switch {
                    InputFormat.R4G4_UNORM_PACK8 => throw new NotImplementedException()
                    , InputFormat.R4G4B4A4_UNORM_PACK16 => throw new NotImplementedException()
                    , InputFormat.B4G4R4A4_UNORM_PACK16 => throw new NotImplementedException()
                    , InputFormat.R5G6B5_UNORM_PACK16 => throw new NotImplementedException()
                    , InputFormat.B5G6R5_UNORM_PACK16 => throw new NotImplementedException()
                    , InputFormat.R5G5B5A1_UNORM_PACK16 => throw new NotImplementedException()
                    , InputFormat.B5G5R5A1_UNORM_PACK16 => throw new NotImplementedException()
                    , InputFormat.A1R5G5B5_UNORM_PACK16 => throw new NotImplementedException()
                    , InputFormat.R8_UNORM => (InternalFormat.R8, PixelFormat.RED, PixelType.UNSIGNED_BYTE)
                    , InputFormat.R8_SNORM => throw new NotImplementedException()
                    , InputFormat.R8_USCALED => throw new NotImplementedException()
                    , InputFormat.R8_SSCALED => throw new NotImplementedException()
                    , InputFormat.R8_UINT => throw new NotImplementedException()
                    , InputFormat.R8_SINT => throw new NotImplementedException()
                    , InputFormat.R8_SRGB => throw new NotImplementedException()
                    , InputFormat.R8G8_UNORM => throw new NotImplementedException()
                    , InputFormat.R8G8_SNORM => throw new NotImplementedException()
                    , InputFormat.R8G8_USCALED => throw new NotImplementedException()
                    , InputFormat.R8G8_SSCALED => throw new NotImplementedException()
                    , InputFormat.R8G8_UINT => throw new NotImplementedException()
                    , InputFormat.R8G8_SINT => throw new NotImplementedException()
                    , InputFormat.R8G8_SRGB => throw new NotImplementedException()
                    , InputFormat.R8G8B8_UNORM => throw new NotImplementedException()
                    , InputFormat.R8G8B8_SNORM => throw new NotImplementedException()
                    , InputFormat.R8G8B8_USCALED => throw new NotImplementedException()
                    , InputFormat.R8G8B8_SSCALED => throw new NotImplementedException()
                    , InputFormat.R8G8B8_UINT => throw new NotImplementedException()
                    , InputFormat.R8G8B8_SINT => throw new NotImplementedException()
                    , InputFormat.R8G8B8_SRGB => throw new NotImplementedException()
                    , InputFormat.B8G8R8_UNORM => throw new NotImplementedException()
                    , InputFormat.B8G8R8_SNORM => throw new NotImplementedException()
                    , InputFormat.B8G8R8_USCALED => throw new NotImplementedException()
                    , InputFormat.B8G8R8_SSCALED => throw new NotImplementedException()
                    , InputFormat.B8G8R8_UINT => throw new NotImplementedException()
                    , InputFormat.B8G8R8_SINT => throw new NotImplementedException()
                    , InputFormat.B8G8R8_SRGB => throw new NotImplementedException()
                    , InputFormat.R8G8B8A8_UNORM => (InternalFormat.RGBA, PixelFormat.RGBA, PixelType.UNSIGNED_BYTE)
                    , InputFormat.R8G8B8A8_SNORM => throw new NotImplementedException()
                    , InputFormat.R8G8B8A8_USCALED => throw new NotImplementedException()
                    , InputFormat.R8G8B8A8_SSCALED => throw new NotImplementedException()
                    , InputFormat.R8G8B8A8_UINT => throw new NotImplementedException()
                    , InputFormat.R8G8B8A8_SINT => throw new NotImplementedException()
                    , InputFormat.R8G8B8A8_SRGB => throw new NotImplementedException()
                    , InputFormat.B8G8R8A8_UNORM => (InternalFormat.RGBA, PixelFormat.BGRA, PixelType.UNSIGNED_BYTE)
                    , InputFormat.B8G8R8A8_SNORM => throw new NotImplementedException()
                    , InputFormat.B8G8R8A8_USCALED => throw new NotImplementedException()
                    , InputFormat.B8G8R8A8_SSCALED => throw new NotImplementedException()
                    , InputFormat.B8G8R8A8_UINT => throw new NotImplementedException()
                    , InputFormat.B8G8R8A8_SINT => throw new NotImplementedException()
                    , InputFormat.B8G8R8A8_SRGB => throw new NotImplementedException()
                    , InputFormat.A8B8G8R8_UNORM_PACK32 => throw new NotImplementedException()
                    , InputFormat.A8B8G8R8_SNORM_PACK32 => throw new NotImplementedException()
                    , InputFormat.A8B8G8R8_USCALED_PACK32 => throw new NotImplementedException()
                    , InputFormat.A8B8G8R8_SSCALED_PACK32 => throw new NotImplementedException()
                    , InputFormat.A8B8G8R8_UINT_PACK32 => throw new NotImplementedException()
                    , InputFormat.A8B8G8R8_SINT_PACK32 => throw new NotImplementedException()
                    , InputFormat.A8B8G8R8_SRGB_PACK32 => throw new NotImplementedException()
                    , InputFormat.A2R10G10B10_UNORM_PACK32 => throw new NotImplementedException()
                    , InputFormat.A2R10G10B10_SNORM_PACK32 => throw new NotImplementedException()
                    , InputFormat.A2R10G10B10_USCALED_PACK32 => throw new NotImplementedException()
                    , InputFormat.A2R10G10B10_SSCALED_PACK32 => throw new NotImplementedException()
                    , InputFormat.A2R10G10B10_UINT_PACK32 => throw new NotImplementedException()
                    , InputFormat.A2R10G10B10_SINT_PACK32 => throw new NotImplementedException()
                    , InputFormat.A2B10G10R10_UNORM_PACK32 => throw new NotImplementedException()
                    , InputFormat.A2B10G10R10_SNORM_PACK32 => throw new NotImplementedException()
                    , InputFormat.A2B10G10R10_USCALED_PACK32 => throw new NotImplementedException()
                    , InputFormat.A2B10G10R10_SSCALED_PACK32 => throw new NotImplementedException()
                    , InputFormat.A2B10G10R10_UINT_PACK32 => throw new NotImplementedException()
                    , InputFormat.A2B10G10R10_SINT_PACK32 => throw new NotImplementedException()
                    , InputFormat.R16_UNORM => throw new NotImplementedException()
                    , InputFormat.R16_SNORM => throw new NotImplementedException()
                    , InputFormat.R16_USCALED => throw new NotImplementedException()
                    , InputFormat.R16_SSCALED => throw new NotImplementedException()
                    , InputFormat.R16_UINT => throw new NotImplementedException()
                    , InputFormat.R16_SINT => throw new NotImplementedException()
                    , InputFormat.R16_SFLOAT => throw new NotImplementedException()
                    , InputFormat.R16G16_UNORM => throw new NotImplementedException()
                    , InputFormat.R16G16_SNORM => throw new NotImplementedException()
                    , InputFormat.R16G16_USCALED => throw new NotImplementedException()
                    , InputFormat.R16G16_SSCALED => throw new NotImplementedException()
                    , InputFormat.R16G16_UINT => throw new NotImplementedException()
                    , InputFormat.R16G16_SINT => throw new NotImplementedException()
                    , InputFormat.R16G16_SFLOAT => throw new NotImplementedException()
                    , InputFormat.R16G16B16_UNORM => throw new NotImplementedException()
                    , InputFormat.R16G16B16_SNORM => throw new NotImplementedException()
                    , InputFormat.R16G16B16_USCALED => throw new NotImplementedException()
                    , InputFormat.R16G16B16_SSCALED => throw new NotImplementedException()
                    , InputFormat.R16G16B16_UINT => throw new NotImplementedException()
                    , InputFormat.R16G16B16_SINT => throw new NotImplementedException()
                    , InputFormat.R16G16B16_SFLOAT => throw new NotImplementedException()
                    , InputFormat.R16G16B16A16_UNORM => throw new NotImplementedException()
                    , InputFormat.R16G16B16A16_SNORM => throw new NotImplementedException()
                    , InputFormat.R16G16B16A16_USCALED => throw new NotImplementedException()
                    , InputFormat.R16G16B16A16_SSCALED => throw new NotImplementedException()
                    , InputFormat.R16G16B16A16_UINT => throw new NotImplementedException()
                    , InputFormat.R16G16B16A16_SINT => throw new NotImplementedException()
                    , InputFormat.R16G16B16A16_SFLOAT => throw new NotImplementedException()
                    , InputFormat.R32_UINT => throw new NotImplementedException()
                    , InputFormat.R32_SINT => throw new NotImplementedException()
                    , InputFormat.R32_SFLOAT => throw new NotImplementedException()
                    , InputFormat.R32G32_UINT => throw new NotImplementedException()
                    , InputFormat.R32G32_SINT => throw new NotImplementedException()
                    , InputFormat.R32G32_SFLOAT => throw new NotImplementedException()
                    , InputFormat.R32G32B32_UINT => throw new NotImplementedException()
                    , InputFormat.R32G32B32_SINT => throw new NotImplementedException()
                    , InputFormat.R32G32B32_SFLOAT => throw new NotImplementedException()
                    , InputFormat.R32G32B32A32_UINT => throw new NotImplementedException()
                    , InputFormat.R32G32B32A32_SINT => throw new NotImplementedException()
                    , InputFormat.R32G32B32A32_SFLOAT => throw new NotImplementedException()
                    , InputFormat.R64_UINT => throw new NotImplementedException()
                    , InputFormat.R64_SINT => throw new NotImplementedException()
                    , InputFormat.R64_SFLOAT => throw new NotImplementedException()
                    , InputFormat.R64G64_UINT => throw new NotImplementedException()
                    , InputFormat.R64G64_SINT => throw new NotImplementedException()
                    , InputFormat.R64G64_SFLOAT => throw new NotImplementedException()
                    , InputFormat.R64G64B64_UINT => throw new NotImplementedException()
                    , InputFormat.R64G64B64_SINT => throw new NotImplementedException()
                    , InputFormat.R64G64B64_SFLOAT => throw new NotImplementedException()
                    , InputFormat.R64G64B64A64_UINT => throw new NotImplementedException()
                    , InputFormat.R64G64B64A64_SINT => throw new NotImplementedException()
                    , InputFormat.R64G64B64A64_SFLOAT => throw new NotImplementedException()
                    , InputFormat.B10G11R11_UFLOAT_PACK32 => throw new NotImplementedException()
                    , _ => throw new ArgumentOutOfRangeException(nameof(format), format, null)
                };
        }

        private void ReleaseUnmanagedResources(GL gl)
        {
            uint[] handles = new uint[1];
            handles[0] = _pixelUnpackBuffer;
            gl.DeleteBuffers(1, handles);
            handles[0] = _texture;
            gl.DeleteTextures(1, handles);

            _pixelUnpackBuffer = 0;
            _texture = 0;
            
            if (_weakHandle != IntPtr.Zero) {
                GCHandle.FromIntPtr(_weakHandle).Free();
                _weakHandle = IntPtr.Zero;
            }
        }

        public void Dispose()
        {
            // TODO: Check if already disposed
            ReleaseUnmanagedResources(_glProvider.Gl);
            GC.SuppressFinalize(this);
        }

        ~GLTexture()
        {
            _glProvider.AddFinalizerAction(ReleaseUnmanagedResources);
        }
    }
}
