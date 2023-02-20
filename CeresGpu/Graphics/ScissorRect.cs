namespace CeresGpu.Graphics
{
    public struct ScissorRect
    {
        public int X;
        public int Y;
        public uint Width;
        public uint Height;

        public ScissorRect(int x, int y, uint width, uint height)
        {
            X = x;
            Y = y;
            Width = width;
            Height = height;
        }
    }
}