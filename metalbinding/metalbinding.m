#import <Foundation/Foundation.h>
#import <Metal/Metal.h>
#import <MetalKit/MetalKit.h>
#import <AppKit/NSWindow.h>
#import <QuartzCore/CAMetalLayer.h>

#include "metalbinding.h"
#include "context.h"

id<MTLDevice> find_lower_power_device(void) {
    NSArray<id<MTLDevice>>* deviceList = MTLCopyAllDevices();
    for (id<MTLDevice> device in deviceList) {
        if (device.isLowPower) {
            return device;
        }
    }
    return nil;
}


MetalBindingContext* metalbinding_create(NSWindow* window, uint frameCount) {
    MetalBindingContext* context = malloc(sizeof(MetalBindingContext));
    memset((void*)context, 0, sizeof(MetalBindingContext));

//    id<MTLDevice> device = find_lower_power_device();
//    if (device == nil) {
//        device = MTLCreateSystemDefaultDevice();
//    }
    
    CAMetalLayer* layer = [CAMetalLayer layer];
    id<MTLDevice> device = [layer preferredDevice];
    [layer setDevice:device];
    [layer setOpaque:YES];
    context->device = device;
    context->layer = layer;
    
    [layer setPixelFormat:MTLPixelFormatBGRA8Unorm];
    
    // Wait until a drawable is avilable instead of returning NIL after a second.
    [layer setAllowsNextDrawableTimeout:NO];
    
    [[window contentView] setLayer:layer];
    [[window contentView] setWantsLayer:YES];
    
//    MTKView* view = [[MTKView alloc] initWithFrame:[window frame]];
//    context->view = view;
    //id<MTLDevice> device = [view preferredDevice];
    //[view setDevice:device];
    //context->device = device;
    context->commandQueue = [device newCommandQueue];
    
    //MTKView* view = [[MTKView alloc] initWithFrame: [window frame] device:device];
    //context->view = view;

    //[view setPaused:YES];
    //[view setEnableSetNeedsDisplay:NO];
    
    //[view setAutoResizeDrawable:YES];
    
    //[window setContentView: context->view];
    
    context->semaphore = dispatch_semaphore_create(frameCount);
    
    return context;
}

void metalbinding_destroy(MetalBindingContext* context) {
    //[context->view release];
    // TODO: Does this actually release everything in the struct!? Check the generated code!!
    free(context);
}

//void metalbinding_set_prefered_framexs_per_second(MetalBindingContext* context, int frameRate) {
//    context->view.preferredFramesPerSecond = frameRate;
//}

uint32_t metalbinding_get_last_error_length(MetalBindingContext* context) {
    if (!context->lastError) {
        return 0;
    }
    NSUInteger length = [context->lastError length];
    if (length > UINT32_MAX) {
        return UINT32_MAX;
    }
    return (uint32_t)length;
}

void metalbinding_get_last_error(MetalBindingContext* context, char* outUtf8Text, uint32_t length) {
    if (!context->lastError) {
        return;
    }
    
    NSUInteger srcLength = [context->lastError length];
    if (length > srcLength) {
        length = (uint32_t)srcLength;
    }
    
    memcpy(outUtf8Text, [context->lastError bytes], length);
}

void set_last_error(MetalBindingContext* context, NSError* error) {
    if (error) {
        context->lastError = [[error localizedDescription] dataUsingEncoding:NSUTF8StringEncoding];
    } else {
        context->lastError = NULL;
    }
}

void metalbinding_capture(MetalBindingContext* context) {
    MTLCaptureManager* captureManager = [MTLCaptureManager sharedCaptureManager];
    
    if (![captureManager supportsDestination:MTLCaptureDestinationGPUTraceDocument]) {
        NSLog(@"Capture to gpu trace document not supported");
        return;
    }
    
    NSURL* dest = [[[NSFileManager defaultManager] temporaryDirectory] URLByAppendingPathComponent:@"frameCapture.gputrace"];
    NSLog(@"Capturing to %@", [dest absoluteString]);
    
    MTLCaptureDescriptor* descriptor = [[MTLCaptureDescriptor alloc] init];
    descriptor.captureObject = context->device;
    descriptor.destination = MTLCaptureDestinationGPUTraceDocument;
    descriptor.outputURL = dest;
    
    NSError* error;
    if (![captureManager startCaptureWithDescriptor:descriptor error:&error]) {
        NSLog(@"%@", [error localizedDescription]);
    }
}

