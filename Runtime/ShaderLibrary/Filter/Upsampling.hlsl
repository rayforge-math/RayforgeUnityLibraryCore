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
#define SAMPLER_P_C sampler_PointClamp

#if defined(UPSAMPLE_ADAPTIVE)
    #if !defined(UPSAMPLE_ADAPTIVE_BIAS)
        #define ADAPTIVE_BIAS 0.1
    #endif

    #define BIL_UP_ADAPTIVE_FALLOFF(refDepth, baseFalloff) \
        (baseFalloff * (1.0 / (refDepth * ADAPTIVE_BIAS + 0.1)))
#else
    #define BIL_UP_ADAPTIVE_FALLOFF(refDepth, baseFalloff) (baseFalloff)
#endif

#define CORE_BILATERAL_UPSAMPLE_LOGIC(SAMPLE_MACRO, COUNT) \
    float rawRefDepth = SAMPLE_MACRO(fDepth, SAMPLER_P_C, uv, 0).r; \
    float referenceDepth = LinearEyeDepth(rawRefDepth, _ZBufferParams); \
    \
    /* use partial derivatives for gradient (rate of change) */ \
    float depthGradient = fwidth(referenceDepth); \
    float edgeStrictness = 1.0 + saturate(depthGradient); \
    \
    float finalFalloff = pow(abs(falloff), edgeStrictness); \
    finalFalloff = BIL_UP_ADAPTIVE_FALLOFF(referenceDepth, finalFalloff); \
    \
    float4 combinedColor = 0; \
    float combinedWeight = 0; \
    \
    [unroll] \
    for(int i = 0; i < COUNT; i++) { \
        float2 sampleUV = uv + offsets[i] * texSize.xy; \
        float rawSampleDepth = SAMPLE_MACRO(lDepth, SAMPLER_P_C, sampleUV, 0).r; \
        float sampleDepth = LinearEyeDepth(rawSampleDepth, _ZBufferParams); \
        \
        float4 sampleColor = SAMPLE_MACRO(src, srcSmp, sampleUV, 0); \
        float depthDiff = sampleDepth - referenceDepth; \
        \
        float w = (1.0 / (abs(depthDiff) * finalFalloff + 0.1) * 0.1); \
        /*if (depthDiff < 0) w *= 0.0001;*/ \
        \
        float spatial = (i == 0) ? 2.0 : 1.0; \
        combinedColor += sampleColor * (w * spatial); \
        combinedWeight += (w * spatial); \
    } \
    return combinedColor / (combinedWeight + 0.00001);

float4 ExecuteBilateralFilter5XR(TEXTURE2D_X_PARAM(src, srcSmp), TEXTURE2D_X(lDepth), TEXTURE2D_X(fDepth), float2 texSize, float2 uv, float2 offsets[5], float falloff)
{
    CORE_BILATERAL_UPSAMPLE_LOGIC(SAMPLE_TEXTURE2D_X_LOD, 5)
}

float4 ExecuteBilateralFilter5(TEXTURE2D_PARAM(src, srcSmp), TEXTURE2D(lDepth), TEXTURE2D(fDepth), float2 texSize, float2 uv, float2 offsets[5], float falloff)
{
    CORE_BILATERAL_UPSAMPLE_LOGIC(SAMPLE_TEXTURE2D_LOD, 5)
}

float4 ExecuteBilateralFilter5XR(TEXTURE2D_X(src), TEXTURE2D_X(lDepth), TEXTURE2D_X(fDepth), float2 texSize, float2 uv, float2 offsets[5], float falloff)
{
    return ExecuteBilateralFilter5XR(
        TEXTURE2D_X_ARGS(src, sampler_LinearClamp),
        lDepth,
        fDepth,
        texSize, uv, offsets, falloff
    );
}

