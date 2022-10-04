#ifndef metalbinding_h
#define metalbinding_h
#import <Metal/Metal.h>

typedef struct MetalBindingContext MetalBindingContext;

//
// Context
//
MetalBindingContext* metalbinding_create(NSWindow* window, uint frameCount);
void metalbinding_destroy(MetalBindingContext* context);
uint32_t metalbinding_get_last_error_length(MetalBindingContext* context);
void metalbinding_get_last_error(MetalBindingContext* context, char* outUtf8Text, uint32_t length);
void metalbinding_capture(MetalBindingContext* context);
void metalbinding_stop_capture(MetalBindingContext* context);
void metalbinding_set_content_scale(MetalBindingContext* context, float scale, uint32_t drawableWidth, uint32_t drawableHeight);

//
// Render Pass Descriptors
//
MTLRenderPassDescriptor* metalbinding_create_current_frame_render_pass_descriptor(MetalBindingContext* context, bool clear, float r, float g, float b, float a) NS_RETURNS_RETAINED;
void metalbinding_release_render_pass_descriptor(MTLRenderPassDescriptor* NS_RELEASES_ARGUMENT rpd);

//
// Command Buffers
//
id<MTLCommandBuffer> metalbinding_acquire_command_buffer(MetalBindingContext* context) NS_RETURNS_RETAINED;
void metalbinding_release_command_buffer(id<MTLCommandBuffer> NS_RELEASES_ARGUMENT commandBuffer);
void metalbinding_present_current_frame_after_minimum_duration(MetalBindingContext* context, id<MTLCommandBuffer> commandBuffer, double seconds);
void metalbinding_commit_command_buffer(id<MTLCommandBuffer> commandBuffer);

//
// Command Encoders
//
id<MTLRenderCommandEncoder> metalbinding_new_command_encoder(id<MTLCommandBuffer> commandBuffer, MTLRenderPassDescriptor* passDescriptor) NS_RETURNS_RETAINED;
void metalbinding_release_command_encoder(id<MTLRenderCommandEncoder> NS_RELEASES_ARGUMENT encoder);
void metalbinding_command_encoder_end_encoding(id<MTLRenderCommandEncoder> encoder);
void metalbinding_command_encoder_set_pipeline(id<MTLRenderCommandEncoder> encoder, id<MTLRenderPipelineState> pipeline);
void metalbinding_command_encoder_set_scissor(id<MTLRenderCommandEncoder> encoder, int32_t x, int32_t y, uint32_t w, uint32_t h);
void metalbinding_command_encoder_set_viewport(id<MTLRenderCommandEncoder> encoder, uint32_t x, uint32_t y, uint32_t w, uint32_t h);
void metalbinding_command_encoder_set_cull_mode(id<MTLRenderCommandEncoder> encoder, MTLCullMode cullMode);
void metalbinding_command_encoder_set_dss(id<MTLRenderCommandEncoder> encoder, id<MTLDepthStencilState> dss);
void metalbinding_command_encoder_set_vertex_buffer(id<MTLRenderCommandEncoder> encoder, id<MTLBuffer> buffer, uint32_t offset, uint32_t index);
void metalbinding_command_encoder_set_fragment_buffer(id<MTLRenderCommandEncoder> encoder, id<MTLBuffer> buffer, uint32_t offset, uint32_t index);
void metalbinding_command_encoder_draw(id<MTLRenderCommandEncoder> encoder, uint32_t vertexCount, uint32_t instanceCount, uint32_t firstVertex, uint32_t firstInstance);
void metalbinding_command_encoder_draw_indexed(id<MTLRenderCommandEncoder> encoder, MTLIndexType indexType, id<MTLBuffer> indexBuffer, uint32_t indexCount, uint32_t instanceCount, uint32_t indexBufferOffset, uint32_t vertexOffset, uint32_t firstInstance);


//
// Buffers
//
id<MTLBuffer> metalbinding_new_buffer(MetalBindingContext* context, uint32_t length) NS_RETURNS_RETAINED;
void metalbinding_release_buffer(id<MTLBuffer> NS_RELEASES_ARGUMENT buffer);
void metalbinding_copy_to_buffer(id<MTLBuffer> buffer, void* source, uint32_t offset, uint32_t size);
void metalbinding_buffer_did_modify_range(id<MTLBuffer> buffer, uint32_t offset, uint32_t size);

//
// Textures
//
id<MTLTexture> metalbinding_new_texture(MetalBindingContext* context, uint32_t width, uint32_t height, MTLPixelFormat format) NS_RETURNS_RETAINED;
void metalbinding_release_texture(id<MTLTexture> NS_RELEASES_ARGUMENT texture);
void metalbinding_set_texture_data(id<MTLTexture> texture, uint32_t width, uint32_t height, void* data, uint32_t bytesPerRow);

//
// Libraries
//
id<MTLLibrary> metalbinding_new_library(MetalBindingContext* context, const char* utf8Source) NS_RETURNS_RETAINED;
void metalbinding_release_library(id<MTLLibrary> NS_RELEASES_ARGUMENT library);

