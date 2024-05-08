using System;
using System.Collections.Generic;
using System.Numerics;
using System.Threading;
using CeresGL;
using CeresGLFW;
using CeresGpu.Graphics.Shaders;
using Metalancer.Graphcs.OpenGL;

namespace CeresGpu.Graphics.OpenGL
{
    public class OpenGLRenderer : IRenderer
    {
        private readonly GLContext _context;
        private readonly GLFWWindow _window;
        
        private GLPass? _currentPass;

        public uint UniqueFrameId { get; private set; }
        
        public IGLProvider GLProvider => _context;
        public GLPass? CurrentPass => _currentPass;

        public OpenGLRenderer(GLFWWindow window)
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
            
            // Get supported shader binary formats
            int numShaderBinaryFormats = 0;
            unsafe {
                gl.glGetIntegerv((uint)GetPName.NUM_SHADER_BINARY_FORMATS, (IntPtr)(&numShaderBinaryFormats));
            }
            
            int[] formats = new int[numShaderBinaryFormats];
            unsafe {
                fixed (int* pFormats = formats) {
                    gl.glGetIntegerv((uint)GetPName.SHADER_BINARY_FORMATS, (IntPtr)(pFormats));
                }
            }

            bool supportsSpirV = false;
            
            foreach(int formatInt in formats) {
                ShaderBinaryFormat format = (ShaderBinaryFormat)formatInt;
                Console.WriteLine($"OpenGLRenderer: Supports Shader Binary Format {Enum.GetName(format)}");
                if (format == ShaderBinaryFormat.SHADER_BINARY_FORMAT_SPIR_V)
                {
                    supportsSpirV = true;
                } 
            }

            if (!supportsSpirV) {
                throw new InvalidOperationException("This device does not support SPIR-V shader binaries. As OpenGL does not support selecting GPUs, please set a different GPU for this app in your GPU settings.");
            }
            
            gl.Enable(EnableCap.SCISSOR_TEST);
        }
        
        public IBuffer<T> CreateStaticBuffer<T>(int elementCount) where T : unmanaged
        {
            if (elementCount < 0) {
                throw new ArgumentOutOfRangeException(nameof(elementCount));
            }
            
            StaticGLBuffer<T> buffer = new StaticGLBuffer<T>(_context); 
            buffer.Allocate((uint)elementCount);
            return buffer;
        }

        public IBuffer<T> CreateStreamingBuffer<T>(int elementCount) where T : unmanaged
        {
            if (elementCount < 0) {
                throw new ArgumentOutOfRangeException(nameof(elementCount));
            }
            
            StreamingGLBuffer<T> buffer = new StreamingGLBuffer<T>(_context);
            buffer.Allocate((uint)elementCount);
            return buffer;
        }

        public ITexture CreateTexture()
        {
            return new GLTexture(_context);
        }

        public IShaderBacking CreateShaderBacking(IShader shader)
        {
            return new GLShaderBacking(_context, shader);
        }

        public IShaderInstanceBacking CreateShaderInstanceBacking(int vertexBufferCountHint, IShader shader)
        {
            return new GLShaderInstanceBacking(_context, vertexBufferCountHint, shader);
        }

        public IDescriptorSet CreateDescriptorSet(IShaderBacking shader, ShaderStage stage, int index, in DescriptorSetCreationHints hints)
        {
            return new GLDescriptorSet(_context, in hints);
        }

        public IPipeline<ShaderT> CreatePipeline<ShaderT>(PipelineDefinition definition, ShaderT shader) where ShaderT : IShader
        {
            return new OpenGLPipeline<ShaderT>(definition, shader);
        }
        
        /// <summary>
        /// To be called by GLPass when encoding has finished.
        /// </summary>
        public void FinishPass()
        {
            _currentPass = null;
        }

        private GLPass SetCurrentPass(GLPass pass)
        {
            _currentPass?.Finish();
            _currentPass = pass;
            return pass;
        }

        public IPass CreateFramebufferPass(LoadAction colorLoadAction, Vector4 clearColor, bool withDepthStencil, double depthClearValue, uint stencilClearValue)
        {
            _window.GetFramebufferSize(out int width, out int height);
            GLPass pass = SetCurrentPass(new GLPass(this, (uint)width, (uint)height));
            
            // TODO: Need to better clarify which methods CeresGPU API allows to be performed outside of the main thread.
            // If the CeresGPU API allows CreateFramebuffer pass to be called outside of the main thread, We will need
            // to serially queue the GL commands here to the GL thread.
            GL gl = GLProvider.Gl;
            gl.Viewport(0, 0, width, height);

            ClearBufferMask clearMask = 0;
            if (colorLoadAction == LoadAction.Clear) {
                gl.ClearColor(clearColor.X, clearColor.Y, clearColor.Z, clearColor.W);
                clearMask |= ClearBufferMask.COLOR_BUFFER_BIT;
            }
            if (withDepthStencil) {
                gl.ClearDepth(depthClearValue);
                gl.ClearStencil((int)stencilClearValue);
                clearMask |= ClearBufferMask.DEPTH_BUFFER_BIT | ClearBufferMask.STENCIL_BUFFER_BIT;
            }

            pass.SetScissor(new ScissorRect(0, 0, (uint)width, (uint)height));

            if (clearMask != 0) {
                gl.Clear(clearMask);    
            }
            
            return pass;
        }

        public IPass CreatePass(ReadOnlySpan<ColorAttachment> colorAttachments, ITexture? depthStencilAttachment, LoadAction depthLoadAction
            , double depthClearValue, LoadAction stencilLoadAction, uint stenclClearValue)
        {
            throw new NotImplementedException();
        }

        public void Present(float minimumElapsedSeocnds)
        {
            _window.SwapBuffers();
            ++UniqueFrameId;
            
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