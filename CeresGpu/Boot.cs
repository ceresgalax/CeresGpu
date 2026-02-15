using System;
using System.Runtime.InteropServices;
using CeresGLFW;
using CeresGpu.Graphics;
using CeresGpu.Graphics.Metal;
using CeresGpu.Graphics.OpenGL;
using Silk.NET.Vulkan;

namespace CeresGpu;


public interface IWindowFactory
{
    // TODO: Remove this.
    GLFWWindow GetOrCreateWindow();
}

/// <summary>
/// A window factory that supports making windows with OpenGL contexts.
/// </summary>
public interface IGLWindowFactory : IWindowFactory
{
    void SetOpenGLInfo(int majorVersion, int minorVersion, bool needsCompatibility);
}

/// <summary>
/// A window factory that supports creating a VkSurfaceKHR
/// </summary>
public interface IVulkanWindowFactory : IWindowFactory
{
    string[] GetRequiredInstanceExtensions();
    Result CreateSurface(Instance instance, ReadOnlySpan<AllocationCallbacks> allocator, out SurfaceKHR surface);
}

/// <summary>
/// A window factory that supports returning a Cocoa window.
/// </summary>
public interface IMetalWindowFactory : IWindowFactory
{
    IntPtr GetCocoaWindow();
}

public class GLFWWindowFactory : IGLWindowFactory, IVulkanWindowFactory, IMetalWindowFactory
{
    private WindowHints _hints;
    private int _width;
    private int _height;
    private string _title;

    private GLFWWindow? _window;
    
    public GLFWWindowFactory(WindowHints baseHints, int width, int height, string title)
    {
        _hints = baseHints;
        _width = width;
        _height = height;
        _title = title;
        
        if (GLFW.MainThread == null) {
            GLFW.Init();
        }
    }

    public GLFWWindow GetOrCreateWindow()
    {
        return _window ?? MakeWindow();
    }
    
    public void SetOpenGLInfo(int majorVersion, int minorVersion, bool needsCompatibility)
    {
        _hints.ClientApi = Api.OpenGL;
        _hints.ContextVersionMajor = 4;
        _hints.ContextVersionMinor = 6;
        if (needsCompatibility) {
            _hints.OpenGLProfile = OpenGLProfile.Core;
            _hints.OpenGLForwardCompat = true;    
        }
    }

    public string[] GetRequiredInstanceExtensions()
    {
        return GLFW.GetRequiredInstanceExtensions();
    }
    
    public Result CreateSurface(Instance instance, ReadOnlySpan<AllocationCallbacks> allocator, out SurfaceKHR surface)
    {
        _hints.ClientApi = Api.NoAPI;
        GLFWWindow window = MakeWindow();

        Result result;
        ulong surfaceHandle;
        unsafe {
            fixed (AllocationCallbacks* pAllocator = allocator) {
                // Just in case the zero-length span doesn't return a null pointer..
                IntPtr allocatorIntPtr = IntPtr.Zero;
                if (allocator.Length > 0) {
                    allocatorIntPtr = new IntPtr(pAllocator);
                }
                result = (Result)window.CreateWindowSurface(instance.Handle, allocatorIntPtr, out surfaceHandle);
            }
        }
        
        surface = new SurfaceKHR(surfaceHandle);
        return result;
    }
    
    public IntPtr GetCocoaWindow()
    {
        _hints.ClientApi = Api.NoAPI;
        GLFWWindow window = MakeWindow();
        return window.GetCocoaWindow();
    }
    
    private GLFWWindow MakeWindow()
    {
        if (_window != null) {
            throw new InvalidOperationException("Cannot create surface, the window has already been created.");
        }
            
        // GLFW does not allow width or height of 0.
        if (_width == 0) {
            // TODO: Should we log somehow?
            _width = 1;
        }
        if (_height == 0) {
            // TODO: Should we log somehow?
            _height = 1;
        }
        
        // Make sure windows match video mode so that GLFW can use "Windowed Fullscreen" on platforms like Windows.
        GLFWMonitor? primaryMonitor = GLFW.GetPrimaryMonitor();
        if (primaryMonitor != null) {
            GLFWVideoMode videoMode = primaryMonitor.GetVideoMode();
            _hints.RedBits = videoMode.RedBits;
            _hints.GreenBits = videoMode.GreenBits;
            _hints.BlueBits = videoMode.BlueBits;
            _hints.RefreshRate = videoMode.RefreshRate;
        }

        _window = new(
            width: _width,
            height: _height,
            title: _title,
            share: null,
            hints: _hints
        );

        GLFW.MakeContextCurrent(_window);

        if (_hints.ClientApi is Api.OpenGL or Api.OpenGLES) {
            GLFW.SwapInterval(1);    
        }
            
        return _window;
    }
}


public static class Boot
{
    public static WindowHints MakeBaseWindowHints()
    {
        WindowHints hints = new();

        // GLFW ignores this for macOS. Needed for windows to scale according to size.
        hints.ScaleToMonitor = true;

        return hints;
    }
    
    public static IRenderer MakeRenderer(IWindowFactory windowFactory)
    {
        // TODO: Better API selection.

        bool isMacOs = RuntimeInformation.IsOSPlatform(OSPlatform.OSX); 
        
        // Try Metal First
        if (isMacOs && windowFactory is IMetalWindowFactory metalWindowFactory) {
            return new MetalRenderer(metalWindowFactory.GetCocoaWindow(), metalWindowFactory.GetOrCreateWindow());    
        }
        
        // Try Vulkan
        // if (!isMacOs && windowFactory is IVulkanWindowFactory vulkanWindowFactory) {
        //     return new VulkanRenderer(vulkanWindowFactory);
        // }
        
        // Try OpenGL
        if (windowFactory is IGLWindowFactory glWindowFactory) {
            glWindowFactory.SetOpenGLInfo(4, 6, true);
            return new GLRenderer(windowFactory.GetOrCreateWindow());
        }
        
        // TODO: Different exception type? 
        throw new InvalidOperationException("Failed to find an appropriate renderer impl.");
    }

}