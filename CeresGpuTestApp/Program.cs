﻿// See https://aka.ms/new-console-template for more information

using System.Numerics;
using CeresGLFW;
using CeresGpu;
using CeresGpu.Graphics;
using CeresGpu.Graphics.Shaders;
using CeresGpuTestApp;

using GLFWWindow window = Boot.MakeWindow(800, 600, "CeresGpu Test", false);
using IRenderer renderer = Boot.MakeRenderer(window);

FramebufferPass.RegisterSelf(renderer);

using ShaderManager shaderManager = new ShaderManager(renderer);
using TestRenderer testRenderer = new TestRenderer(renderer, shaderManager);
using FramebufferPass pass = new FramebufferPass(renderer);
pass.Setup(null!, new Vector4(0f, 0.25f, 0.5f, 1f));

while (!window.ShouldClose) {
    using IPass<FramebufferPass> encoder = renderer.CreatePassEncoder([], pass);
    //using IPass pass = renderer.CreateFramebufferPass(LoadAction.Clear, new Vector4(0f, 1f, 1f, 1f), false, 0, 0);
    testRenderer.Draw(encoder);
    //pass.Finish();
    renderer.Present(1f / 60f);
    GLFW.PollEvents();
}
