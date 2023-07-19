using System;

namespace CeresGpu.Graphics.Shaders;

[AttributeUsage(AttributeTargets.Field)]
public class HintAttribute : Attribute
{
    public readonly string Hint;

    public HintAttribute(string hint)
    {
        Hint = hint;
    }
}