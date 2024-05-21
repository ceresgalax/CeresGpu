namespace CeresGpu.Graphics;

public enum SamplerAddressMode
{
    ClampToEdge,
    
    // Not supported by OpenGL4.6. Supported by Metal. Haven't checked Vulkan support.
    //MirrorClampToEdge,
    
    Repeat,
    MirrorRepeat,
    
    // Not supported by OpenGL4.6. Supported by Metal. Haven't checked Vulkan support.
    //ClampToZero,
    
    // Not supported by OpenGL4.6. Supported by Metal. Haven't checked Vulkan support.
    //ClampToBorderColor
}