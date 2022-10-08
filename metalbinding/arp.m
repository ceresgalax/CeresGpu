//
// Functions to manage the autorelease pool.
// This file is compiled with ARC disabled.
//

#import <Foundation/Foundation.h>

#include "context.h"

//void metalbinding_arp_init(MetalBindingContext* context) {
//    NSAutoreleasePool* pool = [[NSAutoreleasePool alloc] init];
//    context->arp = pool;
//}

void metalbinding_arp_deinit(MetalBindingContext* context) {
    NSAutoreleasePool* pool = (NSAutoreleasePool*)context->arp;
    if (pool) {
        [pool drain];
    }
}

void metalbinding_arp_drain(MetalBindingContext* context) {
    NSAutoreleasePool* pool = (NSAutoreleasePool*)context->arp;
    
    if (pool) {
        [pool drain];
    }
    
    context->arp = [[NSAutoreleasePool alloc] init];
}
