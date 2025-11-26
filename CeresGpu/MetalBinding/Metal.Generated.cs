using System;
using System.CodeDom.Compiler;
using System.Runtime.InteropServices;

namespace CeresGpu.MetalBinding
{
    [GeneratedCode("genmetal.py", "0")]
    public class MetalApi
    {
        private const string DLL_NAME = "metalbinding";
        
        [DllImport(DLL_NAME)]
        public static extern IntPtr metalbinding_create(IntPtr window, uint frameCount);
        
        [DllImport(DLL_NAME)]
        public static extern void metalbinding_destroy(IntPtr context);
        
        [DllImport(DLL_NAME)]
        public static extern uint metalbinding_get_last_error_length(IntPtr context);
        
        [DllImport(DLL_NAME)]
        public static extern void metalbinding_get_last_error(IntPtr context, IntPtr outUtf8Text, uint length);
        
        [DllImport(DLL_NAME)]
        public static extern void metalbinding_capture(IntPtr context);
        
        [DllImport(DLL_NAME)]
        public static extern void metalbinding_stop_capture(IntPtr context);
        
        [DllImport(DLL_NAME)]
        public static extern void metalbinding_set_content_scale(IntPtr context, float scale, uint drawableWidth, uint drawableHeight);
        
        [DllImport(DLL_NAME)]
        public static extern IntPtr metalbinding_get_current_frame_drawable_texture(IntPtr context);
        
        [DllImport(DLL_NAME)]
        public static extern IntPtr metalbinding_create_render_pass_descriptor();
        
        [DllImport(DLL_NAME)]
        public static extern void metalbinding_set_render_pass_descriptor_color_attachment(IntPtr descriptor, uint colorAttachmentIndex, IntPtr texture, MTLLoadAction loadAction, MTLStoreAction storeAction, double clearR, double clearG, double clearB, double clearA);
        
        [DllImport(DLL_NAME)]
        public static extern void metalbinding_set_render_pass_descriptor_depth_attachment(IntPtr descriptor, IntPtr texture, MTLLoadAction loadAction, MTLStoreAction storeAction, double clearDepth);
        
        [DllImport(DLL_NAME)]
        public static extern void metalbinding_set_render_pass_descriptor_stencil_attachment(IntPtr descriptor, IntPtr texture, MTLLoadAction loadAction, MTLStoreAction storeAction, uint clearStencil);
        
        [DllImport(DLL_NAME)]
        public static extern void metalbinding_release_render_pass_descriptor(IntPtr rpd);
        
        [DllImport(DLL_NAME)]
        public static extern void metalbinding_acquire_drawable(IntPtr context);
        
        [DllImport(DLL_NAME)]
        public static extern IntPtr metalbinding_create_command_buffer(IntPtr context);
        
        [DllImport(DLL_NAME)]
        public static extern void metalbinding_release_command_buffer(IntPtr commandBuffer);
        
        [DllImport(DLL_NAME)]
        public static extern void metalbinding_present_current_frame_after_minimum_duration(IntPtr context, IntPtr commandBuffer, double seconds);
        
        [DllImport(DLL_NAME)]
        public static extern void metalbinding_commit_command_buffer(IntPtr commandBuffer);
        
        [DllImport(DLL_NAME)]
        public static extern IntPtr metalbinding_new_command_encoder(IntPtr commandBuffer, IntPtr passDescriptor);
        
        [DllImport(DLL_NAME)]
        public static extern void metalbinding_release_command_encoder(IntPtr encoder);
        
        [DllImport(DLL_NAME)]
        public static extern void metalbinding_command_encoder_end_encoding(IntPtr encoder);
        
        [DllImport(DLL_NAME)]
        public static extern void metalbinding_command_encoder_set_pipeline(IntPtr encoder, IntPtr pipeline);
        
        [DllImport(DLL_NAME)]
        public static extern void metalbinding_command_encoder_set_scissor(IntPtr encoder, int x, int y, uint w, uint h);
        
        [DllImport(DLL_NAME)]
        public static extern void metalbinding_command_encoder_set_viewport(IntPtr encoder, double x, double y, double w, double h);
        
        [DllImport(DLL_NAME)]
        public static extern void metalbinding_command_encoder_set_cull_mode(IntPtr encoder, MTLCullMode cullMode);
        
