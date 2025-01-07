using System;
using System.Collections.Generic;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Threading;
using CeresGL;
using CeresGLFW;
using CeresGpu.Graphics.Shaders;
using Metalancer.Graphcs.OpenGL;

namespace CeresGpu.Graphics.OpenGL
{
    public sealed class GLRenderer : IRenderer
    {
        private readonly GLContext _context;
        private readonly GLFWWindow _window;
        
        //private GLPass? _currentPass;
        
        /// <summary>
        /// This is arbitrary, but should always be more than one so that it's easy for users to rat out bugs with
        /// mis-used streaming buffers while using the GL Renderer impl.
        /// </summary>
        public uint WorkingFrameCount => 3;
        
        public uint WorkingFrame { get; private set; }
        public uint UniqueFrameId { get; private set; }
        
        public IGLProvider GLProvider => _context;
        // public GLPass? CurrentPass => _currentPass;

        public readonly GLTexture FallbackTexture;
        public readonly GLSampler FallbackSampler;
        
        private readonly DebugCallback? _debugCallback;

        public GLRenderer(GLFWWindow window, bool isDebugContext = false)
        {
            GL gl = new();
            gl.Init(new GlfwGLLoader());
            _context = new(gl, Thread.CurrentThread);
            _window = window;

            Span<int> pMajorVersion = stackalloc int[1];
            Span<int> pMinorVersion = stackalloc int[1];
            Span<int> pContextFlags = stackalloc int[1];
            gl.GetIntegerv(GetPName.MAJOR_VERSION, pMajorVersion);
            gl.GetIntegerv(GetPName.MINOR_VERSION, pMinorVersion);
            gl.GetIntegerv(GetPName.CONTEXT_FLAGS, pContextFlags);

            int majorVersion = pMajorVersion[0];
            int minorVersion = pMinorVersion[0];
            int flags = pContextFlags[0];
            
            Console.WriteLine($"OpenGLRenderer: OpenGL version {majorVersion}.{minorVersion}, context flags: {flags}");
            
            // TODO: Fix parameter validation in gl.GetIntegerv
            
            if (isDebugContext) {
                _debugCallback = HandleDebugMessage;
                gl.glDebugMessageCallback(Marshal.GetFunctionPointerForDelegate(_debugCallback), IntPtr.Zero);
            }
            
            gl.Enable(EnableCap.SCISSOR_TEST);

            FallbackTexture = (GLTexture)RendererUtil.CreateFallbackTexture(this);
            FallbackSampler = (GLSampler)CreateSampler(default);
        }

        private delegate void DebugCallback(DebugSource source, DebugType type, uint id, DebugSeverity severity, uint length, IntPtr message, IntPtr userParam); 

        private void HandleDebugMessage(DebugSource source, DebugType type, uint id, DebugSeverity severity, uint length, IntPtr message, IntPtr userParam)
        {
            string? messageStr = Marshal.PtrToStringAnsi(message) ?? "";            
            Console.WriteLine($"[Source:{source}][Type: {type}]: {id}: {severity}: {messageStr}");
        }
        
        public IStaticBuffer<T> CreateStaticBuffer<T>(int elementCount) where T : unmanaged
        {
            if (elementCount < 0) {
                throw new ArgumentOutOfRangeException(nameof(elementCount));
            }
            
            StaticGLBuffer<T> buffer = new StaticGLBuffer<T>(_context); 
            buffer.Allocate((uint)elementCount);
            return buffer;
        }

        public IStreamingBuffer<T> CreateStreamingBuffer<T>(int elementCount) where T : unmanaged
        {
            if (elementCount < 0) {
                throw new ArgumentOutOfRangeException(nameof(elementCount));
            }

            IStreamingBuffer<T> buffer;
            #if DEBUG
            buffer = new DebugStreamingGLBuffer<T>(this);
            #else
            buffer = new StreamingGLBuffer<T>(this);
            #endif
            
            buffer.Allocate((uint)elementCount);
            return buffer;
        }

        public ITexture CreateTexture()
        {
            return new GLTexture(_context);
        }

        public ISampler CreateSampler(in SamplerDescription description)
        {
            return new GLSampler(_context, in description);
        }

        public IShaderBacking CreateShaderBacking(IShader shader)
        {
            return new GLShaderBacking(_context, shader);
        }

        public IShaderInstanceBacking CreateShaderInstanceBacking(IShader shader)
        {
            return new GLShaderInstanceBacking(this, shader);
        }

