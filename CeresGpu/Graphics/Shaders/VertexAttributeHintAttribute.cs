using System;

namespace CeresGpu.Graphics.Shaders;

[AttributeUsage(AttributeTargets.Field)]
public class VertexAttributeHintAttribute : Attribute
{
    public readonly string Hint;

    public VertexAttributeHintAttribute(string hint)
    {
        Hint = hint;
    }
}