void metalbinding_stop_capture(MetalBindingContext* context) {
    [[MTLCaptureManager sharedCaptureManager] stopCapture];
}

void metalbinding_set_content_scale(MetalBindingContext* context, float scale, uint32_t drawableWidth, uint32_t drawableHeight) {
    [context->layer setContentsScale:scale];
    [context->layer setDrawableSize:CGSizeMake(drawableWidth * scale, drawableHeight * scale)];
}

id<MTLTexture> metalbinding_get_current_frame_drawable_texture(MetalBindingContext* context) {
    if (!context->currentDrawable) {
        NSLog(@"BAD: No current drawable.");
        return NULL;
    }
    return context->currentDrawable.texture;
}

//
// Render Pass Descriptors
//
MTLRenderPassDescriptor* metalbinding_create_render_pass_descriptor(void) NS_RETURNS_RETAINED {
    // TODO: Do we need NS_RETURNS_RETAINED on this function and the related "metalbinding_release_render_pass_descriptor" function?
    //       The [MTLRenderPassDescriptor renderPassDescriptor] method below states the the returned renderPassDescriptor is autoreleased..
    MTLRenderPassDescriptor* pass = [MTLRenderPassDescriptor renderPassDescriptor];
    return pass;
}

//MTLRenderPassDescriptor* metalbinding_create_current_frame_render_pass_descriptor(MetalBindingContext* context, bool clear, double r, double g, double b, double a) NS_RETURNS_RETAINED {
//    if (!context->currentDrawable) {
//        NSLog(@"BAD: No current drawable.");
//        return NULL;
//    }
//
//    MTLRenderPassDescriptor* pass = [MTLRenderPassDescriptor renderPassDescriptor];
//    if (clear) {
//        pass.colorAttachments[0].clearColor = MTLClearColorMake(r, g, b, a);
//        pass.colorAttachments[0].loadAction = MTLLoadActionClear;
//    } else {
//        pass.colorAttachments[0].loadAction = MTLLoadActionDontCare;
//    }
//    pass.colorAttachments[0].storeAction = MTLStoreActionStore;
//    pass.colorAttachments[0].texture = context->currentDrawable.texture;
//
//    // TODO: No texture is set to the depth attachment. Are we not using a depth buffer!?
//    pass.depthAttachment.loadAction = MTLLoadActionClear;
//    pass.depthAttachment.storeAction = MTLStoreActionStore;
//    pass.depthAttachment.clearDepth = 1.0;
//    return pass;
//
//    //return [context->view currentRenderPassDescriptor];
//}


void metalbinding_set_render_pass_descriptor_color_attachment(MTLRenderPassDescriptor* descriptor, uint32_t colorAttachmentIndex, id<MTLTexture> texture, MTLLoadAction loadAction, MTLStoreAction storeAction, double clearR, double clearG, double clearB, double clearA) {
    MTLRenderPassColorAttachmentDescriptor* attachmentDescriptor = descriptor.colorAttachments[colorAttachmentIndex];
    
    attachmentDescriptor.clearColor = MTLClearColorMake(clearR, clearG, clearB, clearA);
    attachmentDescriptor.loadAction = loadAction;
    attachmentDescriptor.storeAction = storeAction;
    attachmentDescriptor.texture = texture;
    
    // TODO: Verify if this is really necesary.
    // In the past there was an issue where just editing the returned descriptor didn't get applied to the descriptor
    // in the attachment array, so I fear that the api is returning a pointer to a copy of the object instead of a reference.
    descriptor.colorAttachments[colorAttachmentIndex] = attachmentDescriptor;
}

