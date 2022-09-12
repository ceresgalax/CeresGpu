using System;
using CeresGL;
using CeresGLFW;

namespace Metalancer.Graphcs.OpenGL
{
    public class GlfwGLLoader : ILoader
    {
        public T? GetProc<T>(string name) where T : Delegate
        {
            return GLFW.GetProc<T>(name);
        }
    }
}