        [DllImport(DLL_NAME)]
        public static extern void metalbinding_command_encoder_set_dss(IntPtr encoder, IntPtr dss);
        
        [DllImport(DLL_NAME)]
        public static extern void metalbinding_command_encoder_set_vertex_buffer(IntPtr encoder, IntPtr buffer, uint offset, uint index);
        
        [DllImport(DLL_NAME)]
        public static extern void metalbinding_command_encoder_set_fragment_buffer(IntPtr encoder, IntPtr buffer, uint offset, uint index);
        
        [DllImport(DLL_NAME)]
        public static extern void metalbinding_command_encoder_draw(IntPtr encoder, uint vertexCount, uint instanceCount, uint firstVertex, uint firstInstance);
        
        [DllImport(DLL_NAME)]
        public static extern void metalbinding_command_encoder_draw_indexed(IntPtr encoder, MTLIndexType indexType, IntPtr indexBuffer, uint indexCount, uint instanceCount, uint indexBufferOffset, int vertexOffset, uint firstInstance);
        
        [DllImport(DLL_NAME)]
        public static extern IntPtr metalbinding_new_buffer(IntPtr context, uint length);
        
        [DllImport(DLL_NAME)]
        public static extern void metalbinding_release_buffer(IntPtr buffer);
        
        [DllImport(DLL_NAME)]
        public static extern void metalbinding_copy_to_buffer(IntPtr buffer, IntPtr source, uint offset, uint size);
        
        [DllImport(DLL_NAME)]
        public static extern void metalbinding_buffer_did_modify_range(IntPtr buffer, uint offset, uint size);
        
        [DllImport(DLL_NAME)]
        public static extern IntPtr metalbinding_buffer_get_contents(IntPtr buffer);
        
        [DllImport(DLL_NAME)]
        public static extern IntPtr metalbinding_new_texture(IntPtr context, uint width, uint height, MTLPixelFormat format);
        
        [DllImport(DLL_NAME)]
        public static extern void metalbinding_release_texture(IntPtr texture);
        
        [DllImport(DLL_NAME)]
        public static extern void metalbinding_set_texture_data(IntPtr texture, uint width, uint height, IntPtr data, uint bytesPerRow);
        
        [DllImport(DLL_NAME)]
        public static extern void metalbinding_get_texture_info(IntPtr texture, ref uint ref_width, ref uint ref_height, ref MTLPixelFormat ref_format);
        
        [DllImport(DLL_NAME)]
        public static extern IntPtr metalbinding_new_library(IntPtr context, string utf8Source);
        
        [DllImport(DLL_NAME)]
        public static extern void metalbinding_release_library(IntPtr library);
        
        [DllImport(DLL_NAME)]
        public static extern IntPtr metalbinding_new_function(IntPtr library, string utf8Name);
        
        [DllImport(DLL_NAME)]
        public static extern void metalbinding_release_function(IntPtr function);
        
        [DllImport(DLL_NAME)]
        public static extern IntPtr metalbinding_new_rpd(IntPtr context);
        
        [DllImport(DLL_NAME)]
        public static extern void metalbinding_release_rpd(IntPtr descriptor);
        
        [DllImport(DLL_NAME)]
        public static extern void metalbinding_set_rpd_functions(IntPtr descriptor, IntPtr vertex, IntPtr fragment);
        
        [DllImport(DLL_NAME)]
        public static extern void metalbinding_set_rpd_common(IntPtr descriptor, bool blend, MTLBlendOperation colorBlendOp, MTLBlendOperation alphaBlendOp, MTLBlendFactor sourceRgb, MTLBlendFactor destRgb, MTLBlendFactor sourceAlpha, MTLBlendFactor destAlpha);
        
        [DllImport(DLL_NAME)]
        public static extern void metalbinding_set_rpd_vertex_descriptor(IntPtr descriptor, IntPtr vertexDescriptor);
        
        [DllImport(DLL_NAME)]
        public static extern IntPtr metalbinding_new_pipeline_state(IntPtr context, IntPtr descriptor);
        
        [DllImport(DLL_NAME)]
        public static extern void metalbinding_release_pipeline_state(IntPtr state);
        
        [DllImport(DLL_NAME)]
        public static extern IntPtr metalbinding_new_vertex_descriptor(IntPtr context);
        