void metalbinding_set_render_pass_descriptor_depth_attachment(MTLRenderPassDescriptor* descriptor, id<MTLTexture> texture, MTLLoadAction loadAction, MTLStoreAction storeAction, double clearDepth) {
    descriptor.depthAttachment.texture = texture;
    descriptor.depthAttachment.clearDepth = clearDepth;
    descriptor.depthAttachment.loadAction = loadAction;
    descriptor.depthAttachment.storeAction = storeAction;
}

void metalbinding_set_render_pass_descriptor_stencil_attachment(MTLRenderPassDescriptor* descriptor, id<MTLTexture> texture, MTLLoadAction loadAction, MTLStoreAction storeAction, uint32_t clearStencil) {
    descriptor.stencilAttachment.texture = texture;
    descriptor.stencilAttachment.clearStencil = clearStencil;
    descriptor.stencilAttachment.loadAction = loadAction;
    descriptor.stencilAttachment.storeAction = storeAction;
}

void metalbinding_release_render_pass_descriptor(MTLRenderPassDescriptor* NS_RELEASES_ARGUMENT rpd) {}

//
// Command Buffers
//
id<MTLCommandBuffer> metalbinding_acquire_command_buffer(MetalBindingContext* context) NS_RETURNS_RETAINED {
    id<MTLCommandBuffer> commandBuffer = [context->commandQueue commandBuffer];
    dispatch_semaphore_wait(context->semaphore, DISPATCH_TIME_FOREVER);
    [commandBuffer addCompletedHandler:^(id<MTLCommandBuffer> _Nonnull _) {
        dispatch_semaphore_signal(context->semaphore);
    }];
    
    id<CAMetalDrawable> drawable = [context->layer nextDrawable];
    if (!drawable) {
        NSLog(@"WTF: nextDrawable returned NULL? This shouldn't happen if setAllowsNextDrawableTimeout is set to NO");
    }
    context->currentDrawable = drawable;
    
    return commandBuffer;
}

void metalbinding_release_command_buffer(id<MTLCommandBuffer> NS_RELEASES_ARGUMENT commandBuffer) {}

void metalbinding_present_current_frame_after_minimum_duration(MetalBindingContext* context, id<MTLCommandBuffer> commandBuffer, double seconds) {
    if (seconds > 0.0) {
        [commandBuffer presentDrawable:context->currentDrawable afterMinimumDuration:seconds];
    } else {
        [commandBuffer presentDrawable:context->currentDrawable];
    }
}

void metalbinding_commit_command_buffer(id<MTLCommandBuffer> commandBuffer) {
    [commandBuffer commit];
}

//
// Command Encoders
//
id<MTLRenderCommandEncoder> metalbinding_new_command_encoder(id<MTLCommandBuffer> commandBuffer, MTLRenderPassDescriptor* passDescriptor) NS_RETURNS_RETAINED {
    return [commandBuffer renderCommandEncoderWithDescriptor:passDescriptor];
}

void metalbinding_release_command_encoder(id<MTLRenderCommandEncoder> NS_RELEASES_ARGUMENT encoder) {}

void metalbinding_command_encoder_end_encoding(id<MTLRenderCommandEncoder> encoder) {
    [encoder endEncoding];
}

void metalbinding_command_encoder_set_pipeline(id<MTLRenderCommandEncoder> encoder, id<MTLRenderPipelineState> pipeline) {
    [encoder setRenderPipelineState:pipeline];
}

void metalbinding_command_encoder_set_scissor(id<MTLRenderCommandEncoder> encoder, int32_t x, int32_t y, uint32_t w, uint32_t h) {
    MTLScissorRect rect = {};
    rect.x = x;
    rect.y = y;
    rect.width = w;
    rect.height = h;
    [encoder setScissorRect:rect];
}

