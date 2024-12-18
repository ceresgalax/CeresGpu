using System;
using System.Collections.Generic;
using CeresGL;
using CeresGpu.Graphics.Shaders;

namespace CeresGpu.Graphics.OpenGL
{
    public sealed class VertexArray : IVertexDescriptor, IDisposable
    {
        private IGLProvider _glProvider;
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

        public void RecreateIfNecesaryAndBind(List<IGLBuffer?> buffers, IShader shader)
        {
            GL gl = _glProvider.Gl;
            
            if (_prevVertexBufferHandles.Count == buffers.Count) {
                bool same = true;
                for (int i = 0, ilen = _prevVertexBufferHandles.Count; i < ilen; ++i) {
                    if (_prevVertexBufferHandles[i] != buffers[i]?.GetHandleForCurrentFrame()) {
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

            ReadOnlySpan<VertexBufferLayout> layouts = shader.GetVertexBufferLayouts();
            
            foreach (VertexAttributeDescriptor attrib in shader.GetVertexAttributeDescriptors()) {
                if (attrib.BufferIndex > buffers.Count) {
                    continue;
                }
                IGLBuffer? buffer = buffers[(int)attrib.BufferIndex];
                if (buffer == null) {
                    continue;
                }
                if (attrib.BufferIndex > layouts.Length) {
                    continue;
                }

                VertexBufferLayout layout = layouts[(int)attrib.BufferIndex];
                gl.EnableVertexAttribArray(attrib.Index);
                gl.BindBuffer(BufferTargetARB.ARRAY_BUFFER, buffer.GetHandleForCurrentFrame());
                SetAttribute(gl, attrib, layout);
                gl.VertexAttribDivisor(attrib.Index, layout.StepFunction == VertexStepFunction.PerInstance ? 1u : 0u);
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
        
        private void SetAttribute(GL gl, VertexAttributeDescriptor attrib, VertexBufferLayout layout)
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
                
                switch (attrib.Format) {
                    // Int
                    case VertexFormat.Char:
                    case VertexFormat.Char2:
                    case VertexFormat.Char3:
                    case VertexFormat.Char4:
                        gl.glVertexAttribIPointer(attrib.Index, size, (uint)VertexAttribIType.BYTE, (int)layout.Stride, new IntPtr(attrib.Offset));
                        break;
                        
                    case VertexFormat.UChar:
                    case VertexFormat.UChar2:
                    case VertexFormat.UChar3:
                    case VertexFormat.UChar4:
                        gl.glVertexAttribIPointer(attrib.Index, size, (uint)VertexAttribIType.UNSIGNED_BYTE, (int)layout.Stride, new IntPtr(attrib.Offset));
                        break;
                        
                    case VertexFormat.Short:
                    case VertexFormat.Short2:
                    case VertexFormat.Short3:
                    case VertexFormat.Short4:
                        gl.glVertexAttribIPointer(attrib.Index, size, (uint)VertexAttribIType.SHORT, (int)layout.Stride, new IntPtr(attrib.Offset));
                        break;
                        
                    case VertexFormat.UShort:
                    case VertexFormat.UShort2:
                    case VertexFormat.UShort3:
                    case VertexFormat.UShort4:
                        gl.glVertexAttribIPointer(attrib.Index, size, (uint)VertexAttribIType.UNSIGNED_SHORT, (int)layout.Stride, new IntPtr(attrib.Offset));
                        break;
                        
                    case VertexFormat.Int:
                    case VertexFormat.Int2:
                    case VertexFormat.Int3:
                    case VertexFormat.Int4:
                        gl.glVertexAttribIPointer(attrib.Index, size, (uint)VertexAttribIType.INT, (int)layout.Stride, new IntPtr(attrib.Offset));
                        break;
                    
                    case VertexFormat.UInt:
                    case VertexFormat.UInt2:
                    case VertexFormat.UInt3:
                    case VertexFormat.UInt4:
                        gl.glVertexAttribIPointer(attrib.Index, size, (uint)VertexAttribIType.UNSIGNED_INT, (int)layout.Stride, new IntPtr(attrib.Offset));
                        break;
                    
                    // Float
                    
                    case VertexFormat.CharNormalized:
                    case VertexFormat.Char2Normalized:
                    case VertexFormat.Char3Normalized:
                    case VertexFormat.Char4Normalized:
                        gl.glVertexAttribPointer(attrib.Index, size, (uint)VertexAttribType.BYTE, true, (int)layout.Stride, new IntPtr(attrib.Offset));
                        break;
                    
                    case VertexFormat.UCharNormalized:
                    case VertexFormat.UChar2Normalized:
                    case VertexFormat.UChar3Normalized:
                    case VertexFormat.UChar4Normalized:
                        gl.glVertexAttribPointer(attrib.Index, size, (uint)VertexAttribType.UNSIGNED_BYTE, true, (int)layout.Stride, new IntPtr(attrib.Offset));
                        break;
                    
                    case VertexFormat.ShortNormalized:
                    case VertexFormat.Short2Normalized:
                    case VertexFormat.Short3Normalized:
                    case VertexFormat.Short4Normalized:
                        gl.glVertexAttribPointer(attrib.Index, size, (uint)VertexAttribType.SHORT, true, (int)layout.Stride, new IntPtr(attrib.Offset));
                        break;
                    
                    case VertexFormat.UShortNormalized:
                    case VertexFormat.UShort2Normalized:
                    case VertexFormat.UShort3Normalized:
                    case VertexFormat.UShort4Normalized:
                        gl.glVertexAttribPointer(attrib.Index, size, (uint)VertexAttribType.SHORT, true, (int)layout.Stride, new IntPtr(attrib.Offset));
                        break;
                    
                    case VertexFormat.Half:
                    case VertexFormat.Half2:
                    case VertexFormat.Half3:
                    case VertexFormat.Half4:
                        gl.glVertexAttribPointer(attrib.Index, size, (uint)VertexAttribType.HALF_FLOAT, false, (int)layout.Stride, new IntPtr(attrib.Offset));
                        break;
                    
                    case VertexFormat.Float:
                    case VertexFormat.Float2:
                    case VertexFormat.Float3:
                    case VertexFormat.Float4:
                        gl.glVertexAttribPointer(attrib.Index, size, (uint)VertexAttribType.FLOAT, false, (int)layout.Stride, new IntPtr(attrib.Offset));
                        break;

                    case VertexFormat.Invalid:
                        break;
                    case VertexFormat.Int1010102Normalized:
                        gl.glVertexAttribPointer(attrib.Index, size, (uint)VertexAttribType.INT_2_10_10_10_REV, true, (int)layout.Stride, new IntPtr(attrib.Offset));
                        break;
                    case VertexFormat.UInt1010102Normalized:
                        gl.glVertexAttribPointer(attrib.Index, size, (uint)VertexAttribType.UNSIGNED_INT_2_10_10_10_REV, true, (int)layout.Stride, new IntPtr(attrib.Offset));
                        break;
                    case VertexFormat.UChar4Normalized_BGRA:
                        gl.glVertexAttribPointer(attrib.Index, size, (uint)VertexAttribType.FLOAT, true, (int)layout.Stride, new IntPtr(attrib.Offset));
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
        }
    }
}