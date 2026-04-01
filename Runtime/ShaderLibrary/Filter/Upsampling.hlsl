// ============================================================================
// Rayforge Unity Library Core - Bilateral Filter Shader Include
// Author: Matthew
// Description: pipeline independant HLSL blur functions
// ============================================================================

// ============================================================================
// 1. Includes
// ============================================================================

#include "../Common.hlsl"
#include "../Rendering/Uv.hlsl"
#include "../Sampling.hlsl"

// ============================================================================
// 2. Constants
// ============================================================================

// Classic Cross (+)
static const float2 OffsetsCross[5] = {
    float2(0, 0),
    float2(1, 0), float2(-1, 0),
    float2(0, 1), float2(0, -1)
};

// Diagonal (X)
static const float2 OffsetsDiagonal[5] = {
    float2(0, 0),
    float2(1, 1), float2(-1, -1),
    float2(1, -1), float2(-1, 1)
};

// Star (9-Tap)
static const float2 OffsetsStar[9] = {
    float2(0, 0),
    float2(1, 0), float2(-1, 0), float2(0, 1), float2(0, -1), // Cross
    float2(1, 1), float2(-1, -1), float2(1, -1), float2(-1, 1) // Diagonal
};

// ============================================================================
// 4. Utility Functions
// ============================================================================

/**
 * @brief Core logic for bilateral upsampling and filtering.
 * Processes a variable number of samples comparing depth differences to preserve edges.
 * * @param ARGS_MACRO     Texture argument macro (TEXTURE2D_X_ARGS or TEXTURE2D_ARGS).
 * @param SAMPLE_MACRO   Texture sampling macro (SAMPLE_TEXTURE2D_X_LOD or SAMPLE_TEXTURE2D_LOD).
 * @param srcTex         The low-resolution source texture to be filtered.
 * @param srcSmp         Sampler state for the source texture.
 * @param lowDepth       The low-resolution depth buffer matching the source texture.
 * @param lowDepthSmp    Sampler state for the low-res depth (should be Point Clamp).
 * @param fullDepth      The full-resolution depth buffer used as a reference.
 * @param fullDepthSmp   Sampler state for the full-res depth (should be Point Clamp).
 * @param uv             Current pixel UV coordinates.
 * @param offsets        Array of float2 offsets for the sampling kernel (e.g., OffsetsStar).
 * @param texSize        Texel size of the low-resolution source (xy = 1/width, 1/height).
 * @param falloff        Depth sensitivity factor. Higher values preserve edges more strictly.
 * @param COUNT          The number of samples in the offset array (e.g., 5 or 9).
 */
#if !defined(SAMPLER_P_C)
    #define SAMPLER_P_C sampler_PointClamp
#endif
#if !defined(SAMPLER_L_C)
    #define SAMPLER_L_C sampler_LinearClamp
#endif

// ============================================================================
// 1. Core Logic Macro
// ============================================================================

#define CORE_BILATERAL_UPSAMPLE_LOGIC(TYPE, SAMPLE_SRC_FUNC, SAMPLE_DEPTH_FUNC, COUNT) \
    float rawRefDepth = SAMPLE_DEPTH_FUNC(fDepth, SAMPLER_P_C, uv); \
    float referenceDepth = LinearEyeDepth(rawRefDepth, _ZBufferParams); \
    float finalFalloff = GetFinalFalloff(referenceDepth, falloff); \
    \
    TYPE bilinearFallback = (TYPE)SAMPLE_SRC_FUNC(src, SAMPLER_L_C, uv); \
    \
    TYPE combinedColor = (TYPE)0; \
    float combinedWeight = 0; \
    \
    [unroll] \
    for(int i = 0; i < COUNT; i++) { \
        float2 sampleUV = uv + offsets[i] * texSize.xy; \
        \
        float rawSampleDepth = SAMPLE_DEPTH_FUNC(lDepth, SAMPLER_P_C, sampleUV); \
        float sampleDepth = LinearEyeDepth(rawSampleDepth, _ZBufferParams); \
        \
        TYPE sampleData = (TYPE)SAMPLE_SRC_FUNC(src, srcSmp, sampleUV); \
        \
        float w = GetBilateralWeight(referenceDepth, sampleDepth, finalFalloff); \
        \
        float distSq = dot(offsets[i], offsets[i]); \
        float spatial = exp(-distSq * 0.5); \
        if(i == 0) spatial *= 2.0; \
        \
        float finalW = w * spatial; \
        combinedColor += sampleData * finalW; \
        combinedWeight += finalW; \
    } \
    \
    float confidence = saturate(combinedWeight * 10.0); \
    TYPE upsampledResult = combinedColor / max(combinedWeight, 0.00001); \
    \
    return lerp(bilinearFallback, upsampledResult, (TYPE)confidence);

#if !defined(SAMPLE_DEPTH_XR)
    #define SAMPLE_DEPTH_XR(tex, smp, uv) SAMPLE_TEXTURE2D_X_LOD(tex, smp, uv, 0).r
#endif

#if !defined(SAMPLE_SRC_XR)
    #define SAMPLE_SRC_XR(tex, smp, uv)   SAMPLE_TEXTURE2D_X_LOD(tex, smp, uv, 0)
#endif

#if !defined(SAMPLE_DEPTH)
    #define SAMPLE_DEPTH(tex, smp, uv) SAMPLE_TEXTURE2D_LOD(tex, smp, uv, 0).r
#endif

#if !defined(SAMPLE_SRC)
    #define SAMPLE_SRC(tex, smp, uv)   SAMPLE_TEXTURE2D_LOD(tex, smp, uv, 0)
#endif

// ============================================================================
// 2. Execution Layer (Internal)
// ============================================================================