        [DllImport(DLL_NAME)]
        public static extern void metalbinding_release_vertex_descriptor(IntPtr descriptor);
        
        [DllImport(DLL_NAME)]
        public static extern void metalbinding_set_vertex_descriptor_vad(IntPtr descriptor, uint index, MTLVertexFormat format, uint offset, uint bufferIndex);
        
        [DllImport(DLL_NAME)]
        public static extern void metalbinding_set_vertex_descriptor_vbl(IntPtr descriptor, uint index, MTLVertexStepFunction stepFunction, uint stride);
        
        [DllImport(DLL_NAME)]
        public static extern IntPtr metalbinding_new_dsd(MTLCompareFunction depthCompareFunc, bool depthWriteEnabled, IntPtr backFaceStencil, IntPtr frontFaceStencil);
        
        [DllImport(DLL_NAME)]
        public static extern void metalbinding_release_dsd(IntPtr dsd);
        
        [DllImport(DLL_NAME)]
        public static extern IntPtr metalbinding_new_stencil_descriptor(IntPtr context, MTLStencilOperation stencilFailOp, MTLStencilOperation depthFailOp, MTLStencilOperation passOp, MTLCompareFunction stencilCompareFunc, uint readMask, uint writeMask);
        
        [DllImport(DLL_NAME)]
        public static extern void metalbinding_release_stencil_descriptor(IntPtr descriptor);
        
        [DllImport(DLL_NAME)]
        public static extern IntPtr metalbinding_new_depth_stencil_state(IntPtr context, IntPtr descriptor);
        
        [DllImport(DLL_NAME)]
        public static extern void metalbinding_release_depth_stencil_state(IntPtr state);
        
        [DllImport(DLL_NAME)]
        public static extern IntPtr metalbinding_new_argument_encoder(IntPtr function, uint index);
        
        [DllImport(DLL_NAME)]
        public static extern void metalbinding_release_argument_encoder(IntPtr encoder);
        
        [DllImport(DLL_NAME)]
        public static extern uint metalbinding_get_argument_buffer_size(IntPtr encoder);
        
        [DllImport(DLL_NAME)]
        public static extern void metalbinding_set_argument_buffer(IntPtr encoder, IntPtr buffer);
        
        [DllImport(DLL_NAME)]
        public static extern void metalbinding_encode_buffer_argument(IntPtr encoder, IntPtr commandEncoder, IntPtr buffer, uint offset, uint index, uint stages);
        
        [DllImport(DLL_NAME)]
        public static extern void metalbinding_encode_texture_argument(IntPtr encoder, IntPtr commandEncoder, IntPtr texture, uint index, uint stages);
        
        [DllImport(DLL_NAME)]
        public static extern void metalbinding_encode_sampler_argument(IntPtr encoder, IntPtr sampler, uint index);
        
        [DllImport(DLL_NAME)]
        public static extern IntPtr metalbinding_create_sampler(IntPtr context, MTLSamplerMinMagFilter min, MTLSamplerMinMagFilter mag, MTLSamplerMipFilter mip, MTLSamplerAddressMode rAddressMode, MTLSamplerAddressMode sAddressMode, MTLSamplerAddressMode tAddressMode, bool normalizedCoordinates, bool supportArgumentBuffers);
        
        [DllImport(DLL_NAME)]
        public static extern void metalbinding_release_sampler(IntPtr sampler);
        
        [DllImport(DLL_NAME)]
        public static extern void metalbinding_get_memory_info(IntPtr context, ref ulong ref_current_allocated_size, ref ulong ref_recommended_working_set_size, ref ulong ref_has_unified_memory, ref ulong ref_max_transfer_rate);
        
        [DllImport(DLL_NAME)]
        public static extern void metalbinding_arp_deinit(IntPtr context);
        
        [DllImport(DLL_NAME)]
        public static extern void metalbinding_arp_drain(IntPtr context);
        
