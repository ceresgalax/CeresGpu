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
using TestRenderer testRenderer = new TestRenderer(renderer, shaderManager);
using FramebufferPass pass = new FramebufferPass(renderer);
//using IRenderTarget colorTarget = renderer.CreateRenderTarget(ColorFormat.R8G8B8A8_UNORM, 512, 512); 
//pass.Setup(colorTarget, new Vector4(0f, 1f, 1f, 1f));

pass.Setup(renderer.GetSwapchainColorTarget(), new Vector4(0f, 1f, 1f, 1f));

while (!window.ShouldClose) {
    IPass<FramebufferPass> encoder = renderer.CreatePassEncoder(pass);
    //using IPass pass = renderer.CreateFramebufferPass(LoadAction.Clear, new Vector4(0f, 1f, 1f, 1f), false, 0, 0);
    testRenderer.Draw(encoder);
    //pass.Finish();
    renderer.Present(1f / 60f);
    GLFW.PollEvents();
}