void metalbinding_command_encoder_set_viewport(id<MTLRenderCommandEncoder> encoder, uint32_t x, uint32_t y, uint32_t w, uint32_t h) {
    MTLViewport viewport = {};
    viewport.originX = x;
    viewport.originY = y;
    viewport.width = w;
    viewport.height = h;
    viewport.znear = 0;
    viewport.zfar = 1;
    [encoder setViewport:viewport];
}

void metalbinding_command_encoder_set_cull_mode(id<MTLRenderCommandEncoder> encoder, MTLCullMode cullMode) {
    [encoder setCullMode:cullMode];
}

void metalbinding_command_encoder_set_dss(id<MTLRenderCommandEncoder> encoder, id<MTLDepthStencilState> dss) {
    [encoder setDepthStencilState:dss];
}

void metalbinding_command_encoder_set_vertex_buffer(id<MTLRenderCommandEncoder> encoder, id<MTLBuffer> buffer, uint32_t offset, uint32_t index) {
    [encoder setVertexBuffer:buffer offset:offset atIndex:index];
}

void metalbinding_command_encoder_set_fragment_buffer(id<MTLRenderCommandEncoder> encoder, id<MTLBuffer> buffer, uint32_t offset, uint32_t index) {
    [encoder setFragmentBuffer:buffer offset:offset atIndex:index];
}

void metalbinding_command_encoder_draw(id<MTLRenderCommandEncoder> encoder, uint32_t vertexCount, uint32_t instanceCount, uint32_t firstVertex, uint32_t firstInstance
) {
    [encoder drawPrimitives:MTLPrimitiveTypeTriangle
                vertexStart:firstVertex
                vertexCount:vertexCount
              instanceCount:instanceCount
               baseInstance:firstInstance];
}

void metalbinding_command_encoder_draw_indexed(id<MTLRenderCommandEncoder> encoder, MTLIndexType indexType, id<MTLBuffer> indexBuffer, uint32_t indexCount, uint32_t instanceCount, uint32_t indexBufferOffset, uint32_t vertexOffset, uint32_t firstInstance
) {
    [encoder drawIndexedPrimitives:MTLPrimitiveTypeTriangle
                        indexCount:indexCount
                         indexType:indexType
                       indexBuffer:indexBuffer
                 indexBufferOffset:indexBufferOffset
                     instanceCount:instanceCount
                        baseVertex:vertexOffset
                      baseInstance:firstInstance];
}

//
// Buffers
//

id<MTLBuffer> metalbinding_new_buffer(MetalBindingContext* context, uint32_t length) NS_RETURNS_RETAINED {
    id<MTLDevice> device = context->device;
    //id<MTLBuffer> buffer = [device newBufferWithLength:length options:MTLResourceStorageModeManaged];
    id<MTLBuffer> buffer = [device newBufferWithLength:length options:MTLResourceStorageModeShared];
    return buffer;
}

void metalbinding_release_buffer(id<MTLBuffer> NS_RELEASES_ARGUMENT buffer) { }

void metalbinding_copy_to_buffer(id<MTLBuffer> buffer, void* source, uint32_t offset, uint32_t size) {
    void* dest = [buffer contents] + offset;
    memcpy(dest, source, size);
    //[buffer didModifyRange:NSMakeRange(offset, size)];
}

void metalbinding_buffer_did_modify_range(id<MTLBuffer> buffer, uint32_t offset, uint32_t size) {
    //[buffer didModifyRange:NSMakeRange(offset, size)];
}

void* metalbinding_buffer_get_contents(id<MTLBuffer> buffer) {
    return [buffer contents];
}

//
// Textures
//

id<MTLTexture> metalbinding_new_texture(MetalBindingContext* context, uint32_t width, uint32_t height, MTLPixelFormat format) NS_RETURNS_RETAINED {
    MTLTextureDescriptor* descriptor = [[MTLTextureDescriptor alloc] init];
    [descriptor setWidth:width];
    [descriptor setHeight:height];
    [descriptor setPixelFormat:format];
    [descriptor setTextureType:MTLTextureType2D];
    [descriptor setStorageMode:MTLStorageModeManaged];
    [descriptor setUsage:MTLResourceUsageRead | MTLResourceUsageSample];
    return [context->device newTextureWithDescriptor:descriptor];
}

