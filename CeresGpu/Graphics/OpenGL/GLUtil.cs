using System;
using System.Numerics;
using CeresGL;

namespace Metalancer.Graphics.OpenGL
{
    // TODO: Add these to CeresGL if they are useful.
    public static class MetaGLUtil
    {
        public static unsafe void UniformMatrix4fv(this GL gl, int location, bool transpose, Matrix4x4 value)
        {
            // Note: This assumes that System.Numerics.Matrix4x4's layout stays constant. It better or I'll be pissed!!
            gl.UniformMatrix4fv(location, 1, transpose, new Span<float>(&value, 16));
        }

        public static void Uniform2f(this GL gl, int location, Vector2 value)
        {
            gl.Uniform2f(location, value.X, value.Y);
        }
        
        public static void Uniform4f(this GL gl, int location, Vector4 value)
        {
            gl.Uniform4f(location, value.X, value.Y, value.Z, value.W);
        }
    }
}