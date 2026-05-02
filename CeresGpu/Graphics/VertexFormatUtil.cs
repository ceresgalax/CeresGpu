using System;

namespace CeresGpu.Graphics;

public static class VertexFormatUtil
{
    public static uint GetBytesPerElement(this VertexFormat vertexFormat)
    {
        return vertexFormat switch {
            VertexFormat.Char => 1,
            VertexFormat.CharNormalized => 1,
            VertexFormat.UChar => 1,
            VertexFormat.UCharNormalized => 1,
            VertexFormat.Half => 2,
            VertexFormat.Short => 2,
            VertexFormat.ShortNormalized => 2,
            VertexFormat.UShort => 2,
            VertexFormat.UShortNormalized => 2,
            VertexFormat.Float => 4,
            VertexFormat.Int => 4,
            VertexFormat.UInt => 4,
            
            VertexFormat.Char2 => 1 * 2,
            VertexFormat.Char2Normalized => 1 * 2,
            VertexFormat.UChar2 => 1 * 2,
            VertexFormat.UChar2Normalized => 1 * 2,
            VertexFormat.Half2 => 2 * 2,
            VertexFormat.Short2 => 2 * 2,
            VertexFormat.Short2Normalized => 2 * 2,
            VertexFormat.UShort2 => 2 * 2,
            VertexFormat.UShort2Normalized => 2 * 2,
            VertexFormat.Float2 => 4 * 2,
            VertexFormat.Int2 => 4 * 2,
            VertexFormat.UInt2 => 4 * 2,
            
            VertexFormat.Char3 => 1 * 3,
            VertexFormat.Char3Normalized => 1 * 3,
            VertexFormat.UChar3 => 1 * 3,
            VertexFormat.UChar3Normalized => 1 * 3,
            VertexFormat.Half3 => 2 * 3,
            VertexFormat.Short3 => 2 * 3,
            VertexFormat.Short3Normalized => 2 * 3,
            VertexFormat.UShort3 => 2 * 3,
            VertexFormat.UShort3Normalized => 2 * 3,
            VertexFormat.Float3 => 4 * 3,
            VertexFormat.Int3 => 4 * 3,
            VertexFormat.UInt3 => 4 * 3,
            
            VertexFormat.Char4 => 1 * 4,
            VertexFormat.Char4Normalized => 1 * 4,
            VertexFormat.UChar4 => 1 * 4,
            VertexFormat.UChar4Normalized => 1 * 4,
            VertexFormat.Half4 => 2 * 4,
            VertexFormat.Short4 => 2 * 4,
            VertexFormat.Short4Normalized => 2 * 4,
            VertexFormat.UShort4 => 2 * 4,
            VertexFormat.UShort4Normalized => 2 * 4,
            VertexFormat.Float4 => 4 * 4,
            VertexFormat.Int4 => 4 * 4,
            VertexFormat.UInt4 => 4 * 4,
            
            VertexFormat.Int1010102Normalized => 4, // 32 bits
            VertexFormat.UInt1010102Normalized => 4, // 32 bits 
            VertexFormat.UChar4Normalized_BGRA => 1 * 4,
            
            VertexFormat.Invalid => throw new ArgumentOutOfRangeException(null, nameof(vertexFormat))
            , _ => throw new ArgumentOutOfRangeException()
        };
    }

