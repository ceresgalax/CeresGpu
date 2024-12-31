namespace CeresGpu.Graphics
{
    /// <summary>
    /// Char/Short/Int are unsigned integer values.
    /// UChar/Short/Int are signed integer values.
    /// Integer values with Normalized are interpreted as values between 0 and 1 for unsigned integers,
    /// or -1 to 1 for signed integers.
    /// </summary>
    public enum VertexFormat
    {
        Invalid = 0,
        UChar2 = 1,
        UChar3 = 2,
        UChar4 = 3,
        Char2 = 4,
        Char3 = 5,
        Char4 = 6,
        UChar2Normalized = 7,
        UChar3Normalized = 8,
        UChar4Normalized = 9,
        Char2Normalized = 10,
        Char3Normalized = 11,
        Char4Normalized = 12,
        UShort2 = 13,
        UShort3 = 14,
        UShort4 = 15,
        Short2 = 16,
        Short3 = 17,
        Short4 = 18,
        UShort2Normalized = 19,
        UShort3Normalized = 20,
        UShort4Normalized = 21,
        Short2Normalized = 22,
        Short3Normalized = 23,
        Short4Normalized = 24,
        Half2 = 25,
        Half3 = 26,
        Half4 = 27,
        Float = 28,
        Float2 = 29,
        Float3 = 30,
        Float4 = 31,
        Int = 32,
        Int2 = 33,
        Int3 = 34,
        Int4 = 35,
        UInt = 36,
        UInt2 = 37,
        UInt3 = 38,
        UInt4 = 39,
        
        /// <summary>
        /// One packed 32-bit value with four normalized signed two's complement integer values, arranged as 10 bits, 10 bits, 10 bits, and 2 bits.
        /// </summary>
        Int1010102Normalized = 40,
        
        /// <summary>
        /// ne packed 32-bit value with four normalized unsigned integer values, arranged as 10 bits, 10 bits, 10 bits, and 2 bits.
        /// </summary>
        UInt1010102Normalized = 41,
        
        UChar4Normalized_BGRA = 42,
        UChar = 45,
        Char = 46,
        UCharNormalized = 47,
        CharNormalized = 48,
        UShort = 49,
        Short = 50,
        UShortNormalized = 51,
        ShortNormalized = 52,
        Half = 53,
    }
}