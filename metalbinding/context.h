#ifndef context_h
#define context_h
#import <Metal/Metal.h>
#import <MetalKit/MetalKit.h>

struct MetalBindingContext {
    //MTKView* view;
    CAMetalLayer* layer;
    id<MTLDevice> device;
    id<CAMetalDrawable> currentDrawable;
    id<MTLCommandQueue> commandQueue;
    dispatch_semaphore_t semaphore;
    NSData *lastError;
    /* NSAutoreleasePool* */ void* arp;
};

typedef struct MetalBindingContext MetalBindingContext;

#endif /* context_h */
