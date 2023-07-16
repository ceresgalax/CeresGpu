
using System;

namespace CeresGpu.Graphics
{
    public struct VertexBufferLayout
    {
        public VertexStepFunction StepFunction;
        public uint Stride;

        /// <summary>
        /// The Type of structure which has been generated for this vertex buffer this shader accepts.
        /// Intended for use by shader introspection tools.
        ///
        /// Not used by CeresGpu itself. 
        /// </summary>
        public Type? BufferType;
    }
}