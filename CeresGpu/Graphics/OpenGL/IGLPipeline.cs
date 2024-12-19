using CeresGL;

namespace CeresGpu.Graphics.OpenGL
{
    public interface IGLPipeline
    {
        IVertexBufferLayout VertexBufferLayout { get; }
        
        public void Setup(GL gl);
    }
}