//
// Functions
//
id<MTLFunction> metalbinding_new_function(id<MTLLibrary> library, const char* utf8Name) NS_RETURNS_RETAINED;
void metalbinding_release_function(id<MTLFunction> NS_RELEASES_ARGUMENT function);

//
// RPDs (Render Pipeline Descriptors)
//
MTLRenderPipelineDescriptor* metalbinding_new_rpd(MetalBindingContext* context) NS_RETURNS_RETAINED;
void metalbinding_release_rpd(MTLRenderPipelineDescriptor* NS_RELEASES_ARGUMENT descriptor);
void metalbinding_set_rpd_functions(MTLRenderPipelineDescriptor* descriptor, id<MTLFunction> vertex, id<MTLFunction> fragment);
void metalbinding_set_rpd_common(MTLRenderPipelineDescriptor* descriptor, BOOL blend, MTLBlendOperation blendOp, MTLBlendFactor sourceRgb, MTLBlendFactor destRgb, MTLBlendFactor sourceAlpha, MTLBlendFactor destAlpha);
void metalbinding_set_rpd_vertex_descriptor(MTLRenderPipelineDescriptor* descriptor, MTLVertexDescriptor* vertexDescriptor);

//
// Pipeline State
//
id<MTLRenderPipelineState> metalbinding_new_pipeline_state(MetalBindingContext* context, MTLRenderPipelineDescriptor* descriptor) NS_RETURNS_RETAINED;
void metalbinding_release_pipeline_state(id<MTLRenderPipelineState> NS_RELEASES_ARGUMENT state);

//
// Vertex Descriptors
//
MTLVertexDescriptor* metalbinding_new_vertex_descriptor(MetalBindingContext* context) NS_RETURNS_RETAINED;
void metalbinding_release_vertex_descriptor(MTLVertexDescriptor* NS_RELEASES_ARGUMENT descriptor);
void metalbinding_set_vertex_descriptor_vad(MTLVertexDescriptor* descriptor, uint index, MTLVertexFormat format, uint32_t offset, uint32_t bufferIndex);
void metalbinding_set_vertex_descriptor_vbl(MTLVertexDescriptor* descriptor, uint index, MTLVertexStepFunction stepFunction, uint32_t stride);

//
// DSDs (Depth Stencil Descriptors)
//
MTLDepthStencilDescriptor* metalbinding_new_dsd(MTLCompareFunction depthCompareFunc, BOOL depthWriteEnabled, MTLStencilDescriptor* backFaceStencil, MTLStencilDescriptor* frontFaceStencil) NS_RETURNS_RETAINED;
void metalbinding_release_dsd(MTLDepthStencilDescriptor* NS_RELEASES_ARGUMENT dsd);

//
// Stencil Descriptors
//
MTLStencilDescriptor* metalbinding_new_stencil_descriptor(MetalBindingContext* context, MTLStencilOperation stencilFailOp, MTLStencilOperation depthFailOp, MTLStencilOperation passOp, MTLCompareFunction stencilCompareFunc, uint32_t readMask, uint32_t writeMask) NS_RETURNS_RETAINED;
void metalbinding_release_stencil_descriptor(MTLStencilDescriptor* NS_RELEASES_ARGUMENT descriptor);

//
// Depth Stencil State
//
id<MTLDepthStencilState> metalbinding_new_depth_stencil_state(MetalBindingContext* context, MTLDepthStencilDescriptor* descriptor) NS_RETURNS_RETAINED;
void metalbinding_release_depth_stencil_state(id<MTLDepthStencilState> NS_RELEASES_ARGUMENT state);

//
// Argument Encoders
//
id<MTLArgumentEncoder> metalbinding_new_argument_encoder(id<MTLFunction> function, uint32_t index) NS_RETURNS_RETAINED;
void metalbinding_release_argument_encoder(id<MTLArgumentEncoder> NS_RELEASES_ARGUMENT encoder);
uint32_t metalbinding_get_argument_buffer_size(id<MTLArgumentEncoder> encoder);
void metalbinding_set_argument_buffer(id<MTLArgumentEncoder> encoder, id<MTLBuffer> buffer);
void metalbinding_encode_buffer_argument(id<MTLArgumentEncoder> encoder, id<MTLRenderCommandEncoder> commandEncoder, id<MTLBuffer> buffer, uint32_t offset, uint32_t index, uint32_t stages);
void metalbinding_encode_texture_argument(id<MTLArgumentEncoder> encoder, id<MTLRenderCommandEncoder> commandEncoder, id<MTLTexture> texture, uint32_t index, uint32_t stages);
void metalbinding_encode_sampler_argument(id<MTLArgumentEncoder> encoder, id<MTLSamplerState> sampler, uint32_t index);

//
// Samplers
//
id<MTLSamplerState> metalbinding_create_sampler(MetalBindingContext* context) NS_RETURNS_RETAINED;
void metalbinding_release_sampler(id<MTLSamplerState> NS_RELEASES_ARGUMENT sampler);

#endif /* metalbinding_h */
