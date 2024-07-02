using System;
using System.IO;
using CeresGL;
using CeresGpu.Graphics.Shaders;

namespace CeresGpu.Graphics.OpenGL
{
    public sealed class GLShaderBacking : IShaderBacking
    {
        private readonly IGLProvider _provider;
        private readonly uint _program;

        public uint Program => _program;
        
        public GLShaderBacking(IGLProvider glProvider, IShader shader)
        {
            _provider = glProvider;
            GL gl = glProvider.Gl;
            
            _program = gl.CreateProgram();

            uint vertShader = gl.CreateShader(ShaderType.VERTEX_SHADER);
            try {
                uint fragShader = gl.CreateShader(ShaderType.FRAGMENT_SHADER);
                try {
                    SetShader(gl, vertShader, shader, ".vert_gl.glsl");
                    SetShader(gl, fragShader, shader, ".frag_gl.glsl");
                    gl.AttachShader(_program, vertShader);
                    gl.AttachShader(_program, fragShader);
                    gl.LinkProgram(_program);
                    Console.WriteLine($"Program Log: {GLUtil.GetProgramInfoLog(gl, _program)}");
                } finally {
                    gl.DeleteShader(fragShader);
                }
            } finally {
                gl.DeleteShader(vertShader);
            }
        }

        private void SetShader(GL gl, uint handle, IShader shader, string postfix)
        {
            //Span<uint> shaders = stackalloc uint[1] { handle };
            byte[] spirv = GetSource(shader, postfix);
            
            gl.ShaderSource(handle, spirv);
            gl.CompileShader(handle);
            
            //gl.ShaderBinary(1, shaders, ShaderBinaryFormat.SHADER_BINARY_FORMAT_SPIR_V, spirv, spirv.Length);
            //gl.SpecializeShader(handle, "main", 0, null, null);
            Console.WriteLine($"Shader Log: {GLUtil.GetShaderInfoLog(gl, handle)}"); // TODO: Needs more info
        }
        
        private byte[] GetSource(IShader shader, string postfix)
        {
            using Stream? stream = shader.GetShaderResource(postfix);
            if (stream == null) {
                throw new InvalidOperationException($"Cannot find resource with {postfix}");
            }
            long len = stream.Length;
            byte[] spirv = new byte[len];
            int offset = 0;
            while (true) {
                int read = stream.Read(spirv, offset, (int)len - offset);
                offset += read;
                if (read == 0) {
                    if (offset != len) {
                        throw new InvalidOperationException("Internal error. Fewer bytes than stream.Length read.");
                    }
                    break;
                }
            } 
            return spirv;
        }

        private void ReleaseUnmanagedResources()
        {
            if (_program != 0) {
                _provider.AddFinalizerAction(gl => gl.glDeleteProgram(_program));
            }
        }

        public void Dispose()
        {
            ReleaseUnmanagedResources();
            GC.SuppressFinalize(this);
        }

        ~GLShaderBacking() {
            ReleaseUnmanagedResources();
        }
    }
}