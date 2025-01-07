﻿using System;
using CeresGpu.Graphics.Shaders;
using Silk.NET.Vulkan;
using Buffer = Silk.NET.Vulkan.Buffer;

namespace CeresGpu.Graphics.Vulkan;

public sealed class VulkanCommandEncoder<TRenderPass> : IPass<TRenderPass> where TRenderPass : IRenderPass
{
    private readonly VulkanRenderer _renderer;
    private readonly CommandBuffer _commandBuffer;
    
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
        // TODO: MUST MAKE SURE THAT RENDERER HASN'T BEEN DISPOSED.
        fixed (CommandBuffer* pCommandBuffer = &_commandBuffer) {
            _renderer.Vk.FreeCommandBuffers(_renderer.Device, _renderer.CommandPool, 1, pCommandBuffer);    
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

    private IUntypedShaderInstance? _currentShaderInstance;
    
    public void SetPipeline<TShader, TVertexBufferLayout>(IPipeline<TRenderPass, TShader, TVertexBufferLayout> pipeline, IShaderInstance<TShader, TVertexBufferLayout> shaderInstance) where TShader : IShader where TVertexBufferLayout : IVertexBufferLayout<TShader>
    {
        if (pipeline is not VulkanPipeline<TRenderPass, TShader, TVertexBufferLayout> vulkanPipeline) {
            throw new ArgumentException("Incompatible pipeline backend type", nameof(pipeline));
        }
        
        Vk vk = _renderer.Vk;
        vk.CmdBindPipeline(_commandBuffer, PipelineBindPoint.Graphics, vulkanPipeline.Pipeline);

        _currentShaderInstance = shaderInstance;
        
        RefreshPipeline();
    }

    public void Finish()
    {
        // TODO Need to vkCmdEndRenderPass.
        throw new NotImplementedException();
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