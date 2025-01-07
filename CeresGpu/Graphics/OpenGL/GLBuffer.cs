using System;
using System.Runtime.InteropServices;
using CeresGL;

namespace CeresGpu.Graphics.OpenGL
{
    public class GLBuffer<T> where T : unmanaged
    {
        private readonly IGLProvider _glProvider;
        private readonly uint _elementSize;
        
        public uint Handle { get; private set; }
        public uint Count { get; private set; }

        public GLBuffer(IGLProvider glProvider)
        {
            _glProvider = glProvider;
            _elementSize = (uint)Marshal.SizeOf<T>();
            
            Span<uint> buffers = stackalloc uint[1];
            _glProvider.Gl.GenBuffers(1, buffers);
            Handle = buffers[0];
        }

        private void ReleaseUnmanagedResources(GL gl)
        {
            Span<uint> buffers = stackalloc uint[1];
            buffers[0] = Handle;
            gl.DeleteBuffers(1, buffers);
            Handle = 0;
        }

        private void CheckDisposed()
        {
            if (Handle == 0) {
                throw new ObjectDisposedException(null);
            }
        }
        
        public void Dispose()
        {
            CheckDisposed();
            ReleaseUnmanagedResources(_glProvider.Gl);
            GC.SuppressFinalize(this);
        }

        ~GLBuffer() 
        {
            _glProvider.AddFinalizerAction(ReleaseUnmanagedResources);
        }

        //protected abstract BufferUsageARB GetBufferUsage();

        public void Allocate(uint elementCount, BufferUsageARB bufferUsage)
        {
            CheckDisposed();
            GL gl = _glProvider.Gl;
            gl.BindBuffer(BufferTargetARB.ARRAY_BUFFER, Handle);
            gl.BufferData(BufferTargetARB.ARRAY_BUFFER, elementCount * _elementSize, bufferUsage);
            Count = elementCount;
        }
        
        public unsafe void Set(uint offset, ReadOnlySpan<T> elements, uint count)
        {
            CheckDisposed();
            
            if (count > elements.Length) {
                throw new ArgumentOutOfRangeException(nameof(count), "Count is larger than elements length");
            }
            if (offset + count > Count) {
                throw new ArgumentOutOfRangeException(nameof(count), "Buffer does not contain offset + count elements.");
            }

            GL gl = _glProvider.Gl;
            gl.BindBuffer(BufferTargetARB.ARRAY_BUFFER, Handle);
            
            // TODO: Once verified working, put into CeresGL as BufferSubData<T>
            fixed (void* dataPtr = elements) {
                gl.glBufferSubData((uint)BufferTargetARB.ARRAY_BUFFER, (IntPtr)(offset * _elementSize), (IntPtr)(count * _elementSize), (IntPtr)dataPtr);
            }
        }

    }
}