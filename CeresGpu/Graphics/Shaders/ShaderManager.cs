using System;
using System.Collections.Generic;
using System.Reflection;

namespace CeresGpu.Graphics.Shaders
{
    public interface IShaderVisitor
    {
        void Visit<TShader>(TShader shader) where TShader : IShader;
    }
    
    public sealed class ShaderManager : IDisposable
    {
        private record struct ShaderEntry(IShader Shader, MethodInfo VisitMethod);
        
        private readonly IRenderer _renderer;
        private readonly Dictionary<Type, ShaderEntry> _shaderMap = new();
        
        public ShaderManager(IRenderer renderer)
        {
            _renderer = renderer;
        }

        public T GetShader<T>() where T : IShader, new()
        {
            return (T)(GetShader(typeof(T)).Shader);
        }

        private ShaderEntry GetShader(Type type)
        {
            if (!_shaderMap.TryGetValue(type, out ShaderEntry entry)) {
                IShader shader = (IShader)(Activator.CreateInstance(type) ?? throw new InvalidOperationException());
                shader.Prime(_renderer);
                shader.Backing = _renderer.CreateShaderBacking(shader);
                
                MethodInfo info = typeof(IShaderVisitor).GetMethod(nameof(IShaderVisitor.Visit))!;
                
                entry = new ShaderEntry(shader, info.MakeGenericMethod(type));
                _shaderMap.Add(type, entry);
            }

            return entry;
        }
        
        public void Accept<TVisitor>(Type shaderType, ref TVisitor visitor) where TVisitor : IShaderVisitor
        {
            ShaderEntry entry = GetShader(shaderType);
            object obj = visitor;
            entry.VisitMethod.Invoke(obj, [entry.Shader]);
            visitor = (TVisitor)obj;
        }

        public void Dispose()
        {
            foreach (ShaderEntry entry in _shaderMap.Values) {
                entry.Shader.Dispose();
            }
            _shaderMap.Clear();
        }
    }
}