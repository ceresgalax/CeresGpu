﻿using System;
using Silk.NET.Vulkan;
using Buffer = Silk.NET.Vulkan.Buffer;

namespace CeresGpu.Graphics.Vulkan;

public interface IVulkanCommandEncoder
{
    IVulkanCommandEncoder? Prev { get; set; }
    IVulkanCommandEncoder? Next { get; set; }
    
    CommandBuffer CommandBuffer { get; }
    void Finish();
}

public class VulkanCommandEncoderAnchor : IVulkanCommandEncoder
{
    public IVulkanCommandEncoder? Prev { get; set; }
    public IVulkanCommandEncoder? Next { get; set; }
    
    public CommandBuffer CommandBuffer => throw new NotSupportedException();
    
    public void Finish()
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

public sealed class VulkanCommandEncoder : PassEncoder, IVulkanCommandEncoder, IDeferredDisposable 
{
    private readonly VulkanRenderer _renderer;
    private readonly VulkanPassBacking _passBacking;
    private readonly CommandBuffer _commandBuffer;

    public CommandBuffer CommandBuffer => _commandBuffer;

    private bool _isFinished;
    
    public unsafe VulkanCommandEncoder(VulkanRenderer renderer, VulkanPassBacking passBacking, VulkanFramebuffer framebuffer)
    {
        _renderer = renderer;
        _passBacking = passBacking;
        
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
        SetViewport(new Viewport(0, 0, framebuffer.Width, framebuffer.Height));
        SetScissor(new ScissorRect(0, 0, framebuffer.Width, framebuffer.Height));
    }

    private void ReleaseUnmanagedResources()
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

    public IVulkanCommandEncoder? Prev { get; set; }
    public IVulkanCommandEncoder? Next { get; set; }

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
    
    public void InsertBefore(IVulkanCommandEncoder other)
    {
        Prev = other.Prev;
        other.Prev = this;
        Next = other;
    }

    public void InsertAfter(IVulkanCommandEncoder other)
    {
        Next = other.Next;
        other.Next = this;
        Prev = other;
    }
    
    protected override void SetPipelineImpl<TShader, TVertexBufferLayout>(IPipeline<TShader, TVertexBufferLayout> pipeline, IShaderInstance<TShader, TVertexBufferLayout> shaderInstance)
    {
        // TODO: Verify that pipeline is compatible with this renderpass.
        // TODO: ALSO THIS VERIFICATION SHOULD HAPPEN FOR ALL RENDERER IMPLS OF IPASS
        
        if (pipeline is not VulkanPipeline<TShader, TVertexBufferLayout> vulkanPipeline) {
            throw new ArgumentException("Incompatible pipeline backend type", nameof(pipeline));
        }
        
        Vk vk = _renderer.Vk;
        vulkanPipeline.GetPipelineForPassBacking(_passBacking, out Pipeline vkPipeline);
        vk.CmdBindPipeline(_commandBuffer, PipelineBindPoint.Graphics, vkPipeline);

        VulkanShaderInstanceBacking shaderInstanceBacking = (VulkanShaderInstanceBacking)shaderInstance.Backing;

        uint numDescriptorSets = shaderInstanceBacking.Shader.NumDescriptorSets;
        int startSet = _renderer.WorkingFrame * (int)numDescriptorSets;
        ReadOnlySpan<DescriptorSet> descriptorSets = shaderInstanceBacking.DescriptorSets[startSet..(startSet+(int)numDescriptorSets)];
        
        unsafe {
            fixed (DescriptorSet* pDescriptorSets = descriptorSets) {
                vk.CmdBindDescriptorSets(_commandBuffer, PipelineBindPoint.Graphics, shaderInstanceBacking.Shader.PipelineLayout, 0, numDescriptorSets, pDescriptorSets, dynamicOffsetCount: 0, null);
            }
        }
        
        RefreshPipeline();
    }

    public void Finish()
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
    
    protected override void RefreshPipelineImpl()
    {
        if (CurrentShaderInstance == null) {
            throw new InvalidOperationException("No pipeline has been set. Call SetPipeline first!");
        }

        VulkanShaderInstanceBacking vulkanShaderInstanceBacking = (VulkanShaderInstanceBacking)CurrentShaderInstance.Backing;
        vulkanShaderInstanceBacking.Update();
        
        ReadOnlySpan<object?> vertexBuffers = CurrentShaderInstance.VertexBufferAdapter.VertexBuffers;
            
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

    protected override void SetScissorImpl(ScissorRect scissor)
    {
        Rect2D vkScissor = new (new Offset2D(scissor.X, scissor.Y), new Extent2D(scissor.Width, scissor.Height));
        _renderer.Vk.CmdSetScissor(_commandBuffer, 0, 1, in vkScissor);
    }

    protected override void SetViewportImpl(Viewport viewport)
    {
        // Silk.NET.Vulkan.Viewport vkViewport = new (viewport.X, viewport.Y, viewport.Width, viewport.Height, 0f, 1f);
        Silk.NET.Vulkan.Viewport vkViewport = new (viewport.X, viewport.Height - viewport.Y, viewport.Width, -viewport.Height, 0f, 1f);
        _renderer.Vk.CmdSetViewport(_commandBuffer, 0, 1, in vkViewport);
    }

    protected override void DrawImpl(uint vertexCount, uint instanceCount, uint firstVertex, uint firstInstance)
    {
        // TODO: Need to assert that a pipeline has been set. Any way that could be done in CeresGPU's non-api-specific code?
        
        Vk vk = _renderer.Vk;
        vk.CmdDraw(_commandBuffer, vertexCount, instanceCount, firstVertex, firstInstance);
    }

    protected override void DrawIndexedUshortImpl(IBuffer<ushort> indexBuffer, uint indexCount, uint instanceCount, uint firstIndex, int vertexOffset, uint firstInstance)
    {
        DrawIndexed(indexBuffer, IndexType.Uint16, indexCount, instanceCount, firstIndex, vertexOffset, firstInstance);
    }

    protected override void DrawIndexedUintImpl(IBuffer<uint> indexBuffer, uint indexCount, uint instanceCount, uint firstIndex, int vertexOffset, uint firstInstance)
    {
        DrawIndexed(indexBuffer, IndexType.Uint32, indexCount, instanceCount, firstIndex, vertexOffset, firstInstance);
    }

    private void DrawIndexed(object indexBuffer, IndexType indexType, uint indexCount, uint instanceCount, uint firstIndex, int vertexOffset, uint firstInstance)
    {
        if (indexBuffer is not IVulkanBuffer vkIndexBuffer) {
            throw new ArgumentException("Incompatible index buffer", nameof(indexBuffer));
        }
        
        Vk vk = _renderer.Vk;
        vk.CmdBindIndexBuffer(_commandBuffer, vkIndexBuffer.GetBufferForCurrentFrame(), 0, indexType);
        vk.CmdDrawIndexed(_commandBuffer, indexCount, instanceCount, firstIndex, vertexOffset, firstInstance);
    }
    
}