        public IDescriptorSet CreateDescriptorSet(IShaderBacking shader, ShaderStage stage, int index, in DescriptorSetCreationHints hints)
        {
            return new GLDescriptorSet(this, in hints);
        }

        public void RegisterPassType<TRenderPass>(RenderPassDefinition definition) where TRenderPass : IRenderPass
        {
            throw new NotImplementedException();
        }

        public IPipeline<TRenderPass, TShader, TVertexBufferLayout> CreatePipeline<TRenderPass, TShader, TVertexBufferLayout>(
            PipelineDefinition definition,
            TShader shader,
            TVertexBufferLayout vertexBufferLayout
        )
            where TRenderPass : IRenderPass
            where TShader : IShader
            where TVertexBufferLayout : IVertexBufferLayout<TShader>
        {
            return new GLPipeline<TRenderPass, TShader, TVertexBufferLayout>(definition, shader, vertexBufferLayout);
        }

        public IMutableFramebuffer CreateFramebuffer<TRenderPass>() where TRenderPass : IRenderPass
        {
            throw new NotImplementedException();
        }

        public IRenderTarget CreateRenderTarget(ColorFormat format, uint width, uint height)
        {
            throw new NotImplementedException();
        }

        public IRenderTarget CreateRenderTarget(DepthStencilFormat format, uint width, uint height)
        {
            throw new NotImplementedException();
        }

        public IRenderTarget GetSwapchainColorTarget()
        {
            throw new NotImplementedException();
        }

        public IPass<TRenderPass> CreatePassEncoder<TRenderPass>(ReadOnlySpan<IPass> dependentPasses, TRenderPass pass) where TRenderPass : IRenderPass
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// To be called by GLPass when encoding has finished.
        /// </summary>
        public void FinishPass()
        {
            //_currentPass = null;
        }

        // private GLPass SetCurrentPass(GLPass pass)
        // {
        //     _currentPass?.Finish();
        //     _currentPass = pass;
        //     return pass;
        // }

        // public IPass CreateFramebufferPass(LoadAction colorLoadAction, Vector4 clearColor, bool withDepthStencil, double depthClearValue, uint stencilClearValue)
        // {
        //     _window.GetFramebufferSize(out int width, out int height);
        //     GLPass pass = SetCurrentPass(new GLPass(this, (uint)width, (uint)height));
        //     
        //     // TODO: Need to better clarify which methods CeresGPU API allows to be performed outside of the main thread.
        //     // If the CeresGPU API allows CreateFramebuffer pass to be called outside of the main thread, We will need
        //     // to serially queue the GL commands here to the GL thread.
        //     GL gl = GLProvider.Gl;
        //     gl.Viewport(0, 0, width, height);
        //
        //     ClearBufferMask clearMask = 0;
        //     if (colorLoadAction == LoadAction.Clear) {
        //         gl.ClearColor(clearColor.X, clearColor.Y, clearColor.Z, clearColor.W);
        //         clearMask |= ClearBufferMask.COLOR_BUFFER_BIT;
        //     }
        //     if (withDepthStencil) {
        //         gl.ClearDepth(depthClearValue);
        //         gl.ClearStencil((int)stencilClearValue);
        //         clearMask |= ClearBufferMask.DEPTH_BUFFER_BIT | ClearBufferMask.STENCIL_BUFFER_BIT;
        //     }
        //
        //     pass.SetScissor(new ScissorRect(0, 0, (uint)width, (uint)height));
        //
        //     if (clearMask != 0) {
        //         gl.Clear(clearMask);    
        //     }
        //     
        //     return pass;
        // }
        //
        // public IPass CreatePass(ReadOnlySpan<ColorAttachment> colorAttachments, ITexture? depthStencilAttachment, LoadAction depthLoadAction
        //     , double depthClearValue, LoadAction stencilLoadAction, uint stenclClearValue)
        // {
        //     throw new NotImplementedException();
        // }

        public void Present(float minimumElapsedSeocnds)
        {
            _window.SwapBuffers();
            ++UniqueFrameId;
            WorkingFrame = (WorkingFrame + 1) % WorkingFrameCount;
            
            _context.ProcessFinalizerActions();
        }

        public void GetDiagnosticInfo(IList<(string key, object value)> entries)
        {
        }

        public void Dispose()
        {
            // TODO
        }
    }
}