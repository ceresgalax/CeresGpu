namespace CeresGpu.Graphics.OpenGL
{
    public interface IGLBuffer
    {
        void Commit();
        uint GetHandleForCurrentFrame();
    }
}