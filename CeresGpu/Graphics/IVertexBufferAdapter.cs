using CeresGpu.Graphics.Shaders;

namespace CeresGpu.Graphics;

public interface IVertexBufferAdapter<TShader, TLayout>
    where TShader : IShader
    where TLayout : IVertexBufferLayout<TShader>
{
    int NumVertexBuffers { get; }
    
    // TODO: This is meant to return any IBuffer<T> without type info. 
    //    Buffer<T> doesn't implement an untyped interface because I want to make it hard to mix and match usage of the
    //    typed and untyped buffer manipulation. However, maybe that's not necesary, and there could be benefits of
    //    allowing the typed and untyped interfaces to coexist?
    object GetVertexBuffer(int index);
}