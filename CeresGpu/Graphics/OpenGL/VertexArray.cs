using System;
using System.Collections.Generic;
using CeresGL;
using CeresGpu.Graphics.Shaders;

namespace CeresGpu.Graphics.OpenGL
{
    public sealed class VertexArray : IVertexDescriptor, IDisposable
    {
        private readonly IGLProvider _glProvider;
        private uint _handle;

        public uint Handle => _handle;

        private readonly List<uint> _prevVertexBufferHandles = new();

        public VertexArray(IGLProvider glProvider)
        {
            _glProvider = glProvider;
            Span<uint> handleBuffer = stackalloc uint[1];
            glProvider.Gl.GenVertexArrays(1, handleBuffer);
            _handle = handleBuffer[0];
        }

        public void RecreateIfNecesaryAndBind(IShader shader, IVertexBufferLayout layout, IUntypedVertexBufferAdapter adapter)
        {
            GL gl = _glProvider.Gl;

            ReadOnlySpan<object?> buffers = adapter.VertexBuffers;
            
            if (_prevVertexBufferHandles.Count == buffers.Length) {
                bool same = true;
                for (int i = 0, ilen = _prevVertexBufferHandles.Count; i < ilen; ++i) {
                    IGLBuffer? buffer = adapter.VertexBuffers[i] as IGLBuffer;
                    if (_prevVertexBufferHandles[i] != buffer?.GetHandleForCurrentFrame()) {
                        same = false;
                        break;
                    }
                }
                if (same) {
                    // Buffers have not changed, VAO is still valid.
                    gl.BindVertexArray(_handle);
                    return;
                }
            }
            
            Span<uint> pVao = stackalloc uint[1] { _handle };
            if (_handle != 0) {
                gl.DeleteVertexArrays(1, pVao);
            }
            gl.CreateVertexArrays(1, pVao);
            _handle = pVao[0];
            
            gl.BindVertexArray(_handle);

            //ReadOnlySpan<VertexBufferLayout> layouts = shader.GetVertexBufferLayouts();
            ReadOnlySpan<ShaderVertexAttributeDescriptor> shaderAttributes = shader.GetVertexAttributeDescriptors();
            ReadOnlySpan<VblBufferDescriptor> bufferDescriptors = layout.BufferDescriptors;
            
            foreach (ref readonly VblAttributeDescriptor attributeDescriptor in layout.AttributeDescriptors) {
                uint shaderAttributeIndex = attributeDescriptor.AttributeIndex;
                if (shaderAttributeIndex >= shaderAttributes.Length) {
                    // TODO: Should this log an error or something?
                    continue;
                }
                if (attributeDescriptor.BufferIndex > buffers.Length) {
                    // TODO: Should this log an error or something?
                    continue;
                }
                if (attributeDescriptor.BufferIndex > bufferDescriptors.Length) {
                    // TODO: Should this log an error or something?
                    continue;
                }
                
                IGLBuffer? buffer = buffers[(int)attributeDescriptor.BufferIndex] as IGLBuffer;
                if (buffer == null) {
                    // TODO: This should log an error?
                    continue;
                }
                
                ref readonly VblBufferDescriptor bufferDescriptor = ref bufferDescriptors[(int)attributeDescriptor.BufferIndex];
                ref readonly ShaderVertexAttributeDescriptor shaderAttributeDescriptor = ref shaderAttributes[(int)shaderAttributeIndex];
                
                gl.EnableVertexAttribArray(shaderAttributeIndex);
                gl.BindBuffer(BufferTargetARB.ARRAY_BUFFER, buffer.GetHandleForCurrentFrame());
                SetAttribute(gl, shaderAttributeIndex, shaderAttributeDescriptor, attributeDescriptor, bufferDescriptor);
                gl.VertexAttribDivisor(shaderAttributeIndex, bufferDescriptor.StepFunction == VertexStepFunction.PerInstance ? 1u : 0u);
            }

            _prevVertexBufferHandles.Clear();
            foreach (IGLBuffer? buffer in buffers) {
                _prevVertexBufferHandles.Add(buffer?.GetHandleForCurrentFrame() ?? 0);
            }
        }
        
