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

        public void Set(ReadOnlySpan<byte> data, uint width, uint height, ColorFormat format)
        {
            IntPtr mappedBufferAddress = IntPtr.Zero;
            uint size = width * height * (uint)format.GetBytesPerPixel();
            
            _glProvider.DoOnContextThread(gl => {
                if (_texture == 0) {
                    uint[] textures = new uint[1];
                    gl.GenTextures(textures.Length, textures);
                    gl.BindTexture(TextureTarget.TEXTURE_2D, textures[0]);
                    
                    // THIS IS NOW DONE BY SAMPLERS.
                    // gl.TexParameteri(TextureTarget.TEXTURE_2D, TextureParameterName.TEXTURE_WRAP_S, (int)TextureWrapMode.CLAMP_TO_EDGE);
                    // gl.TexParameteri(TextureTarget.TEXTURE_2D, TextureParameterName.TEXTURE_WRAP_T, (int)TextureWrapMode.CLAMP_TO_EDGE);
                    // gl.TexParameteri(TextureTarget.TEXTURE_2D, TextureParameterName.TEXTURE_MIN_FILTER, (int)TextureMinFilter.NEAREST);
                    // gl.TexParameteri(TextureTarget.TEXTURE_2D, TextureParameterName.TEXTURE_MAG_FILTER, (int)TextureMinFilter.NEAREST);
                    
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
                (InternalFormat internalFormat, PixelFormat pixelFormat, PixelType pixelType) = format.GetGLFormats();
                gl.TexImage2DPixelBuffer(TextureTarget.TEXTURE_2D, 0, internalFormat, (int)width, (int)height, 0, pixelFormat, pixelType, 0);
            });
        }

        public void DeclareMutationInPass()
        {
            throw new NotImplementedException();
        }

        public void DeclareReadInPass()
        {
            throw new NotImplementedException();
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
