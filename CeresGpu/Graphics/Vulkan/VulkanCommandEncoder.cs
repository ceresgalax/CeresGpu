using System;
using CeresGpu.Graphics.Shaders;
using Silk.NET.Vulkan;
using Buffer = Silk.NET.Vulkan.Buffer;

namespace CeresGpu.Graphics.Vulkan;

interface IVulkanCommandEncoder : IDisposable
{
    //CommandBuffer CommandBuffer { get; }
}

public abstract class VulkanCommandEncoder
{
    public abstract CommandBuffer CommandBuffer { get; }
    
    public VulkanCommandEncoder? Prev { get; protected set; }
    public VulkanCommandEncoder? Next { get; protected set; }

    public void Remove()
    {
        if (Prev != null || Next != null) {
            if (Prev != null) {
                Prev.Next = Next;
            }
            if (Next != null) {
                Next.Prev = Prev;
            }
        }
    }
    
    public void InsertBefore(VulkanCommandEncoder other)
    {
        Prev = other.Prev;
        other.Prev = this;
        Next = other;
    }

    public void InsertAfter(VulkanCommandEncoder other)
    {
        Next = other.Next;
        other.Next = this;
        Prev = other;
    }

    public abstract void Finish();
}

public class VulkanCommandEncoderAnchor : VulkanCommandEncoder
{
    public override CommandBuffer CommandBuffer => throw new NotSupportedException();
    
    public override void Finish()
    {
        // This should not be called on the anchors.
        throw new NotSupportedException();
    }

    public void ResetAsFront(VulkanCommandEncoderAnchor endAnchor)
    {
        Next = endAnchor;
        endAnchor.Prev = this;
    }
}