// --- 5-Tap (XR & Standard) ---
float4 ExecuteBilateralFilter5XR(TEXTURE2D_X_PARAM( src, srcSmp), TEXTURE2D_X(lDepth), TEXTURE2D_X(fDepth), float2 texSize, float2 uv, float2 offsets[5], float falloff)
{
    CORE_BILATERAL_UPSAMPLE_LOGIC(float4, SAMPLE_SRC_XR, SAMPLE_DEPTH_XR, 5)
}

float4 ExecuteBilateralFilter5(TEXTURE2D_PARAM( src, srcSmp), TEXTURE2D(lDepth), TEXTURE2D(fDepth), float2 texSize, float2 uv, float2 offsets[5], float falloff)
{
    CORE_BILATERAL_UPSAMPLE_LOGIC(float4, SAMPLE_SRC, SAMPLE_DEPTH, 5)
}

// --- 9-Tap (XR & Standard) ---
float4 ExecuteBilateralFilter9XR(TEXTURE2D_X_PARAM( src, srcSmp), TEXTURE2D_X(lDepth), TEXTURE2D_X(fDepth), float2 texSize, float2 uv, float2 offsets[9], float falloff)
{
    CORE_BILATERAL_UPSAMPLE_LOGIC(float4, SAMPLE_SRC_XR, SAMPLE_DEPTH_XR, 9)
}

float4 ExecuteBilateralFilter9(TEXTURE2D_PARAM( src, srcSmp), TEXTURE2D(lDepth), TEXTURE2D(fDepth), float2 texSize, float2 uv, float2 offsets[9], float falloff)
{
    CORE_BILATERAL_UPSAMPLE_LOGIC(float4, SAMPLE_SRC, SAMPLE_DEPTH, 9)
}

// ============================================================================
// 3. Convenience Wrappers (Auto-Sampler)
// ============================================================================

// XR Wrappers
float4 ExecuteBilateralFilter5XR(TEXTURE2D_X( src), TEXTURE2D_X(lDepth), TEXTURE2D_X(fDepth), float2 texSize, float2 uv, float2 offsets[5], float falloff)
{
    return ExecuteBilateralFilter5XR(TEXTURE2D_X_ARGS(src, SAMPLER_L_C), lDepth, fDepth, texSize, uv, offsets, falloff);
}

float4 ExecuteBilateralFilter9XR(TEXTURE2D_X( src), TEXTURE2D_X(lDepth), TEXTURE2D_X(fDepth), float2 texSize, float2 uv, float2 offsets[9], float falloff)
{
    return ExecuteBilateralFilter9XR(TEXTURE2D_X_ARGS(src, SAMPLER_L_C), lDepth, fDepth, texSize, uv, offsets, falloff);
}

// Standard Wrappers
float4 ExecuteBilateralFilter5(TEXTURE2D( src), TEXTURE2D(lDepth), TEXTURE2D(fDepth), float2 texSize, float2 uv, float2 offsets[5], float falloff)
{
    return ExecuteBilateralFilter5(TEXTURE2D_ARGS(src, SAMPLER_L_C), lDepth, fDepth, texSize, uv, offsets, falloff);
}

float4 ExecuteBilateralFilter9(TEXTURE2D( src), TEXTURE2D(lDepth), TEXTURE2D(fDepth), float2 texSize, float2 uv, float2 offsets[9], float falloff)
{
    return ExecuteBilateralFilter9(TEXTURE2D_ARGS(src, SAMPLER_L_C), lDepth, fDepth, texSize, uv, offsets, falloff);
}

// ============================================================================
// 4. Public API
// ============================================================================

// --- XR Star / Cross / Diagonal ---
float4 UpsampleBilateralStar9XR(TEXTURE2D_X( src), TEXTURE2D_X(lDepth), TEXTURE2D_X(fDepth), float2 texSize, float2 uv, float falloff)
{
    return ExecuteBilateralFilter9XR(src, lDepth, fDepth, texSize, uv, OffsetsStar, falloff);
}

float4 UpsampleBilateralCross5XR(TEXTURE2D_X( src), TEXTURE2D_X(lDepth), TEXTURE2D_X(fDepth), float2 texSize, float2 uv, float falloff)
{
    return ExecuteBilateralFilter5XR(src, lDepth, fDepth, texSize, uv, OffsetsCross, falloff);
}

float4 UpsampleBilateralDiagonal5XR(TEXTURE2D_X( src), TEXTURE2D_X(lDepth), TEXTURE2D_X(fDepth), float2 texSize, float2 uv, float falloff)
{
    return ExecuteBilateralFilter5XR(src, lDepth, fDepth, texSize, uv, OffsetsDiagonal, falloff);
}

// --- Standard Star / Cross / Diagonal ---
float4 UpsampleBilateralStar9(TEXTURE2D( src), TEXTURE2D(lDepth), TEXTURE2D(fDepth), float2 texSize, float2 uv, float falloff)
{
    return ExecuteBilateralFilter9(src, lDepth, fDepth, texSize, uv, OffsetsStar, falloff);
}

float4 UpsampleBilateralCross5(TEXTURE2D( src), TEXTURE2D(lDepth), TEXTURE2D(fDepth), float2 texSize, float2 uv, float falloff)
{
    return ExecuteBilateralFilter5(src, lDepth, fDepth, texSize, uv, OffsetsCross, falloff);
}

float4 UpsampleBilateralDiagonal5(TEXTURE2D( src), TEXTURE2D(lDepth), TEXTURE2D(fDepth), float2 texSize, float2 uv, float falloff)
{
    return ExecuteBilateralFilter5(src, lDepth, fDepth, texSize, uv, OffsetsDiagonal, falloff);
}