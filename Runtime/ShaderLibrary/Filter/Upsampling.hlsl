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
#define CORE_BILATERAL_UPSAMPLE_LOGIC(SAMPLE_MACRO, COUNT) \
    float referenceDepth = SAMPLE_MACRO(fDepth, fDepthSmp, uv, 0).r; \
    float4 combinedColor = 0; \
    float combinedWeight = 0; \
    \
    [unroll] \
    for(int i = 0; i < COUNT; i++) { \
        float2 sampleUV = uv + offsets[i] * texSize.xy; \
        float sampleDepth = SAMPLE_MACRO(lDepth, lDepthSmp, sampleUV, 0).r; \
        float4 sampleColor = SAMPLE_MACRO(src, srcSmp, sampleUV, 0); \
        \
        float w = 1.0 / (abs(referenceDepth - sampleDepth) * falloff + 0.001); \
        \
        float spatial = (i == 0) ? 2.0 : 1.0; \
        \
        combinedColor += sampleColor * (w * spatial); \
        combinedWeight += (w * spatial); \
    } \
    return combinedColor / (combinedWeight + 0.00001);

float4 ExecuteBilateralFilter5XR(TEXTURE2D_X_PARAM(src, srcSmp), TEXTURE2D_X_PARAM(lDepth, lDepthSmp), TEXTURE2D_X_PARAM(fDepth, fDepthSmp), float4 texSize, float2 uv, float2 offsets[5], float falloff)
{
    CORE_BILATERAL_LOGIC(SAMPLE_TEXTURE2D_X_LOD, 5)
}

float4 ExecuteBilateralFilter5(TEXTURE2D_PARAM(src, srcSmp), TEXTURE2D_PARAM(lDepth, lDepthSmp), TEXTURE2D_PARAM(fDepth, fDepthSmp), float4 texSize, float2 uv, float2 offsets[5], float falloff)
{
    CORE_BILATERAL_LOGIC(SAMPLE_TEXTURE2D_LOD, 5)
}

float4 ExecuteBilateralFilter5XR(TEXTURE2D_X(src), TEXTURE2D_X(lDepth), TEXTURE2D_X(fDepth), float4 texSize, float2 uv, float2 offsets[5], float falloff)
{
    return ExecuteBilateralFilter5XR(
        TEXTURE2D_X_ARGS(src, sampler_LinearClamp),
        TEXTURE2D_X_ARGS(lDepth, sampler_PointClamp),
        TEXTURE2D_X_ARGS(fDepth, sampler_PointClamp),
        texSize, uv, offsets, falloff
    );
}

float4 ExecuteBilateralFilter5(TEXTURE2D( src), TEXTURE2D(lDepth), TEXTURE2D(fDepth), float4 texSize, float2 uv, float2 offsets[5], float falloff)
{
    return ExecuteBilateralFilter5(
        TEXTURE2D_ARGS(src, sampler_LinearClamp),
        TEXTURE2D_ARGS(lDepth, sampler_PointClamp),
        TEXTURE2D_ARGS(fDepth, sampler_PointClamp),
        texSize, uv, offsets, falloff
    );
}

float4 ExecuteBilateralFilter9XR(TEXTURE2D_X_PARAM(src, srcSmp), TEXTURE2D_X_PARAM(lDepth, lDepthSmp), TEXTURE2D_X_PARAM(fDepth, fDepthSmp), float4 texSize, float2 uv, float2 offsets[9], float falloff)
{
    CORE_BILATERAL_LOGIC(SAMPLE_TEXTURE2D_X_LOD, 9)
}

float4 ExecuteBilateralFilter9(TEXTURE2D_PARAM(src, srcSmp), TEXTURE2D_PARAM(lDepth, lDepthSmp), TEXTURE2D_PARAM(fDepth, fDepthSmp), float4 texSize, float2 uv, float2 offsets[9], float falloff)
{
    CORE_BILATERAL_LOGIC(SAMPLE_TEXTURE2D_LOD, 9)
}

float4 ExecuteBilateralFilter9XR(TEXTURE2D_X(src), TEXTURE2D_X(lDepth), TEXTURE2D_X(fDepth), float4 texSize, float2 uv, float2 offsets[9], float falloff)
{
    return ExecuteBilateralFilter9XR(
        TEXTURE2D_X_ARGS(src, sampler_LinearClamp), 
        TEXTURE2D_X_ARGS(lDepth, sampler_PointClamp), 
        TEXTURE2D_X_ARGS(fDepth, sampler_PointClamp), 
        texSize, uv, offsets, falloff);
}

float4 ExecuteBilateralFilter9(TEXTURE2D( src), TEXTURE2D(lDepth), TEXTURE2D(fDepth), float4 texSize, float2 uv, float2 offsets[9], float falloff)
{
    return ExecuteBilateralFilter9(
        TEXTURE2D_ARGS(src, sampler_LinearClamp), 
        TEXTURE2D_ARGS(lDepth, sampler_PointClamp), 
        TEXTURE2D_ARGS(fDepth, sampler_PointClamp), 
        texSize, uv, offsets, falloff
    );
}