        private void CheckDisposed()
        {
            if (_handle == 0) {
                throw new ObjectDisposedException(null);
            }
        }
        
        private void ReleaseUnmanagedResources(GL gl)
        {
            Span<uint> handleBuffer = stackalloc uint[1];
            handleBuffer[0] = _handle;
            gl.DeleteVertexArrays(1, handleBuffer);
        }

        public void Dispose()
        {
            CheckDisposed();
            ReleaseUnmanagedResources(_glProvider.Gl);
            GC.SuppressFinalize(this);
        }

        ~VertexArray()
        {
            _glProvider.AddFinalizerAction(ReleaseUnmanagedResources);
        }
        
        private void SetAttribute(GL gl, uint index, ShaderVertexAttributeDescriptor attrib, VblAttributeDescriptor vblAttributeDescriptor, VblBufferDescriptor bufferDescriptor)
        {
            int size = attrib.Format switch {
                VertexFormat.Char => 1,
                VertexFormat.CharNormalized => 1,
                VertexFormat.UChar => 1,
                VertexFormat.UCharNormalized => 1,
                VertexFormat.Half => 1,
                VertexFormat.Float => 1,
                VertexFormat.Short => 1,
                VertexFormat.ShortNormalized => 1,
                VertexFormat.UShort => 1,
                VertexFormat.UShortNormalized => 1,
                VertexFormat.Int => 1,
                VertexFormat.UInt => 1,
                
                VertexFormat.Char2 => 2,
                VertexFormat.Char2Normalized => 2,
                VertexFormat.UChar2 => 2,
                VertexFormat.UChar2Normalized => 2,
                VertexFormat.Half2 => 2,
                VertexFormat.Float2 => 2, 
                VertexFormat.Short2 => 2,
                VertexFormat.Short2Normalized => 2,
                VertexFormat.UShort2 => 2,
                VertexFormat.UShort2Normalized => 2,
                VertexFormat.Int2 => 2,
                VertexFormat.UInt2 => 2,
                
                VertexFormat.Char3 => 3,
                VertexFormat.Char3Normalized => 3,
                VertexFormat.UChar3 => 3,
                VertexFormat.UChar3Normalized => 3,
                VertexFormat.Half3 => 3,
                VertexFormat.Float3 => 3, 
                VertexFormat.Short3 => 3,
                VertexFormat.Short3Normalized => 3,
                VertexFormat.UShort3 => 3,
                VertexFormat.UShort3Normalized => 3,
                VertexFormat.Int3 => 3,
                VertexFormat.UInt3 => 3,
                
                VertexFormat.Char4 => 4,
                VertexFormat.Char4Normalized => 4,
                VertexFormat.UChar4 => 4,
                VertexFormat.UChar4Normalized => 4,
                VertexFormat.Half4 => 4,
                VertexFormat.Float4 => 4, 
                VertexFormat.Short4 => 4,
                VertexFormat.Short4Normalized => 4,
                VertexFormat.UShort4 => 4,
                VertexFormat.UShort4Normalized => 4,
                VertexFormat.Int4 => 4,
                VertexFormat.UInt4 => 4
                
                , VertexFormat.Int1010102Normalized => 1
                , VertexFormat.UInt1010102Normalized => 1
                , VertexFormat.UChar4Normalized_BGRA => (int)PixelFormat.BGRA
                , VertexFormat.Invalid => 0
                , _ => throw new ArgumentOutOfRangeException()
            };
            
            int stride = (int)bufferDescriptor.Stride;
            IntPtr offset = new IntPtr(vblAttributeDescriptor.BufferOffset);
            
            switch (attrib.Format) {
                // Int
                case VertexFormat.Char:
                case VertexFormat.Char2:
                case VertexFormat.Char3:
                case VertexFormat.Char4:
                    gl.glVertexAttribIPointer(index, size, (uint)VertexAttribIType.BYTE, stride, offset);
                    break;
                    
                case VertexFormat.UChar:
                case VertexFormat.UChar2:
                case VertexFormat.UChar3:
                case VertexFormat.UChar4:
                    gl.glVertexAttribIPointer(index, size, (uint)VertexAttribIType.UNSIGNED_BYTE, stride, offset);
                    break;
                    
                case VertexFormat.Short:
                case VertexFormat.Short2:
                case VertexFormat.Short3:
                case VertexFormat.Short4:
                    gl.glVertexAttribIPointer(index, size, (uint)VertexAttribIType.SHORT, stride, offset);
                    break;
                    
                case VertexFormat.UShort:
                case VertexFormat.UShort2:
                case VertexFormat.UShort3:
                case VertexFormat.UShort4:
                    gl.glVertexAttribIPointer(index, size, (uint)VertexAttribIType.UNSIGNED_SHORT, stride, offset);
                    break;
                    
                case VertexFormat.Int:
                case VertexFormat.Int2:
                case VertexFormat.Int3:
                case VertexFormat.Int4:
                    gl.glVertexAttribIPointer(index, size, (uint)VertexAttribIType.INT, stride, offset);
                    break;
                
                case VertexFormat.UInt:
                case VertexFormat.UInt2:
                case VertexFormat.UInt3:
                case VertexFormat.UInt4:
                    gl.glVertexAttribIPointer(index, size, (uint)VertexAttribIType.UNSIGNED_INT, stride, offset);
                    break;
                
                // Float
                
                case VertexFormat.CharNormalized:
                case VertexFormat.Char2Normalized:
                case VertexFormat.Char3Normalized:
                case VertexFormat.Char4Normalized:
                    gl.glVertexAttribPointer(index, size, (uint)VertexAttribType.BYTE, true, stride, offset);
                    break;
                
                case VertexFormat.UCharNormalized:
                case VertexFormat.UChar2Normalized:
                case VertexFormat.UChar3Normalized:
                case VertexFormat.UChar4Normalized:
                    gl.glVertexAttribPointer(index, size, (uint)VertexAttribType.UNSIGNED_BYTE, true, stride, offset);
                    break;
                
                case VertexFormat.ShortNormalized:
                case VertexFormat.Short2Normalized:
                case VertexFormat.Short3Normalized:
                case VertexFormat.Short4Normalized:
                    gl.glVertexAttribPointer(index, size, (uint)VertexAttribType.SHORT, true, stride, offset);
                    break;
                
                case VertexFormat.UShortNormalized:
                case VertexFormat.UShort2Normalized:
                case VertexFormat.UShort3Normalized:
                case VertexFormat.UShort4Normalized:
                    gl.glVertexAttribPointer(index, size, (uint)VertexAttribType.SHORT, true, stride, offset);
                    break;
                
                case VertexFormat.Half:
                case VertexFormat.Half2:
                case VertexFormat.Half3:
                case VertexFormat.Half4:
                    gl.glVertexAttribPointer(index, size, (uint)VertexAttribType.HALF_FLOAT, false, stride, offset);
                    break;
                
                case VertexFormat.Float:
                case VertexFormat.Float2:
                case VertexFormat.Float3:
                case VertexFormat.Float4:
                    gl.glVertexAttribPointer(index, size, (uint)VertexAttribType.FLOAT, false, stride, offset);
                    break;

                case VertexFormat.Invalid:
                    break;
                case VertexFormat.Int1010102Normalized:
                    gl.glVertexAttribPointer(index, size, (uint)VertexAttribType.INT_2_10_10_10_REV, true, stride, offset);
                    break;
                case VertexFormat.UInt1010102Normalized:
                    gl.glVertexAttribPointer(index, size, (uint)VertexAttribType.UNSIGNED_INT_2_10_10_10_REV, true, stride, offset);
                    break;
                case VertexFormat.UChar4Normalized_BGRA:
                    gl.glVertexAttribPointer(index, size, (uint)VertexAttribType.FLOAT, true, stride, offset);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
    }
}