void metalbinding_release_texture(id<MTLTexture> NS_RELEASES_ARGUMENT texture) {}

void metalbinding_set_texture_data(id<MTLTexture> texture, uint32_t width, uint32_t height, void* data, uint32_t bytesPerRow) {
    [texture replaceRegion:MTLRegionMake2D(0, 0, width, height)
               mipmapLevel:0
                 withBytes:data
               bytesPerRow:bytesPerRow];
}

//
// Libraries
//

id<MTLLibrary> metalbinding_new_library(MetalBindingContext* context, const char* utf8Source) NS_RETURNS_RETAINED {
    NSString* source = [NSString stringWithUTF8String:utf8Source];
    NSError* error = NULL;
    id<MTLLibrary> library = [context->device newLibraryWithSource:source options:NULL error:&error];
    if (!library) {
        set_last_error(context, error);
    }
    return library;
}

void metalbinding_release_library(id<MTLLibrary> NS_RELEASES_ARGUMENT library) {}

//
// Functions
//

id<MTLFunction> metalbinding_new_function(id<MTLLibrary> library, const char* utf8Name) NS_RETURNS_RETAINED {
    return [library newFunctionWithName:[NSString stringWithUTF8String:utf8Name]];
}

void metalbinding_release_function(id<MTLFunction> NS_RELEASES_ARGUMENT function) { }

//
// RPDs (Render Pipeline Descriptors)
//

MTLRenderPipelineDescriptor* metalbinding_new_rpd(MetalBindingContext* context) NS_RETURNS_RETAINED {
    return [[MTLRenderPipelineDescriptor alloc] init];
}

void metalbinding_release_rpd(MTLRenderPipelineDescriptor* NS_RELEASES_ARGUMENT descriptor) {}

void metalbinding_set_rpd_functions(MTLRenderPipelineDescriptor* descriptor, id<MTLFunction> vertex, id<MTLFunction> fragment) {
    [descriptor setVertexFunction:vertex];
    [descriptor setFragmentFunction:fragment];
}

void metalbinding_set_rpd_common(MTLRenderPipelineDescriptor* descriptor, BOOL blend, MTLBlendOperation blendOp, MTLBlendFactor sourceRgb,
                                 MTLBlendFactor destRgb, MTLBlendFactor sourceAlpha, MTLBlendFactor destAlpha
) {
    //MTLRenderPipelineColorAttachmentDescriptor* cad = [[MTLRenderPipelineColorAttachmentDescriptor alloc] init];
//    [cad setBlendingEnabled:blend];
//    [cad setRgbBlendOperation:blendOp];
//    [cad setAlphaBlendOperation:blendOp];
//    [cad setSourceRGBBlendFactor:sourceRgb];
//    [cad setDestinationRGBBlendFactor:destRgb];
//    [cad setSourceAlphaBlendFactor:sourceAlpha];
//    [cad setDestinationAlphaBlendFactor:destAlpha];
//    [[descriptor colorAttachments] setObject:cad atIndexedSubscript:0];
    
    descriptor.colorAttachments[0].blendingEnabled = blend;
    descriptor.colorAttachments[0].rgbBlendOperation = blendOp;
    descriptor.colorAttachments[0].alphaBlendOperation = blendOp;
    descriptor.colorAttachments[0].sourceRGBBlendFactor = sourceRgb;
    descriptor.colorAttachments[0].destinationRGBBlendFactor = destRgb;
    descriptor.colorAttachments[0].sourceAlphaBlendFactor = sourceAlpha;
    descriptor.colorAttachments[0].destinationAlphaBlendFactor = destAlpha;
    descriptor.colorAttachments[0].pixelFormat = MTLPixelFormatBGRA8Unorm;
}

void metalbinding_set_rpd_vertex_descriptor(MTLRenderPipelineDescriptor* descriptor, MTLVertexDescriptor* vertexDescriptor) {
    [descriptor setVertexDescriptor:vertexDescriptor];
}