float4 ExecuteBilateralFilter5(TEXTURE2D(src), TEXTURE2D(lDepth), TEXTURE2D(fDepth), float2 texSize, float2 uv, float2 offsets[5], float falloff)
{
    return ExecuteBilateralFilter5(
        TEXTURE2D_ARGS(src, sampler_LinearClamp),
        lDepth,
        fDepth,
        texSize, uv, offsets, falloff
    );
}

float4 UpsampleBilateralCross5XR(TEXTURE2D_X(src), TEXTURE2D_X(lDepth), TEXTURE2D_X(fDepth), float2 texSize, float2 uv, float falloff)
{
    return ExecuteBilateralFilter5XR(src, lDepth, fDepth, texSize, uv, OffsetsCross, falloff);
}

float4 UpsampleBilateralDiagonal5XR(TEXTURE2D_X(src), TEXTURE2D_X(lDepth), TEXTURE2D_X(fDepth), float2 texSize, float2 uv, float falloff)
{
    return ExecuteBilateralFilter5XR(src, lDepth, fDepth, texSize, uv, OffsetsDiagonal, falloff);
}

float4 UpsampleBilateralCross5(TEXTURE2D(src), TEXTURE2D(lDepth), TEXTURE2D(fDepth), float2 texSize, float2 uv, float falloff)
{
    return ExecuteBilateralFilter5(src, lDepth, fDepth, texSize, uv, OffsetsCross, falloff);
}

float4 UpsampleBilateralDiagonal5(TEXTURE2D(src), TEXTURE2D(lDepth), TEXTURE2D(fDepth), float2 texSize, float2 uv, float falloff)
{
    return ExecuteBilateralFilter5(src, lDepth, fDepth, texSize, uv, OffsetsDiagonal, falloff);
}

float4 ExecuteBilateralFilter9XR(TEXTURE2D_X_PARAM(src, srcSmp), TEXTURE2D_X(lDepth), TEXTURE2D_X(fDepth), float2 texSize, float2 uv, float2 offsets[9], float falloff)
{
    CORE_BILATERAL_UPSAMPLE_LOGIC(SAMPLE_TEXTURE2D_X_LOD, 9)
}

float4 ExecuteBilateralFilter9(TEXTURE2D_PARAM(src, srcSmp), TEXTURE2D(lDepth), TEXTURE2D(fDepth), float2 texSize, float2 uv, float2 offsets[9], float falloff)
{
    CORE_BILATERAL_UPSAMPLE_LOGIC(SAMPLE_TEXTURE2D_LOD, 9)
}

float4 ExecuteBilateralFilter9XR(TEXTURE2D_X(src), TEXTURE2D_X(lDepth), TEXTURE2D_X(fDepth), float2 texSize, float2 uv, float2 offsets[9], float falloff)
{
    return ExecuteBilateralFilter9XR(
        TEXTURE2D_X_ARGS(src, sampler_LinearClamp), 
        lDepth, 
        fDepth, 
        texSize, uv, offsets, falloff);
}

float4 ExecuteBilateralFilter9(TEXTURE2D(src), TEXTURE2D(lDepth), TEXTURE2D(fDepth), float2 texSize, float2 uv, float2 offsets[9], float falloff)
{
    return ExecuteBilateralFilter9(
        TEXTURE2D_ARGS(src, sampler_LinearClamp), 
        lDepth, 
        fDepth, 
        texSize, uv, offsets, falloff
    );
}

float4 UpsampleBilateralStar9XR(TEXTURE2D_X(src), TEXTURE2D_X(lDepth), TEXTURE2D_X(fDepth), float2 texSize, float2 uv, float falloff)
{
    return ExecuteBilateralFilter9XR(src, lDepth, fDepth, texSize, uv, OffsetsStar, falloff);
}

float4 UpsampleBilateralStar9(TEXTURE2D(src), TEXTURE2D(lDepth), TEXTURE2D(fDepth), float2 texSize, float2 uv, float falloff)
{
    return ExecuteBilateralFilter9(src, lDepth, fDepth, texSize, uv, OffsetsStar, falloff);
}