public sealed class VulkanCommandEncoder<TRenderPass> : VulkanCommandEncoder, IVulkanCommandEncoder, IPass<TRenderPass>, IDeferredDisposable 
    where TRenderPass : IRenderPass
{
    private readonly VulkanRenderer _renderer;
    private readonly CommandBuffer _commandBuffer;

    public override CommandBuffer CommandBuffer => _commandBuffer;

    private bool _isFinished;

    public unsafe VulkanCommandEncoder(VulkanRenderer renderer, VulkanPassBacking passBacking, VulkanFramebuffer framebuffer)
    {
        _renderer = renderer;
        Vk vk = renderer.Vk;

        // TODO: NEED TO MAKE SURE WE RE-USE UNDERLYING COMMAND BUFFERS
        
        CommandBufferAllocateInfo allocateInfo = new(
            sType: StructureType.CommandBufferAllocateInfo,
            pNext: null,
            commandPool: renderer.CommandPool,
            level: CommandBufferLevel.Primary,
            commandBufferCount: 1
        );
        vk.AllocateCommandBuffers(renderer.Device, in allocateInfo, out _commandBuffer)
            .AssertSuccess("Failed to allocate command buffer");

        CommandBufferBeginInfo beginInfo = new(
            sType: StructureType.CommandBufferBeginInfo,
            pNext: null,
            flags: CommandBufferUsageFlags.OneTimeSubmitBit,
            pInheritanceInfo: null
        );
        vk.BeginCommandBuffer(_commandBuffer, in beginInfo)
            .AssertSuccess("Failed to begin command buffer");

        int numClearValues = passBacking.Definition.ColorAttachments.Length +
                             (passBacking.Definition.DepthStencilAttachment == null ? 0 : 1);
        ClearValue[] clearValues = new ClearValue[numClearValues];

        for (int i = 0; i < numClearValues; ++i) {
            clearValues[i] = new ClearValue(framebuffer.GetClearColor(i));
        }
        if (passBacking.Definition.DepthStencilAttachment != null) {
            clearValues[^1] = new ClearValue(null, framebuffer.GetClearDepthStencil());
        }

        fixed (ClearValue* pClearValues = clearValues) {
            RenderPassBeginInfo passBeginInfo = new(
                sType: StructureType.RenderPassBeginInfo,
                pNext: null,
                renderPass: passBacking.RenderPass,
                framebuffer: framebuffer.GetFramebuffer(),
                renderArea: new Rect2D(new Offset2D(0, 0), new Extent2D(framebuffer.Width, framebuffer.Height)),
                clearValueCount: (uint)numClearValues,
                pClearValues: pClearValues
            );
            vk.CmdBeginRenderPass(_commandBuffer, in passBeginInfo, SubpassContents.Inline);
        }
        
        //
        // Set initial viewport and scissor
        //
        
        Silk.NET.Vulkan.Viewport viewport = new (0, 0, framebuffer.Width, framebuffer.Height, 0f, 1f);
        vk.CmdSetViewport(_commandBuffer, 0, 1, in viewport);
        
        Rect2D scissor = new (new Offset2D(0, 0), new Extent2D(framebuffer.Width, framebuffer.Height));
        vk.CmdSetScissor(_commandBuffer, 0, 1, in scissor);
    }

    private unsafe void ReleaseUnmanagedResources()
    {
        if (!_renderer.IsDisposed) {
            _renderer.DeferDisposal(this);
        }
    }

    public void Dispose()
    {
        ReleaseUnmanagedResources();
        GC.SuppressFinalize(this);
    }

    ~VulkanCommandEncoder()
    {
        ReleaseUnmanagedResources();
    }

    public void DeferredDispose()
    {
        CommandBuffer buffer = _commandBuffer;
        _renderer.Vk.FreeCommandBuffers(_renderer.Device, _renderer.CommandPool, 1, in buffer);
    }

    private IUntypedShaderInstance? _currentShaderInstance;
    
    public void SetPipeline<TShader, TVertexBufferLayout>(IPipeline<TRenderPass, TShader, TVertexBufferLayout> pipeline, IShaderInstance<TShader, TVertexBufferLayout> shaderInstance) where TShader : IShader where TVertexBufferLayout : IVertexBufferLayout<TShader>
    {
        if (pipeline is not VulkanPipeline<TRenderPass, TShader, TVertexBufferLayout> vulkanPipeline) {
            throw new ArgumentException("Incompatible pipeline backend type", nameof(pipeline));
        }
        
        Vk vk = _renderer.Vk;
        vk.CmdBindPipeline(_commandBuffer, PipelineBindPoint.Graphics, vulkanPipeline.Pipeline);

        VulkanShaderInstanceBacking shaderInstanceBacking = (VulkanShaderInstanceBacking)shaderInstance.Backing;

        ReadOnlySpan<IDescriptorSet> descriptorSets = shaderInstance.GetDescriptorSets();
        for (int i = 0; i < descriptorSets.Length; ++i) {
            IDescriptorSet descriptorSet = descriptorSets[i];
            VulkanDescriptorSet vulkanDescriptorSet = (VulkanDescriptorSet)descriptorSet;
            DescriptorSet handle = vulkanDescriptorSet.GetDescriptorSetForCurrentFrame();
            // TODO: Get smart and find a way to call this once to bind all descriptor sets without churning garbage.
            unsafe {
                vk.CmdBindDescriptorSets(_commandBuffer, PipelineBindPoint.Graphics, shaderInstanceBacking.Shader.PipelineLayout, (uint)i, 1, in handle, dynamicOffsetCount: 0, null);    
            }
        }
        
        _currentShaderInstance = shaderInstance;
        
        RefreshPipeline();
    }

    public override void Finish()
    {
        if (_isFinished) {
            return;
        }
        _isFinished = true;
        
        // Note: When CeresGPU supports mutable resources, this is where we'd emit memory barriers for resources that
        // we didn't emit memeory barriers for during encoding. This will ensure that passes that are ordered to execute
        // afterwards will be able to assume that any resources they're seeing for the first time are correctly finished
        // and memory flushed (availble) & cache invalidated (visible).
        
        _renderer.Vk.CmdEndRenderPass(CommandBuffer);
        _renderer.Vk.EndCommandBuffer(CommandBuffer).AssertSuccess("Failed to end command buffer.");
    }

    public ScissorRect CurrentDynamicScissor { get; }
    public Viewport CurrentDynamicViewport { get; }
    
    public void RefreshPipeline()
    {
        if (_currentShaderInstance == null) {
            throw new InvalidOperationException("No pipeline has been set. Call SetPipeline first!");
        }
        
        ReadOnlySpan<IDescriptorSet> descriptorSets = _currentShaderInstance.GetDescriptorSets();
        foreach (IDescriptorSet descriptorSet in descriptorSets) {
            ((VulkanDescriptorSet)descriptorSet).Update();
        }
        
        ReadOnlySpan<object?> vertexBuffers = _currentShaderInstance.VertexBufferAdapter.VertexBuffers;
            
        for (int i = 0, ilen = vertexBuffers.Length; i < ilen; ++i) {
            // No vertex buffer set.
            // TODO: log to let the user know they didn't set a vertex buffer.
            object? untypedVertexBuffer = vertexBuffers[i];
            if (untypedVertexBuffer == null) {
                continue;
            }
            if (untypedVertexBuffer is IVulkanBuffer buffer) {
                buffer.Commit();
                // TODO: Get clever and reduce this to a single vkCmdBindVertexBuffers call.
                Buffer bufferHandle = buffer.GetBufferForCurrentFrame();
                ulong offset = 0;  
                _renderer.Vk.CmdBindVertexBuffers(_commandBuffer, (uint)i, 1, in bufferHandle, in offset);
            } else {
                throw new InvalidOperationException($"Buffer returned by vertex buffer adapter at index {i} is not compatible with VulkanCommandEncoder.");
            }
        }
    }

    public void SetScissor(ScissorRect scissor)
    {
        throw new NotImplementedException();
    }

    public void SetViewport(Viewport viewport)
    {
        throw new NotImplementedException();
    }

    public void Draw(uint vertexCount, uint instanceCount, uint firstVertex, uint firstInstance)
    {
        // TODO: Need to assert that a pipeline has been set. Any way that could be done in CeresGPU's non-api-specific code?
        
        Vk vk = _renderer.Vk;
        vk.CmdDraw(_commandBuffer, vertexCount, instanceCount, firstVertex, firstInstance);
    }

    public void DrawIndexedUshort(IBuffer<ushort> indexBuffer, uint indexCount, uint instanceCount, uint firstIndex, int vertexOffset, uint firstInstance)
    {
        DrawIndexed(indexBuffer, IndexType.Uint16, indexCount, instanceCount, firstIndex, vertexOffset, firstInstance);
    }

    public void DrawIndexedUint(IBuffer<uint> indexBuffer, uint indexCount, uint instanceCount, uint firstIndex, int vertexOffset, uint firstInstance)
    {
        DrawIndexed(indexBuffer, IndexType.Uint32, indexCount, instanceCount, firstIndex, vertexOffset, firstInstance);
    }

    private void DrawIndexed(object indexBuffer, IndexType indexType, uint indexCount, uint instanceCount, uint firstIndex, int vertexOffset, uint firstInstance)
    {
        // TODO: Need to assert that a pipeline has been set. Any way that could be done in CeresGPU's non-api-specific code?
        
        if (indexBuffer is not IVulkanBuffer vkIndexBuffer) {
            throw new ArgumentException("Incompatible index buffer", nameof(indexBuffer));
        }

        vkIndexBuffer.Commit();
        
        Vk vk = _renderer.Vk;
        vk.CmdBindIndexBuffer(_commandBuffer, vkIndexBuffer.GetBufferForCurrentFrame(), 0, indexType);
        vk.CmdDrawIndexed(_commandBuffer, indexCount, instanceCount, firstIndex, vertexOffset, firstInstance);
    }
    
}