namespace CeresGpu.Graphics
{
    public struct Viewport(uint x, uint y, uint width, uint height)
    {
        public float X = x;
        public float Y = y;
        public float Width = width;
        public float Height = height;
    }
}