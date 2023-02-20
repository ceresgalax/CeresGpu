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

        public IPass CreateFramebufferPass(bool clear, Vector4 clearColor)
        {
            _window.GetFramebufferSize(out int width, out int height);
            GLPass pass = SetCurrentPass(new GLPass(this, (uint)width, (uint)height));
            pass.SetScissor(new ScissorRect(0, 0, (uint)width, (uint)height));
            if (clear) {
                Viewport viewport = new(0, 0, (uint)width, (uint)height);
                pass.Clear(viewport, clearColor);
            }
            return pass;
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