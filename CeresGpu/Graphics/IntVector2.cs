using System.Runtime.InteropServices;

namespace CeresGpu.Graphics
{
    [StructLayout(LayoutKind.Explicit, Size=8)]
    public struct IntVector2
    {
        [FieldOffset(0)] public int X;
        [FieldOffset(4)] public int Y;
    }
}