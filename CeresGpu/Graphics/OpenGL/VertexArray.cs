using System;
using CeresGL;

namespace CeresGpu.Graphics.OpenGL
{
    public sealed class VertexArray : IVertexDescriptor, IDisposable
    {
        private IGLProvider _glProvider;
        private uint _handle;

        public uint Handle => _handle;

        public VertexArray(IGLProvider glProvider)
        {
            _glProvider = glProvider;
            Span<uint> handleBuffer = stackalloc uint[1];
            glProvider.Gl.GenVertexArrays(1, handleBuffer);
            _handle = handleBuffer[0];
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
    }
}