        public enum MTLBlendFactor : ulong
        {
            Zero = 0,
            One = 1,
            SourceColor = 2,
            OneMinusSourceColor = 3,
            SourceAlpha = 4,
            OneMinusSourceAlpha = 5,
            DestinationColor = 6,
            OneMinusDestinationColor = 7,
            DestinationAlpha = 8,
            OneMinusDestinationAlpha = 9,
            SourceAlphaSaturated = 10,
            BlendColor = 11,
            OneMinusBlendColor = 12,
            BlendAlpha = 13,
            OneMinusBlendAlpha = 14,
            Source1Color = 15,
            OneMinusSource1Color = 16,
            Source1Alpha = 17,
            OneMinusSource1Alpha = 18,
            Unspecialized = 19,
        }
        
        public enum MTLBlendOperation : ulong
        {
            Add = 0,
            Subtract = 1,
            ReverseSubtract = 2,
            Min = 3,
            Max = 4,
            Unspecialized = 5,
        }
        
        public enum MTLCompareFunction : ulong
        {
            Never = 0,
            Less = 1,
            Equal = 2,
            LessEqual = 3,
            Greater = 4,
            NotEqual = 5,
            GreaterEqual = 6,
            Always = 7,
        }
        
        public enum MTLCullMode : ulong
        {
            None = 0,
            Front = 1,
            Back = 2,
        }
        
        public enum MTLIndexType : ulong
        {
            UInt16 = 0,
            UInt32 = 1,
        }
        
        public enum MTLLoadAction : ulong
        {
            DontCare = 0,
            Load = 1,
            Clear = 2,
        }
        
        public enum MTLPixelFormat : ulong
        {
            Invalid = 0,
            A8Unorm = 1,
            R8Unorm = 10,
            R8Unorm_sRGB = 11,
            R8Snorm = 12,
            R8Uint = 13,
            R8Sint = 14,
            R16Unorm = 20,
            R16Snorm = 22,
            R16Uint = 23,
            R16Sint = 24,
            R16Float = 25,
            RG8Unorm = 30,
            RG8Unorm_sRGB = 31,
            RG8Snorm = 32,
            RG8Uint = 33,
            RG8Sint = 34,
            B5G6R5Unorm = 40,
            A1BGR5Unorm = 41,
            ABGR4Unorm = 42,
            BGR5A1Unorm = 43,
            R32Uint = 53,
            R32Sint = 54,
            R32Float = 55,
            RG16Unorm = 60,
            RG16Snorm = 62,
            RG16Uint = 63,
            RG16Sint = 64,
            RG16Float = 65,
            RGBA8Unorm = 70,
            RGBA8Unorm_sRGB = 71,
            RGBA8Snorm = 72,
            RGBA8Uint = 73,
            RGBA8Sint = 74,
            BGRA8Unorm = 80,
            BGRA8Unorm_sRGB = 81,
            RGB10A2Unorm = 90,
            RGB10A2Uint = 91,
            RG11B10Float = 92,
            RGB9E5Float = 93,
            BGR10A2Unorm = 94,
            BGR10_XR = 554,
            BGR10_XR_sRGB = 555,
            RG32Uint = 103,
            RG32Sint = 104,
            RG32Float = 105,
            RGBA16Unorm = 110,
            RGBA16Snorm = 112,
            RGBA16Uint = 113,
            RGBA16Sint = 114,
            RGBA16Float = 115,
            BGRA10_XR = 552,
            BGRA10_XR_sRGB = 553,
            RGBA32Uint = 123,
            RGBA32Sint = 124,
            RGBA32Float = 125,
            BC1_RGBA = 130,
            BC1_RGBA_sRGB = 131,
            BC2_RGBA = 132,
            BC2_RGBA_sRGB = 133,
            BC3_RGBA = 134,
            BC3_RGBA_sRGB = 135,
            BC4_RUnorm = 140,
            BC4_RSnorm = 141,
            BC5_RGUnorm = 142,
            BC5_RGSnorm = 143,
            BC6H_RGBFloat = 150,
            BC6H_RGBUfloat = 151,
            BC7_RGBAUnorm = 152,
            BC7_RGBAUnorm_sRGB = 153,
            PVRTC_RGB_2BPP = 160,
            PVRTC_RGB_2BPP_sRGB = 161,
            PVRTC_RGB_4BPP = 162,
            PVRTC_RGB_4BPP_sRGB = 163,
            PVRTC_RGBA_2BPP = 164,
            PVRTC_RGBA_2BPP_sRGB = 165,
            PVRTC_RGBA_4BPP = 166,
            PVRTC_RGBA_4BPP_sRGB = 167,
            EAC_R11Unorm = 170,
            EAC_R11Snorm = 172,
            EAC_RG11Unorm = 174,
            EAC_RG11Snorm = 176,
            EAC_RGBA8 = 178,
            EAC_RGBA8_sRGB = 179,
            ETC2_RGB8 = 180,
            ETC2_RGB8_sRGB = 181,
            ETC2_RGB8A1 = 182,
            ETC2_RGB8A1_sRGB = 183,
            ASTC_4x4_sRGB = 186,
            ASTC_5x4_sRGB = 187,
            ASTC_5x5_sRGB = 188,
            ASTC_6x5_sRGB = 189,
            ASTC_6x6_sRGB = 190,
            ASTC_8x5_sRGB = 192,
            ASTC_8x6_sRGB = 193,
            ASTC_8x8_sRGB = 194,
            ASTC_10x5_sRGB = 195,
            ASTC_10x6_sRGB = 196,
            ASTC_10x8_sRGB = 197,
            ASTC_10x10_sRGB = 198,
            ASTC_12x10_sRGB = 199,
            ASTC_12x12_sRGB = 200,
            ASTC_4x4_LDR = 204,
            ASTC_5x4_LDR = 205,
            ASTC_5x5_LDR = 206,
            ASTC_6x5_LDR = 207,
            ASTC_6x6_LDR = 208,
            ASTC_8x5_LDR = 210,
            ASTC_8x6_LDR = 211,
            ASTC_8x8_LDR = 212,
            ASTC_10x5_LDR = 213,
            ASTC_10x6_LDR = 214,
            ASTC_10x8_LDR = 215,
            ASTC_10x10_LDR = 216,
            ASTC_12x10_LDR = 217,
            ASTC_12x12_LDR = 218,
            ASTC_4x4_HDR = 222,
            ASTC_5x4_HDR = 223,
            ASTC_5x5_HDR = 224,
            ASTC_6x5_HDR = 225,
            ASTC_6x6_HDR = 226,
            ASTC_8x5_HDR = 228,
            ASTC_8x6_HDR = 229,
            ASTC_8x8_HDR = 230,
            ASTC_10x5_HDR = 231,
            ASTC_10x6_HDR = 232,
            ASTC_10x8_HDR = 233,
            ASTC_10x10_HDR = 234,
            ASTC_12x10_HDR = 235,
            ASTC_12x12_HDR = 236,
            GBGR422 = 240,
            BGRG422 = 241,
            Depth16Unorm = 250,
            Depth32Float = 252,
            Stencil8 = 253,
            Depth24Unorm_Stencil8 = 255,
            Depth32Float_Stencil8 = 260,
            X32_Stencil8 = 261,
            X24_Stencil8 = 262,
            Unspecialized = 263,
        }
        
