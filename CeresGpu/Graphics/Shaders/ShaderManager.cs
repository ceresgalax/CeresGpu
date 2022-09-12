using System;
using System.Collections.Generic;

namespace Metalancer.Graphics.Shaders
{
    public sealed class ShaderManager : IDisposable
    {
        private readonly IRenderer _renderer;
        private readonly Dictionary<Type, IShader> _shaderMap = new();

        public ShaderManager(IRenderer renderer)
        {
            _renderer = renderer;
        }

        public T GetShader<T>() where T : IShader, new()
        {
            IShader? shader;
            if (!_shaderMap.TryGetValue(typeof(T), out shader)) {
                shader = new T();
                shader.Backing = _renderer.CreateShaderBacking(shader);
                _shaderMap[typeof(T)] = shader;
            }

            return (T)shader;
        }

        public void Dispose()
        {
            foreach (IShader shader in _shaderMap.Values) {
                shader.Dispose();
            }
            _shaderMap.Clear();
        }

        // public static GLShader CompileShader(IShader shader, IGLProvider glProvider, bool loadSourceFromPath = false)
        // {
        //     GL gl = glProvider.Gl;
        //
        //     uint program = gl.CreateProgram();
        //
        //     foreach ((ShaderType type, string source, string path) in shader.GetSources()) {
        //         uint handle = gl.CreateShader(type);
        //         string sourceToLoad = source;
        //
        //         if (loadSourceFromPath) {
        //             try {
        //                 sourceToLoad = File.ReadAllText(path);
        //             } catch (Exception e) {
        //                 Console.Error.WriteLine($"Failed to load shader source from path {path}: {e}");
        //             }
        //         }
        //
        //         gl.ShaderSource(handle, sourceToLoad);
        //         gl.CompileShader(handle);
        //         Console.WriteLine($"{type} Log (for {path}): {GLUtil.GetShaderInfoLog(gl, handle)}");
        //         gl.AttachShader(program, handle);
        //         gl.DeleteShader(handle);
        //     }
        //
        //     gl.LinkProgram(program);
        //     Console.WriteLine($"Program Log: {GLUtil.GetProgramInfoLog(gl, program)}");
        //
        //     return new GLShader(glProvider, program);
        // }
    }
}