namespace CeresGpu.Graphics.OpenGL
{
    public interface IGLBuffer : IBuffer
    {
        //void Commit();
        uint GetHandleForCurrentFrame();
    }
}