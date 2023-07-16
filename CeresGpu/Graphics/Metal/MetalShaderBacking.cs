using System;
using System.IO;
using CeresGpu.Graphics.Shaders;
using CeresGpu.MetalBinding;

namespace CeresGpu.Graphics.Metal
{
    public sealed class MetalShaderBacking : IShaderBacking
    {
        private readonly MetalRenderer _renderer;
        
        public IntPtr VertexFunction { get; private set; }
        public IntPtr FragmentFunction { get; private set; }

        public MetalShaderBacking(MetalRenderer renderer, IShader shader)
        {
            _renderer = renderer;
            
            // Load the shader from resources
            Type shaderType = shader.GetType();

            IntPtr vertLibrary = GetLibrary(shader, ".vert.metal");
            if (vertLibrary == IntPtr.Zero) {
                return;
            }
            try {
                IntPtr fragmentLibrary = MetalApi.metalbinding_new_library(_renderer.Context, GetSource(shader, ".frag.metal"));
                if (fragmentLibrary == IntPtr.Zero) {
                    return;
                }
                try {
                    VertexFunction = MetalApi.metalbinding_new_function(vertLibrary, "main0");
                    FragmentFunction = MetalApi.metalbinding_new_function(fragmentLibrary, "main0");
                } finally {
                    MetalApi.metalbinding_release_library(fragmentLibrary);
                }
            } finally {
                MetalApi.metalbinding_release_library(vertLibrary);
            }
        }

        private IntPtr GetLibrary(IShader shader, string name)
        {
            IntPtr library = MetalApi.metalbinding_new_library(_renderer.Context, GetSource(shader, name));
            if (library == IntPtr.Zero) {
                Console.Error.WriteLine("Error creating shader library from source: }" + _renderer.GetLastError());
            }
            return library;
        }

        private string GetSource(IShader shader, string name)
        {
            Stream? stream = shader.GetShaderResource(name);
            if (stream == null) {
                throw new InvalidOperationException($"Cannot find Metal shader source resource for {name}");
            }
            using StreamReader streamReader = new StreamReader(stream);
            return streamReader.ReadToEnd();
        }

        private void ReleaseUnmanagedResources()
        {
            if (FragmentFunction != IntPtr.Zero) {
                MetalApi.metalbinding_release_function(FragmentFunction);
                FragmentFunction = IntPtr.Zero;
            }
            if (VertexFunction != IntPtr.Zero) {
                MetalApi.metalbinding_release_function(VertexFunction);
                VertexFunction = IntPtr.Zero;
            }
        }

        public void Dispose()
        {
            ReleaseUnmanagedResources();
            GC.SuppressFinalize(this);
        }

        ~MetalShaderBacking() {
            ReleaseUnmanagedResources();
        }
    }
}