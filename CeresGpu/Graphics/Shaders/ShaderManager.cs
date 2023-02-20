using System;
using System.Collections.Generic;

namespace CeresGpu.Graphics.Shaders
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
    }
}