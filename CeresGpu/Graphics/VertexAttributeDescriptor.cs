using System.Reflection;

namespace CeresGpu.Graphics
{
    public struct VertexAttributeDescriptor
    {
        public uint Index;
        public VertexFormat Format;
        public uint Offset;
        public uint BufferIndex;

        /// <summary>
        /// Hint for the purpose of this attribute. For example, this may specify that the attribute is used for
        /// position, color, uvs, color, etc. This may be used for generic shader tools to preview shaders and construct
        /// vertex data to render a preview of the shader with a preview model (cube, sphere, teapot, etc..)
        ///
        /// CeresGpu doesn't use this attribute for anything itself. 
        /// </summary>
        public string Hint;

        /// <summary>
        /// Name of the attribute. This may be used for displaying information about the shader to the user.
        /// This may be used for generic shader tools to show attribute names.
        ///
        /// CeresGpu doesn't use this attribute for anything itself. 
        /// </summary>
        public string Name;
    }
}