using System;
using CeresGL;

namespace Metalancer.Graphics.OpenGL
{
    public interface IGLProvider
    {
        GL Gl { get; }
        void AddFinalizerAction(Action<GL> action);
    }
}