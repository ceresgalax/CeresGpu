using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
using CeresGLFW;
using CeresGpu.Graphics.Shaders;
using CeresGpu.MetalBinding;

namespace CeresGpu.Graphics.Metal
{
    public sealed class MetalRenderer : IRenderer
    {
        public readonly IntPtr Context;
        private readonly GLFWWindow _glfwWindow;
        //private IntPtr _currentFrameCommandBuffer;

        public readonly MetalTexture FallbackTexture;
        public readonly MetalSampler FallbackSampler;
        
        public int FrameCount => 3; 
        public int WorkingFrame { get; private set; }
        public uint UniqueFrameId { get; private set; }

        private bool _hasAcquiredDrawable;
        
        private Dictionary<Type, MetalPassBacking> _passBackings = [];
        
        /// <summary>
        /// Contains the passes that are to be submitted this frame.
        /// </summary>
        private readonly HashSet<MetalPass> _passesToSubmit = new();
    
        // NOTE: These are just anchors, and are not to be submitted.
        private readonly MetalPassAnchor _encoderListStart = new();
        private readonly MetalPassAnchor _encoderListEnd = new();

        private readonly MetalSwapchainTarget _swapchainTarget = new();

        public MetalRenderer(IntPtr window, GLFWWindow glfwWindow)
        {
            _glfwWindow = glfwWindow;
            Context = MetalApi.metalbinding_create(window, (uint)FrameCount);
            MetalApi.metalbinding_arp_drain(Context);

            FallbackTexture = (MetalTexture)RendererUtil.CreateFallbackTexture(this);
            FallbackSampler = (MetalSampler)CreateSampler(default);

            glfwWindow.GetFramebufferSize(out int framebufferWidth, out int framebufferHeight);
            _swapchainTarget.Width = (uint)framebufferWidth;
            _swapchainTarget.Height = (uint)framebufferHeight;
            
            NewFrame();
        }

        public void Dispose()
        {
            // TODO: More things we have to release? Also shouldn't this have a finalizer too?
            MetalApi.metalbinding_arp_deinit(Context);
            MetalApi.metalbinding_destroy(Context);
        }

        public IStaticBuffer<T> CreateStaticBuffer<T>(int elementCount) where T : unmanaged
        {
            if (elementCount < 0) {
                throw new ArgumentOutOfRangeException(nameof(elementCount));
            }
            
            var buffer = new MetalStaticBuffer<T>(this);
            buffer.Allocate((uint)elementCount);
            return buffer;
        }

        public IStreamingBuffer<T> CreateStreamingBuffer<T>(int elementCount) where T : unmanaged
        {
            if (elementCount < 0) {
                throw new ArgumentOutOfRangeException(nameof(elementCount));
            }
            
            var buffer = new MetalStreamingBuffer<T>(this);
            buffer.Allocate((uint)elementCount);
            return buffer;
        }

        public ITexture CreateTexture()
        {
            return new MetalTexture(this);
        }

        public ISampler CreateSampler(in SamplerDescription description)
        {
            return new MetalSampler(this, in description);
        }

        public IShaderBacking CreateShaderBacking(IShader shader)
        {
            return new MetalShaderBacking(this, shader);
        }

        public IShaderInstanceBacking CreateShaderInstanceBacking(IShader shader)
        {
            return new MetalShaderInstanceBacking(this, (MetalShaderBacking)shader.Backing!);
        }

        public bool IsPassRegistered<TRenderPass>() where TRenderPass : IRenderPass
        {
            return _passBackings.ContainsKey(typeof(TRenderPass));
        }

        public void RegisterPassType<TRenderPass>(RenderPassDefinition definition) where TRenderPass : IRenderPass
        {
            _passBackings.Add(typeof(TRenderPass), new MetalPassBacking(definition));
        }
        
        private MetalPassBacking GetPassBackingOrThrow(Type passType)
        {
            if (!_passBackings.TryGetValue(passType, out MetalPassBacking? passBacking)) {
                throw new InvalidOperationException($"Pass of type {passType} has not been registered. You must call RegisterPassType first.");
            }
            return passBacking;
        }

        public IPipeline<TShader, TVertexBufferLayout> CreatePipeline<TShader, TVertexBufferLayout>(
            PipelineDefinition definition,
            ReadOnlySpan<Type> supportedRenderPasses,
            TShader shader,
            TVertexBufferLayout layout
        )
            where TShader : IShader
            where TVertexBufferLayout : IVertexBufferLayout<TShader>
        {
            return new MetalPipeline<TShader, TVertexBufferLayout>(this, definition, shader, layout);
        }

