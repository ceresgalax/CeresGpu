namespace CeresGpu.Graphics
{
    public struct Viewport
    {
        public uint X;
        public uint Y;
        public uint Width;
        public uint Height;

        public Viewport(uint x, uint y, uint width, uint height)
        {
            X = x;
            Y = y;
            Width = width;
            Height = height;
        }
    }
}