using System;
using CeresGL;

namespace CeresGpu.Graphics.OpenGL
{
    public interface IGLProvider
    {
        GL Gl { get; }
        void AddFinalizerAction(Action<GL> action);
        void DoOnContextThread(Action<GL> action);
    }
}