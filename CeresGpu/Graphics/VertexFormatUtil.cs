using System;

namespace CeresGpu.Graphics;

public static class VertexFormatUtil
{
    public static uint GetbytesPerElement(this VertexFormat vertexFormat)
    {
        return vertexFormat switch {
            VertexFormat.Char => 1,
            VertexFormat.CharNormalized => 1,
            VertexFormat.UChar => 1,
            VertexFormat.UCharNormalized => 1,
            VertexFormat.Half => 1,
            VertexFormat.Float => 1,
            VertexFormat.Short => 1,
            VertexFormat.ShortNormalized => 1,
            VertexFormat.UShort => 1,
            VertexFormat.UShortNormalized => 1,
            VertexFormat.Int => 1,
            VertexFormat.UInt => 1,
            
            VertexFormat.Char2 => 2,
            VertexFormat.Char2Normalized => 2,
            VertexFormat.UChar2 => 2,
            VertexFormat.UChar2Normalized => 2,
            VertexFormat.Half2 => 2,
            VertexFormat.Float2 => 2, 
            VertexFormat.Short2 => 2,
            VertexFormat.Short2Normalized => 2,
            VertexFormat.UShort2 => 2,
            VertexFormat.UShort2Normalized => 2,
            VertexFormat.Int2 => 2,
            VertexFormat.UInt2 => 2,
            
            VertexFormat.Char3 => 3,
            VertexFormat.Char3Normalized => 3,
            VertexFormat.UChar3 => 3,
            VertexFormat.UChar3Normalized => 3,
            VertexFormat.Half3 => 3,
            VertexFormat.Float3 => 3, 
            VertexFormat.Short3 => 3,
            VertexFormat.Short3Normalized => 3,
            VertexFormat.UShort3 => 3,
            VertexFormat.UShort3Normalized => 3,
            VertexFormat.Int3 => 3,
            VertexFormat.UInt3 => 3,
            
            VertexFormat.Char4 => 4,
            VertexFormat.Char4Normalized => 4,
            VertexFormat.UChar4 => 4,
            VertexFormat.UChar4Normalized => 4,
            VertexFormat.Half4 => 4,
            VertexFormat.Float4 => 4, 
            VertexFormat.Short4 => 4,
            VertexFormat.Short4Normalized => 4,
            VertexFormat.UShort4 => 4,
            VertexFormat.UShort4Normalized => 4,
            VertexFormat.Int4 => 4,
            VertexFormat.UInt4 => 4,
            
            VertexFormat.Int1010102Normalized => 4, // 32 bits
            VertexFormat.UInt1010102Normalized => 4, // 32 bits 
            VertexFormat.UChar4Normalized_BGRA => 4,
            
            VertexFormat.Invalid => throw new ArgumentOutOfRangeException(null, nameof(vertexFormat))
            , _ => throw new ArgumentOutOfRangeException()
        };
    }
}