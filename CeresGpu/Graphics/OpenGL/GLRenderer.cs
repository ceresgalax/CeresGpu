using System;
using System.Collections.Generic;
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
        
        /// <summary>
        /// This is arbitrary, but should always be more than one so that it's easy for users to rat out bugs with
        /// mis-used streaming buffers while using the GL Renderer impl.
        /// </summary>
        public uint WorkingFrameCount => 3;
        
        public uint WorkingFrame { get; private set; }
        public uint UniqueFrameId { get; private set; }
        
        public IGLProvider GLProvider => _context;

        public readonly GLTexture FallbackTexture;
        public readonly GLSampler FallbackSampler;
        
        private readonly DebugCallback? _debugCallback;

        private readonly Dictionary<Type, GLPassBacking> _passBackings = [];

        /// <summary>
        /// Contains the passes that are to be submitted this frame.
        /// </summary>
        private readonly HashSet<GLPass> _passesToSubmit = new();
    
        // NOTE: These are just anchors, and are not to be submitted.
        private readonly GLPassAnchor _encoderListStart = new();
        private readonly GLPassAnchor _encoderListEnd = new();

        private readonly GLSwapchainTarget _swapchainTarget = new();
        private readonly GLFramebuffer _swapchainBlitSrcFramebuffer;
        
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
            
            _swapchainTarget.InnerBuffer = new GLRenderBuffer(this, true, ColorFormat.R8G8B8A8_UNORM, default, 1, 1);
            _swapchainBlitSrcFramebuffer = new GLFramebuffer(this, new GLPassBacking(new RenderPassDefinition {
                ColorAttachments = [
                    new ColorAttachment { Format = ColorFormat.R8G8B8A8_UNORM, LoadAction = LoadAction.DontCare }
                ],
                DepthStencilAttachment = null
            }), [_swapchainTarget], null);
            NewFrame();
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
            
            StaticGLBuffer<T> buffer = new(_context); 
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

        public bool IsPassRegistered<TRenderPass>() where TRenderPass : IRenderPass
        {
            return _passBackings.ContainsKey(typeof(TRenderPass));
        }

        public void RegisterPassType<TRenderPass>(RenderPassDefinition definition) where TRenderPass : IRenderPass
        {
            _passBackings.Add(typeof(TRenderPass), new GLPassBacking(definition));
        }
        
        private GLPassBacking GetPassBackingOrThrow(Type passType)
        {
            if (!_passBackings.TryGetValue(passType, out GLPassBacking? passBacking)) {
                throw new InvalidOperationException($"Pass of type {passType} has not been registered. You must call RegisterPassType first.");
            }
            return passBacking;
        }

        public IPipeline<TShader, TVertexBufferLayout> CreatePipeline<TShader, TVertexBufferLayout>(
            PipelineDefinition definition,
            ReadOnlySpan<Type> compatiblePasses,
            TShader shader,
            TVertexBufferLayout vertexBufferLayout
        )
            where TShader : IShader
            where TVertexBufferLayout : IVertexBufferLayout<TShader>
        {
            return new GLPipeline<TShader, TVertexBufferLayout>(definition, shader, vertexBufferLayout);
        }

        public IFramebuffer CreateFramebuffer<TRenderPass>(ReadOnlySpan<IRenderTarget> colorAttachments, IRenderTarget? depthStencilAttachment) where TRenderPass : IRenderPass
        {
            GLPassBacking passBacking = GetPassBackingOrThrow(typeof(TRenderPass));
            return new GLFramebuffer(this, passBacking, colorAttachments, depthStencilAttachment);
        }

        public IRenderTarget CreateRenderTarget(ColorFormat format, bool matchSwapchainSize, uint width, uint height)
        {
            throw new NotImplementedException();
        }

        public IRenderTarget CreateRenderTarget(DepthStencilFormat format, bool matchSwapchainSize, uint width, uint height)
        {
            throw new NotImplementedException();
        }

        public IRenderTarget GetSwapchainColorTarget()
        {
            return _swapchainTarget;
        }

        public IPass CreatePassEncoder<TRenderPass>(TRenderPass pass, IPass? occursBefore) where TRenderPass : IRenderPass
        {
            if (pass.Framebuffer is not GLFramebuffer framebuffer) {
                throw new ArgumentException("Backend type of pass is not compatible with this renderer.", nameof(pass));
            }
            
            GLPassBacking passBacking = GetPassBackingOrThrow(typeof(TRenderPass));
            GLPass encoder = new(this, passBacking, framebuffer);
            
            if (occursBefore == null) {
                encoder.InsertAfter(_encoderListEnd.Prev!);
            } else {
                encoder.InsertBefore((GLPass)occursBefore);
            }

            _passesToSubmit.Add(encoder);
            return encoder;
        }

        public void Present(float minimumElapsedSeocnds)
        {
            GL gl = GLProvider.Gl;
            
            IGLPass? currentEncoder = _encoderListStart.Next;
            for (int i = 0, ilen = _passesToSubmit.Count; i < ilen; ++i) {
                if (currentEncoder == null) {
                    throw new InvalidOperationException("Unexpected end of command buffer list. (Likely a bug in CeresGpu)");
                }
                
                currentEncoder.ExecuteCommands(gl);
                
                currentEncoder = currentEncoder.Next;
            }
            
            _passesToSubmit.Clear();
            _encoderListStart.ResetAsFront(_encoderListEnd);
            
            // Now blit our fake swapchain renderbuffer onto the actual backbuffer
            gl.Viewport(0, 0, (int)_swapchainTarget.Width, (int)_swapchainTarget.Height);
            gl.Scissor(0, 0, (int)_swapchainTarget.Width, (int)_swapchainTarget.Height);
            gl.BindFramebuffer(FramebufferTarget.READ_FRAMEBUFFER, _swapchainBlitSrcFramebuffer!.FramebufferHandle);
            gl.BindFramebuffer(FramebufferTarget.DRAW_FRAMEBUFFER, 0);
            gl.BlitFramebuffer(
                0, 0, (int)_swapchainTarget.Width, (int)_swapchainTarget.Height,
                0, 0, (int)_swapchainTarget.Width, (int)_swapchainTarget.Height,
                ClearBufferMask.COLOR_BUFFER_BIT,
                BlitFramebufferFilter.NEAREST
            );
            
            _window.SwapBuffers();
            ++UniqueFrameId;
            WorkingFrame = (WorkingFrame + 1) % WorkingFrameCount;
            
            NewFrame();
        }

        private void NewFrame()
        {
            _context.ProcessFinalizerActions();
            
            _encoderListStart.ResetAsFront(_encoderListEnd);
            
            _window.GetFramebufferSize(out int framebufferWidth, out int framebufferHeight);
            if (framebufferWidth != _swapchainTarget.Width || framebufferHeight != _swapchainTarget.Height) {
                _swapchainTarget.InnerBuffer!.Resize((uint)framebufferWidth, (uint)framebufferHeight);
            }
            
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