//
// Vertex Descriptors
//
MTLVertexDescriptor* metalbinding_new_vertex_descriptor(MetalBindingContext* context) NS_RETURNS_RETAINED {
    return [MTLVertexDescriptor vertexDescriptor];
}
    
void metalbinding_release_vertex_descriptor(MTLVertexDescriptor* NS_RELEASES_ARGUMENT descriptor) {}

void metalbinding_set_vertex_descriptor_vad(MTLVertexDescriptor* descriptor, uint index, MTLVertexFormat format, uint32_t offset, uint32_t bufferIndex) {
    descriptor.attributes[index].format = format;
    descriptor.attributes[index].offset = offset;
    descriptor.attributes[index].bufferIndex = bufferIndex;
}

void metalbinding_set_vertex_descriptor_vbl(MTLVertexDescriptor* descriptor, uint index, MTLVertexStepFunction stepFunction, uint32_t stride) {
    descriptor.layouts[index].stepFunction = stepFunction;
    descriptor.layouts[index].stride = stride;
}

//
// Pipeline State
//
id<MTLRenderPipelineState> metalbinding_new_pipeline_state(MetalBindingContext* context, MTLRenderPipelineDescriptor* descriptor) NS_RETURNS_RETAINED {
    NSError* error = NULL;
    id<MTLRenderPipelineState> state = NULL;
    state = [context->device newRenderPipelineStateWithDescriptor:descriptor error:&error];
    
    if (!state) {
        set_last_error(context, error);
    }
    return state;
}

void metalbinding_release_pipeline_state(id<MTLRenderPipelineState> NS_RELEASES_ARGUMENT state) {}

//
// DSDs (Depth Stencil Descriptors)
//
MTLDepthStencilDescriptor* metalbinding_new_dsd(MTLCompareFunction depthCompareFunc, BOOL depthWriteEnabled, MTLStencilDescriptor* backFaceStencil, MTLStencilDescriptor* frontFaceStencil
) NS_RETURNS_RETAINED {
    MTLDepthStencilDescriptor* descriptor = [[MTLDepthStencilDescriptor alloc] init];
    [descriptor setDepthCompareFunction:depthCompareFunc];
    [descriptor setDepthWriteEnabled:depthWriteEnabled];
    [descriptor setBackFaceStencil:backFaceStencil];
    [descriptor setFrontFaceStencil:frontFaceStencil];
    return descriptor;
}

void metalbinding_release_dsd(MTLDepthStencilDescriptor* NS_RELEASES_ARGUMENT dsd) {}

//
// Stencil Descriptors
//
MTLStencilDescriptor* metalbinding_new_stencil_descriptor(MetalBindingContext* context, MTLStencilOperation stencilFailOp, MTLStencilOperation depthFailOp, MTLStencilOperation passOp, MTLCompareFunction stencilCompareFunc, uint32_t readMask, uint32_t writeMask
) NS_RETURNS_RETAINED {
    MTLStencilDescriptor* descriptor = [[MTLStencilDescriptor alloc] init];
    [descriptor setStencilFailureOperation:stencilFailOp];
    [descriptor setDepthFailureOperation:depthFailOp];
    [descriptor setDepthStencilPassOperation:passOp];
    [descriptor setStencilCompareFunction:stencilCompareFunc];
    [descriptor setReadMask:readMask];
    [descriptor setWriteMask:writeMask];
    return descriptor;
}

void metalbinding_release_stencil_descriptor(MTLStencilDescriptor* NS_RELEASES_ARGUMENT descriptor) {}

//
// Depth Stencil State
//
id<MTLDepthStencilState> metalbinding_new_depth_stencil_state(MetalBindingContext* context, MTLDepthStencilDescriptor* descriptor) NS_RETURNS_RETAINED {
    return [context->device newDepthStencilStateWithDescriptor:descriptor];
}

void metalbinding_release_depth_stencil_state(id<MTLDepthStencilState> NS_RELEASES_ARGUMENT state) {}