        public IFramebuffer CreateFramebuffer<TRenderPass>(ReadOnlySpan<IRenderTarget> colorAttachments, IRenderTarget? depthStencilAttachment) where TRenderPass : IRenderPass
        {
            MetalPassBacking passBacking = GetPassBackingOrThrow(typeof(TRenderPass));
            return new MetalFramebuffer(passBacking, colorAttachments, depthStencilAttachment);
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
            // TODO: Maybe we can be smarter and only acquire the drawable if the framebuffer uses a swapchain target?
            // We need to acquire the drawable as late as possible.
            EnsureDrawableAcquired();

            if (pass.Framebuffer is not MetalFramebuffer framebuffer) {
                throw new ArgumentOutOfRangeException(nameof(pass), "Incompatible framebuffer");
            }
            
            MetalPassBacking passBacking = GetPassBackingOrThrow(typeof(TRenderPass));
            MetalPass encoder = new MetalPass(this, passBacking, framebuffer);

            if (occursBefore == null) {
                encoder.InsertAfter(_encoderListEnd.Prev!);
            } else {
                encoder.InsertBefore((MetalPass)occursBefore);
            }

            _passesToSubmit.Add(encoder);
            return encoder;
        }

        public void Present(float minimumElapsedSeocnds)
        {
            // Ensure we acquired the drawable - in case we didn't encode any render passes.
            EnsureDrawableAcquired();

            IMetalPass? currentEncoder = _encoderListStart.Next;
            for (int i = 0, ilen = _passesToSubmit.Count; i < ilen; ++i) {
                if (currentEncoder == null) {
                    throw new InvalidOperationException("Unexpected end of command buffer list. (Likely a bug in CeresGpu)");
                }
                
                currentEncoder.Finish();
                MetalApi.metalbinding_commit_command_buffer(currentEncoder.CommandBuffer);
                currentEncoder = currentEncoder.Next;
            }
            
            _passesToSubmit.Clear();
            _encoderListStart.ResetAsFront(_encoderListEnd);
            
            // One final command buffer to present 
            // (TODO: Could we cache these?)

            IntPtr finalCommandBuffer = MetalApi.metalbinding_create_command_buffer(Context);
            try {
                MetalApi.metalbinding_present_current_frame_after_minimum_duration(Context, finalCommandBuffer, minimumElapsedSeocnds);
                MetalApi.metalbinding_commit_command_buffer(finalCommandBuffer);
            }
            finally {
                MetalApi.metalbinding_release_command_buffer(finalCommandBuffer);
            }
            
            WorkingFrame = (WorkingFrame + 1) % FrameCount;
            ++UniqueFrameId;

            MetalApi.metalbinding_arp_drain(Context);
            
            NewFrame();
        }

        private void EnsureDrawableAcquired()
        {
            if (!_hasAcquiredDrawable) {
                _hasAcquiredDrawable = true;

                _glfwWindow.GetContentScale(out float scale, out _);
                _glfwWindow.GetSize(out int width, out int height);
                MetalApi.metalbinding_set_content_scale(Context, scale, (uint)width, (uint)height);

                MetalApi.metalbinding_acquire_drawable(Context);
                IntPtr texture = MetalApi.metalbinding_get_current_frame_drawable_texture(Context);
                uint texWidth = 0, texHeight = 0;
                MetalApi.MTLPixelFormat pixelFormat = default;
                MetalApi.metalbinding_get_texture_info(texture, ref texWidth, ref texHeight, ref pixelFormat);
                _swapchainTarget.Drawable = texture;
                _swapchainTarget.Width = texWidth;
                _swapchainTarget.Height = texHeight;
                _swapchainTarget.ColorFormat = pixelFormat.ToColorFormat();

                // Acquire this frame's command buffer for the first time.
                //_currentFrameCommandBuffer = MetalApi.metalbinding_acquire_command_buffer(Context);
            }
        }

        private void NewFrame()
        {
            _hasAcquiredDrawable = false;
            _encoderListStart.ResetAsFront(_encoderListEnd);
        }

        public void GetDiagnosticInfo(IList<(string key, object value)> entries)
        {
            ulong currentAllocatedSize = 0;
            ulong recommendedWorkingSetSize = 0;
            ulong hasUnifiedMemory = 0;
            ulong maxTransferRate = 0;
            
            MetalApi.metalbinding_get_memory_info(Context, ref currentAllocatedSize, ref recommendedWorkingSetSize, ref hasUnifiedMemory, ref maxTransferRate);
            
            entries.Add((nameof(currentAllocatedSize), currentAllocatedSize));
            entries.Add((nameof(recommendedWorkingSetSize), recommendedWorkingSetSize));
            entries.Add((nameof(hasUnifiedMemory), hasUnifiedMemory));
            entries.Add((nameof(maxTransferRate), maxTransferRate));
        }

        // No-BOM utf-8 encoding.
        public static readonly UTF8Encoding UTF8NoBOM = new(false);
        
        public string GetLastError()
        {
            uint len = MetalApi.metalbinding_get_last_error_length(Context);
            IntPtr buffer = Marshal.AllocHGlobal(new IntPtr(len));
            try {
                MetalApi.metalbinding_get_last_error(Context, buffer, len);
                unsafe {
                    return UTF8NoBOM.GetString((byte*)buffer, checked((int)len));
                }
            } finally {
                Marshal.FreeHGlobal(buffer);
            }
        }
    }
}