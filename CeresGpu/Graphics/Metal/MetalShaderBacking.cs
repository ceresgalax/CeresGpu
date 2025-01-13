using System;
using System.Collections.Generic;
using System.IO;
using CeresGpu.Graphics.Shaders;
using CeresGpu.MetalBinding;

namespace CeresGpu.Graphics.Metal
{
    public sealed class MetalShaderBacking : IShaderBacking
    {
        public struct ArgumentBufferInfo
        {
            public required ShaderStage Stage;
            public required uint FunctionIndex;
        }
        
        private readonly MetalRenderer _renderer;
        
        public IntPtr VertexFunction { get; private set; }
        public IntPtr FragmentFunction { get; private set; }
        
        public ArgumentBufferInfo[] ArgumentBufferDetails { get; }

        public MetalShaderBacking(MetalRenderer renderer, IShader shader)
        {
            _renderer = renderer;
            
            // Load the shader from resources
            IntPtr vertLibrary = GetLibrary(shader, ".vert.metal");
            if (vertLibrary == IntPtr.Zero) {
                throw new InvalidOperationException();
            }
            try {
                IntPtr fragmentLibrary = MetalApi.metalbinding_new_library(_renderer.Context, GetSource(shader, ".frag.metal"));
                if (fragmentLibrary == IntPtr.Zero) {
                    throw new InvalidOperationException();
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
            
            List<ArgumentBufferInfo> argumentBuffersDetails = [];
            foreach (DescriptorInfo info in shader.GetDescriptors()) {
                MetalDescriptorBindingInfo metalBinding = (MetalDescriptorBindingInfo)info.Binding;
                while (metalBinding.AbstractedBufferIndex >= argumentBuffersDetails.Count) {
                    argumentBuffersDetails.Add(default);
                }

                argumentBuffersDetails[metalBinding.AbstractedBufferIndex] = new ArgumentBufferInfo {
                    Stage = metalBinding.Stage,
                    FunctionIndex = metalBinding.FunctionArgumentBufferIndex
                };
            }
            ArgumentBufferDetails = argumentBuffersDetails.ToArray();
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