        public enum MTLSamplerAddressMode : ulong
        {
            ClampToEdge = 0,
            MirrorClampToEdge = 1,
            Repeat = 2,
            MirrorRepeat = 3,
            ClampToZero = 4,
            ClampToBorderColor = 5,
        }
        
        public enum MTLSamplerMinMagFilter : ulong
        {
            Nearest = 0,
            Linear = 1,
        }
        
        public enum MTLSamplerMipFilter : ulong
        {
            NotMipmapped = 0,
            Nearest = 1,
            Linear = 2,
        }
        
        public enum MTLStencilOperation : ulong
        {
            Keep = 0,
            Zero = 1,
            Replace = 2,
            IncrementClamp = 3,
            DecrementClamp = 4,
            Invert = 5,
            IncrementWrap = 6,
            DecrementWrap = 7,
        }
        
        public enum MTLStoreAction : ulong
        {
            DontCare = 0,
            Store = 1,
            MultisampleResolve = 2,
            StoreAndMultisampleResolve = 3,
            Unknown = 4,
            CustomSampleDepthStore = 5,
        }
        
        public enum MTLVertexFormat : ulong
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
            Int1010102Normalized = 40,
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
            FloatRG11B10 = 54,
            FloatRGB9E5 = 55,
        }
        
        public enum MTLVertexStepFunction : ulong
        {
            Constant = 0,
            PerVertex = 1,
            PerInstance = 2,
            PerPatch = 3,
            PerPatchControlPoint = 4,
        }
        
    }
}
