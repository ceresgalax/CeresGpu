using System.Runtime.InteropServices;
using CeresGLFW;
using CeresGpu.Graphics;
using CeresGpu.Graphics.Metal;
using CeresGpu.Graphics.OpenGL;

namespace CeresGpu
{
    public static class Boot
    {

        /// <summary>
        /// Creates suitable window hints for creating a GLFW window suitable for the rendering API of choice for the
        /// current platform.
        /// The returned hints can be modified to suit your application (resize policies, window type, etc)
        /// API related hints should be left as is (Api, etc)
        /// </summary>
        public static WindowHints MakeWindowHints()
        {
            WindowHints hints = new();
            if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX)) {
                hints.ClientApi = Api.NoAPI;    
            } else {
                hints.ContextVersionMajor = 4;
                hints.ContextVersionMinor = 1;
                hints.OpenGLProfile = OpenGLProfile.Core;
                hints.OpenGLForwardCompat = true;    
            }
            
            // GLFW ignores this for macOS. Needed for windows to scale according to size.
            hints.ScaleToMonitor = true;

            return hints;
        }

        public static GLFWWindow MakeWindow(WindowHints hints, int width, int height, string title)
        {
            if (GLFW.MainThread == null) {
                GLFW.Init();
            }
            
            GLFWWindow window = new(
                width: width,
                height: height,
                title: title,
                share: null,
                hints);

            GLFW.MakeContextCurrent(window);
            
            return window;
        }
        
        public static GLFWWindow MakeWindow(int width, int height, string title, bool maximized)
        {
            WindowHints hints = MakeWindowHints();
            hints.Maximized = maximized;
            return MakeWindow(hints, width, height, title);
        }

        public static IRenderer MakeRenderer(GLFWWindow window)
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX)) {
                return new MetalRenderer(window.GetCocoaWindow(), window);    
            }
            
            GLFW.SwapInterval(1);
            return new OpenGLRenderer(window);
        }
        
        public static (IRenderer, GLFWWindow) MakeRenderer(WindowHints hints, int width, int height, string title)
        {
            GLFWWindow window = MakeWindow(hints, width, height, title);
            IRenderer renderer = MakeRenderer(window);
            return (renderer, window);
        }

        public static (IRenderer, GLFWWindow) MakeRenderer(int width, int height, string title, bool maximized = true)
        {
            GLFWWindow window = MakeWindow(width, height, title, maximized);
            IRenderer renderer = MakeRenderer(window);
            return (renderer, window);
        }

    }
}