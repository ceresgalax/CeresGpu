using System;
using System.Runtime.InteropServices;
using Metalancer.MetalBinding;

namespace Metalancer.Graphics.Metal
{
    public static class MetalBufferUtil
    {
        public static void CopyBuffer<T>(IntPtr buffer, uint destElementOffset, Span<T> elements, uint elementCount, uint destElementCapacity) where T : unmanaged
        {
            if (buffer == IntPtr.Zero) {
                throw new ArgumentNullException(nameof(buffer));
            }
            
            checked {
                if (elementCount > elements.Length) {
                    throw new ArgumentOutOfRangeException(nameof(elementCount));
                }
                if (destElementOffset + elementCount > destElementCapacity) {
                    throw new ArgumentOutOfRangeException(nameof(destElementOffset));
                }

                uint tSize = (uint)Marshal.SizeOf<T>();
                uint byteSize = tSize * elementCount;
                uint destByteOffset = tSize * destElementOffset; 
                unsafe {
                    fixed (void* source = elements) {
                        MetalApi.metalbinding_copy_to_buffer(buffer, new IntPtr(source), destByteOffset, byteSize);            
                    }
                }
            }
        }
    }
}