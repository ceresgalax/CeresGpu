using System;
using System.Runtime.InteropServices;
using CeresGLFW;
using CeresGpu.Graphics;
using CeresGpu.Graphics.Metal;
using CeresGpu.Graphics.OpenGL;
using CeresGpu.Graphics.Vulkan;
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
        
        GLFW.SwapInterval(1);
    }

    public string[] GetRequiredInstanceExtensions()
    {
        if (GLFW.MainThread == null) {
            GLFW.Init();
        }
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
        
        if (GLFW.MainThread == null) {
            GLFW.Init();
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

        _window = new(
            width: _width,
            height: _height,
            title: _title,
            share: null,
            hints: _hints
        );

        GLFW.MakeContextCurrent(_window);
            
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

    // public static GLFWWindow MakeWindow(WindowHints hints, int width, int height, string title)
    // {
    //     if (GLFW.MainThread == null) {
    //         GLFW.Init();
    //     }
    //         
    //     // GLFW does not allow width or height of 0.
    //     if (width == 0) {
    //         // TODO: Should we log somehow?
    //         width = 1;
    //     }
    //     if (height == 0) {
    //         // TODO: Should we log somehow?
    //         height = 1;
    //     }
    //
    //     GLFWWindow window = new(
    //         width: width,
    //         height: height,
    //         title: title,
    //         share: null,
    //         hints);
    //
    //     GLFW.MakeContextCurrent(window);
    //         
    //     return window;
    // }
        
    // private static GLFWWindow MakeWindow(int width, int height, string title, bool maximized)
    // {
    //     WindowHints hints = MakeWindowHints();
    //     hints.Maximized = maximized;
    //     return MakeWindow(hints, width, height, title);
    // }

    public static IRenderer MakeRenderer(IWindowFactory windowFactory)
    {
        // TODO: Better API selection.

        bool isMacOs = RuntimeInformation.IsOSPlatform(OSPlatform.OSX); 
        
        // Try Metal First
        if (isMacOs && windowFactory is IMetalWindowFactory metalWindowFactory) {
            return new MetalRenderer(metalWindowFactory.GetCocoaWindow(), metalWindowFactory.GetOrCreateWindow());    
        }
        
        // Try Vulkan
        if (!isMacOs && windowFactory is IVulkanWindowFactory vulkanWindowFactory) {
            return new VulkanRenderer(vulkanWindowFactory);
        }
        
        // Try OpenGL
        if (windowFactory is IGLWindowFactory glWindowFactory) {
            return new GLRenderer(windowFactory.GetOrCreateWindow());
        }
        
        // TODO: Different exception type? 
        throw new InvalidOperationException("Failed to find an appropriate renderer impl.");
    }
        
    // public static (IRenderer, GLFWWindow) MakeRenderer(WindowHints hints, int width, int height, string title)
    // {
    //     GLFWWindow window = MakeWindow(hints, width, height, title);
    //     IRenderer renderer = MakeRenderer(window);
    //     return (renderer, window);
    // }
    //
    // public static (IRenderer, GLFWWindow) MakeRenderer(int width, int height, string title, bool maximized = true)
    // {
    //     GLFWWindow window = MakeWindow(width, height, title, maximized);
    //     IRenderer renderer = MakeRenderer(window);
    //     return (renderer, window);
    // }

}