//
// Argument Encoders
//
id<MTLArgumentEncoder> metalbinding_new_argument_encoder(id<MTLFunction> function, uint32_t index) NS_RETURNS_RETAINED {
    return [function newArgumentEncoderWithBufferIndex:index];
}

void metalbinding_release_argument_encoder(id<MTLArgumentEncoder> NS_RELEASES_ARGUMENT encoder) {}

uint32_t metalbinding_get_argument_buffer_size(id<MTLArgumentEncoder> encoder) {
    return (uint32_t)[encoder encodedLength];
}

void metalbinding_set_argument_buffer(id<MTLArgumentEncoder> encoder, id<MTLBuffer> buffer) {
    [encoder setArgumentBuffer:buffer offset:0];
}

void metalbinding_encode_buffer_argument(id<MTLArgumentEncoder> encoder, id<MTLRenderCommandEncoder> commandEncoder, id<MTLBuffer> buffer, uint32_t offset, uint32_t index, uint32_t stages) {
    [commandEncoder useResource:buffer usage:MTLResourceUsageRead stages:stages];
    [encoder setBuffer:buffer offset:offset atIndex:index];
}

void metalbinding_dump_buffer(id<MTLBuffer> buffer) {
    void* ptr = (__bridge void *)(buffer);
    NSLog(@"buffer dump of %lx (len: %i)", (unsigned long)ptr, (uint32_t)[buffer length]);
    
    for (NSUInteger i = 0, ilen = [buffer length]; i < ilen; i += 8) {
        uint64_t* data = (uint64_t*)([buffer contents]) + i;
        NSLog(@"%llx", *data);
    }
}

void metalbinding_encode_texture_argument(id<MTLArgumentEncoder> encoder, id<MTLRenderCommandEncoder> commandEncoder, id<MTLTexture> texture, uint32_t index, uint32_t stages) {
    [commandEncoder useResource:texture usage:MTLResourceUsageSample stages:stages];
    [encoder setTexture:texture atIndex:index];
}

void metalbinding_encode_sampler_argument(id<MTLArgumentEncoder> encoder, id<MTLSamplerState> sampler, uint32_t index) {
    [encoder setSamplerState:sampler atIndex:index];
}

//
// Samplers
//
id<MTLSamplerState> metalbinding_create_sampler(
    MetalBindingContext* context,
    MTLSamplerMinMagFilter min,
    MTLSamplerMinMagFilter mag,
    MTLSamplerMipFilter mip,
    MTLSamplerAddressMode rAddressMode,
    MTLSamplerAddressMode sAddressMode,
    MTLSamplerAddressMode tAddressMode,
    BOOL normalizedCoordinates,
    BOOL supportArgumentBuffers
) NS_RETURNS_RETAINED {
    MTLSamplerDescriptor* desc = [MTLSamplerDescriptor new];
    desc.minFilter = min;
    desc.magFilter = mag;
    desc.mipFilter = mip;
    desc.rAddressMode = rAddressMode;
    desc.sAddressMode = sAddressMode;
    desc.tAddressMode = tAddressMode;
    desc.normalizedCoordinates = normalizedCoordinates;
    desc.supportArgumentBuffers = supportArgumentBuffers;
    return [context->device newSamplerStateWithDescriptor:desc];
}

void metalbinding_release_sampler(id<MTLSamplerState> NS_RELEASES_ARGUMENT sampler) {}

//
// Stats
//

void metalbinding_get_memory_info(
    MetalBindingContext* context,
    uint64_t* ref_current_allocated_size,
    uint64_t* ref_recommended_working_set_size,
    uint64_t* ref_has_unified_memory,
    uint64_t* ref_max_transfer_rate
) {
    id<MTLDevice> device = context->device;
    *ref_current_allocated_size = [device currentAllocatedSize];
    *ref_recommended_working_set_size = [device recommendedMaxWorkingSetSize];
    *ref_has_unified_memory = [device hasUnifiedMemory];
    *ref_max_transfer_rate = [device maxTransferRate];
}
