// See https://aka.ms/new-console-template for more information

using System.Numerics;
using CeresGLFW;
using CeresGpu;
using CeresGpu.Graphics;
using CeresGpu.Graphics.Shaders;
using CeresGpuTestApp;

GLFWWindowFactory windowFactory = new GLFWWindowFactory(Boot.MakeBaseWindowHints(), 800, 600, "CeresGpu Test");
using IRenderer renderer = Boot.MakeRenderer(windowFactory);
using GLFWWindow window = windowFactory.GetOrCreateWindow(); 

FramebufferPass.RegisterSelf(renderer);

using ShaderManager shaderManager = new ShaderManager(renderer);
using TestRenderer testRenderer = new TestRenderer(renderer, shaderManager, [typeof(FramebufferPass)]);
using FramebufferPass pass = new FramebufferPass(renderer, renderer.GetSwapchainColorTarget());
pass.SetClearColor(new Vector4(0f, 1f, 1f, 1f));

while (!window.ShouldClose) {
    IPass encoder = renderer.CreatePassEncoder(pass);
    testRenderer.Draw(encoder);
    renderer.Present(1f / 60f);
    GLFW.PollEvents();
}
