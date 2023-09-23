// See https://aka.ms/new-console-template for more information

using System.Numerics;
using CeresGLFW;
using CeresGpu;
using CeresGpu.Graphics;
using CeresGpu.Graphics.Metal;
using CeresGpu.Graphics.Shaders;
using CeresGpuTestApp;

using GLFWWindow window = Boot.MakeWindow(800, 600, "CeresGpu Test", false);
using IRenderer renderer = Boot.MakeRenderer(window);
using ShaderManager shaderManager = new ShaderManager(renderer);
using TestRenderer testRenderer = new TestRenderer(renderer, shaderManager);

while (!window.ShouldClose)
{
    using IPass pass = renderer.CreateFramebufferPass(true, new Vector4(0f, 1f, 1f, 1f));
    testRenderer.Draw(pass);
    pass.Finish();
    renderer.Present(1f / 60f);
    GLFW.PollEvents();
}