    public static VertexComponentType GetComponentType(this VertexFormat format)
    {
        return format switch {
            VertexFormat.Invalid => throw new ArgumentOutOfRangeException(nameof(format)),
            VertexFormat.UChar2 => VertexComponentType.UnsignedChar,
            VertexFormat.UChar3 => VertexComponentType.UnsignedChar,
            VertexFormat.UChar4 => VertexComponentType.UnsignedChar,
            VertexFormat.Char2 => VertexComponentType.Char,
            VertexFormat.Char3 => VertexComponentType.Char,
            VertexFormat.Char4 => VertexComponentType.Char,
            VertexFormat.UChar2Normalized => VertexComponentType.UnsignedCharNormalized,
            VertexFormat.UChar3Normalized => VertexComponentType.UnsignedCharNormalized,
            VertexFormat.UChar4Normalized => VertexComponentType.UnsignedCharNormalized,
            VertexFormat.Char2Normalized => VertexComponentType.CharNormalized,
            VertexFormat.Char3Normalized => VertexComponentType.CharNormalized,
            VertexFormat.Char4Normalized => VertexComponentType.CharNormalized,
            VertexFormat.UShort2 => VertexComponentType.UnsignedShort,
            VertexFormat.UShort3 => VertexComponentType.UnsignedShort,
            VertexFormat.UShort4 => VertexComponentType.UnsignedShort,
            VertexFormat.Short2 => VertexComponentType.Short,
            VertexFormat.Short3 => VertexComponentType.Short,
            VertexFormat.Short4 => VertexComponentType.Short,
            VertexFormat.UShort2Normalized => VertexComponentType.UnsignedShortNormalized,
            VertexFormat.UShort3Normalized => VertexComponentType.UnsignedShortNormalized,
            VertexFormat.UShort4Normalized => VertexComponentType.UnsignedShortNormalized,
            VertexFormat.Short2Normalized => VertexComponentType.ShortNormalized,
            VertexFormat.Short3Normalized => VertexComponentType.ShortNormalized,
            VertexFormat.Short4Normalized => VertexComponentType.ShortNormalized,
            VertexFormat.Half2 => VertexComponentType.Half,
            VertexFormat.Half3 => VertexComponentType.Half,
            VertexFormat.Half4 => VertexComponentType.Half,
            VertexFormat.Float => VertexComponentType.Float,
            VertexFormat.Float2 => VertexComponentType.Float,
            VertexFormat.Float3 => VertexComponentType.Float,
            VertexFormat.Float4 => VertexComponentType.Float,
            VertexFormat.Int => VertexComponentType.Int,
            VertexFormat.Int2 => VertexComponentType.Int,
            VertexFormat.Int3 => VertexComponentType.Int,
            VertexFormat.Int4 => VertexComponentType.Int,
            VertexFormat.UInt => VertexComponentType.UnsignedInt,
            VertexFormat.UInt2 => VertexComponentType.UnsignedInt,
            VertexFormat.UInt3 => VertexComponentType.UnsignedInt,
            VertexFormat.UInt4 => VertexComponentType.UnsignedInt,
            VertexFormat.Int1010102Normalized => VertexComponentType.PackedIntNormalized,
            VertexFormat.UInt1010102Normalized => VertexComponentType.PackedUnsignedIntNormalized,
            VertexFormat.UChar4Normalized_BGRA => VertexComponentType.CharNormalized,
            VertexFormat.UChar => VertexComponentType.UnsignedChar,
            VertexFormat.Char => VertexComponentType.Char,
            VertexFormat.UCharNormalized => VertexComponentType.UnsignedCharNormalized,
            VertexFormat.CharNormalized => VertexComponentType.CharNormalized,
            VertexFormat.UShort => VertexComponentType.UnsignedShort,
            VertexFormat.Short => VertexComponentType.Short,
            VertexFormat.UShortNormalized => VertexComponentType.UnsignedShortNormalized,
            VertexFormat.ShortNormalized => VertexComponentType.ShortNormalized,
            VertexFormat.Half => VertexComponentType.Half,
            _ => throw new ArgumentOutOfRangeException(nameof(format), format, null)
        };
    }

    public static int GetComponentCount(this VertexFormat format)
    {
        return format switch {
            //
            // 1 Component formats
            //
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

            //
            // 2 component formats
            //
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

            //
            // 3 Comonent formats
            //
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

            //
            // Four component formats
            //
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
            VertexFormat.Int1010102Normalized => 4,
            VertexFormat.UInt1010102Normalized => 4, 
            VertexFormat.UChar4Normalized_BGRA => 4,

            VertexFormat.Invalid => throw new ArgumentOutOfRangeException(null, nameof(format)),
            _ => throw new ArgumentOutOfRangeException()
        };
    }
}