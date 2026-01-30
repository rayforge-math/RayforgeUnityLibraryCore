#pragma once

// ============================================================================
// 1. Includes
// ============================================================================

#include "../Maths/Statistics.hlsl"
#include "../Filter/DeviationFilter.hlsl"

// ============================================================================
// 2. Defines
// ============================================================================

#if defined(TAA_USE_DEPTH)
    #define DEPTH_INPUT_DECL , float curDepth 
    #define DEPTH_INPUT_PASS , curDepth 

    #if defined(TAA_ALLOW_FULL_RGBA) 
        // MRT Case - Add the history depth texture to the arguments.
        #define DEPTH_ARGS_X_DECL , TEXTURE2D_X_PARAM(histDepthTex, histDepthSmp)
        #define DEPTH_ARGS_X_PASS , TEXTURE2D_X_ARGS(histDepthTex, histDepthSmp)
        #define FETCH_HIST_DEPTH(col, uv) SAMPLE_TEXTURE2D_X_LOD(histDepthTex, histDepthSmp, uv, 0).r
    #else
        // Alpha Case - No extra textures needed.
        #define DEPTH_ARGS_DECL 
        #define DEPTH_ARGS_PASS 
        #define FETCH_HIST_DEPTH(col, uv) col.a
    #endif
#else
    #define DEPTH_INPUT_DECL
    #define DEPTH_INPUT_PASS
    #define DEPTH_ARGS_X_DECL 
    #define DEPTH_ARGS_X_PASS 
    #define FETCH_HIST_DEPTH(col, uv) 0.0
#endif

#define TAA_MV_ARGS_X_DECL   , TEXTURE2D_X_PARAM(mvTex, mvSmp) DEPTH_ARGS_X_DECL
#define TAA_MV_ARGS_X_PASS   , TEXTURE2D_X_ARGS(mvTex, mvSmp) DEPTH_ARGS_X_PASS

#define TAA_WP_ARGS_X_DECL   DEPTH_ARGS_X_DECL
#define TAA_WP_ARGS_X_PASS   DEPTH_ARGS_X_PASS

#define SAMPLE_MV(uv) SAMPLE_TEXTURE2D_X_LOD(mvTex, mvSmp, uv, 0).rg

#if !defined(DECODE_MOTION_VECTOR)
    #define DECODE_MOTION_VECTOR(mv) (mv * 0.5)
#endif

// ============================================================================
// 3. Utility Functions
// ============================================================================

/// @brief Samples history at UV; returns fallback if UV is out of bounds [0,1].
float4 SampleHistoryXR(TEXTURE2D_X_PARAM(historyTexture, historySampler), float2 uv, float4 fallback)
{
    if (any(uv < 0.0) || any(uv > 1.0)) 
        return fallback;

    return SAMPLE_TEXTURE2D_X_LOD(historyTexture, historySampler, uv, 0);
}

// --- Pipeline Setups ---

/// @brief Fetches motion vectors and samples history for the motion-based pipeline.
void SetupMotionVectorPipelineXR(TEXTURE2D_X_PARAM(historyTexture, historySampler), float2 currentUV, float4 fallback, out float2 motionVector, out float4 history, out float historyDepth TAA_MV_ARGS_X_DECL)
{
    motionVector = SAMPLE_MV(currentUV);
    
    float2 prevUV = currentUV - DECODE_MOTION_VECTOR(motionVector) - (_TAA_Jitter - _TAA_JitterPrev);
    history = SampleHistoryXR(TEXTURE2D_X_ARGS( historyTexture, historySampler), prevUV, fallback);
    historyDepth = FETCH_HIST_DEPTH(history, prevUV);
}

/// @brief Reconstructs WS position from UV and depth.
float3 ReconstructWorldPos(float2 uv, float currentDepth)
{
    float rawDepth = (1.0 / (currentDepth * _ZBufferParams.z)) - (_ZBufferParams.w / _ZBufferParams.z);
    float2 ndc = uv * 2.0 - 1.0;
    float4 posCS = float4(ndc, rawDepth, 1.0);
    float4 posWS = mul(_Rayforge_Matrix_Inv_VP, posCS);
    return posWS.xyz / posWS.w;
}

/// @brief Reprojects history using world-space position.
void SetupWorldPosPipelineXR(TEXTURE2D_X_PARAM(historyTexture, historySampler), float2 currentUV, float currentDepth, float4 fallback, out float4 history TAA_WP_ARGS_X_DECL)
{
    float3 worldPos = ReconstructWorldPos(currentUV, currentDepth);

    float4 clipPrev = mul(_Rayforge_Matrix_Prev_VP, float4(worldPos, 1.0));
    if (clipPrev.w > 0.001f)
    {
        float2 ndcPrev = clipPrev.xy / clipPrev.w;
        float2 uv = ndcPrev * 0.5 + 0.5;

#if UNITY_UV_STARTS_AT_TOP
        if (_ProjectionParams.x < 0) uv.y = 1.0 - uv.y;
#endif

        history = SampleHistoryXR(TEXTURE2D_X_ARGS(historyTexture, historySampler), uv, fallback);
    }
    else
    {
        history = float4(0, 0, 0, 0);
    }
}

// ============================================================================
// Utility building blocks
// ============================================================================

inline bool ApplyDepthRejection(float4 currentColor, float4 history, float historyDepth, ReprojectionParams params, inout float4 result DEPTH_INPUT_DECL)
{
#if defined(TAA_USE_DEPTH)
    
#if !defined(TAA_ALLOW_FULL_RGBA)
    result.a = curDepth;
#endif
    
    bool depthReject = abs(curDepth - historyDepth) > params.depthThreshold;
    
    if (depthReject)
    {
#if defined(TAA_ALLOW_FULL_RGBA)
        result = currentColor;
#else
        result.rgb = currentColor.rgb;
#endif
        
        return true;
    }
#endif

    return false;
}

inline void ApplyVelocityDisocclusion(float2 motionVector, inout ReprojectionParams params)
{
    bool hasMotion = dot(motionVector, motionVector) > 1e-6;
    
    if (params.velocityDisocclusion && hasMotion)
    {
        float speed = length(motionVector);
        float disocclusion = saturate((speed - params.velocityThreshold) * params.velocityScale);
        params.historyWeight *= (1.0 - disocclusion);
    }
}

inline void ApplyColorClamping(inout float3 historyRGB, float4 neighborhood[9], ReprojectionParams params)
{
    if (params.colorClampingMode != CLAMP_NONE)
    {
        historyRGB = ApplyColorClamping(historyRGB, neighborhood, params.colorClampingMode, params.clipBoxScale);
    }
}

inline void ApplyFinalBlend(float4 currentColor, float4 history, ReprojectionParams params, inout float4 result)
{
    float4 blend = lerp(currentColor, history, params.historyWeight);
    
#if defined(TAA_ALLOW_FULL_RGBA)
    result = blend;
#else
    float depth = result.a;
    result = float4(blend.rgb, depth);
#endif
}

// ============================================================================
// Prebuilt API
// ============================================================================

/// @brief Reprojects and blends the history color for the current pixel using motion vectors
/// and optional rejection heuristics. This variant uses only the current pixel color,
/// without neighborhood-based color clamping.
/// @param historyTexture The history color texture from the previous frame.
/// @param historySampler The sampler used to sample the history texture.
/// @param currentUV The UV coordinate of the current pixel.
/// @param currentColor The current-frame color at this pixel.
/// @param params Reprojection settings controlling depth rejection, velocity disocclusion,
/// history weighting, and optional color clamping mode.
/// @return The blended color result, combining the current color and reprojected history.
float4 BlendHistoryMotionVectorsXR(TEXTURE2D_X_PARAM(historyTexture, historySampler), float2 currentUV, float4 currentColor, ReprojectionParams params DEPTH_INPUT_DECL TAA_MV_ARGS_X_DECL)
{
    float4 result = float4(0, 0, 0, 0);

    float2 motionVector;
    float4 history;
    float historyDepth;
    SetupMotionVectorPipelineXR(TEXTURE2D_X_ARGS(historyTexture, historySampler), currentUV, currentColor, motionVector, history, historyDepth TAA_MV_ARGS_X_PASS);

    if (ApplyDepthRejection(currentColor, history, historyDepth, params, result DEPTH_INPUT_PASS))
    {
        return result;
    }

    ApplyVelocityDisocclusion(motionVector, params);
    ApplyFinalBlend(currentColor, history, params, result);

    return result;
}

/// @brief Reprojects and blends the history color using motion vectors, with optional
/// depth rejection, velocity disocclusion, and neighborhood-based color clamping.
/// This variant requires a full 3x3 neighborhood of current-frame colors.
/// @param historyTexture The history color texture from the previous frame.
/// @param historySampler The sampler used to sample the history texture.
/// @param currentUV The UV coordinate of the current pixel.
/// @param currentNeighborhood A 3x3 neighborhood of current-frame colors, indexed row-major.
/// Element [4] must contain the current pixel's color.
/// @param params Reprojection settings controlling depth rejection, velocity disocclusion,
/// history weighting, and the color clamping mode (None, MinMax, ClipBox).
/// @return The blended color result after reprojection, clamping, and temporal filtering.
float4 BlendHistoryMotionVectorsXR(TEXTURE2D_X_PARAM(historyTexture, historySampler), float2 currentUV, float4 currentNeighborhood[9], ReprojectionParams params DEPTH_INPUT_DECL TAA_MV_ARGS_X_DECL)
{
    float4 result = (float4) 0;

    float2 motionVector;
    float4 history;
    float historyDepth;
    float4 currentColor = currentNeighborhood[4];
    SetupMotionVectorPipelineXR(TEXTURE2D_X_ARGS(historyTexture, historySampler), currentUV, currentColor, motionVector, history, historyDepth TAA_MV_ARGS_X_PASS);

    if (ApplyDepthRejection(currentColor, history, historyDepth, params, result DEPTH_INPUT_PASS))
    {
        return result;
    }

    ApplyVelocityDisocclusion(motionVector, params);
    ApplyColorClamping(history.rgb, currentNeighborhood, params);
    ApplyFinalBlend(currentColor, history, params, result);

    return result;
}

/// @brief Reprojects history using world-space reconstruction instead of motion vectors,
/// then blends the reprojected history color with the current pixel color.
/// This variant assumes a mostly static world (no velocity-based disocclusion)
/// and relies on depth-based rejection to avoid ghosting when geometry changes,
/// moves across edges, or becomes newly visible.
/// Per-object motion is not taken into account.
/// @param historyTexture The history color texture from the previous frame.
/// Expected to store previous-frame depth in the alpha channel.
/// @param historySampler Sampler state used for sampling the history texture.
/// @param currentUV UV coordinate of the current pixel in normalized screen space [0..1].
/// @param currentColor The color computed for the current frame at this pixel (pre-TAA).
/// @param params Reprojection parameters controlling history weighting and depth rejection behavior.
/// @return The blended final color, combining current-frame color with
/// world-reprojected history, or the current color alone if history is rejected.
float4 BlendHistoryWorldPosXR(TEXTURE2D_X_PARAM(historyTexture, historySampler), float2 currentUV, float currentDepth, float4 currentColor, ReprojectionParams params TAA_WP_ARGS_X_DECL)
{
    float4 result = (float4) 0;
    
    float4 history;
    float historyDepth;
    SetupWorldPosPipelineXR(TEXTURE2D_X_ARGS(historyTexture, historySampler), currentUV, currentDepth, currentColor, history TAA_WP_ARGS_X_PASS);

    float curDepth = currentDepth;
    if (ApplyDepthRejection(currentColor, history, historyDepth, params, result DEPTH_INPUT_PASS))
    {
        return result;
    }

    ApplyFinalBlend(currentColor, history, params, result);

    return result;
}

/// @brief Reprojects history using world-space reconstruction and applies optional
/// color-clamping using a 3x3 neighborhood from the current frame.
/// This variant is similar to the motion-vector version of history blending,
/// but uses world-space reprojection instead of stored motion vectors,
/// and therefore does not support velocity-based disocclusion.
/// Per-object motion is not taken into account.
/// @param historyTexture The previous frame's history color texture.  
/// The alpha channel is expected to contain previous-frame depth.
/// @param historySampler Sampler state used when sampling the history texture.
/// @param currentUV UV coordinate of the current pixel in normalized screen space [0..1].
/// @param currentNeighborhood A 3x3 array of current-frame color samples centered at the current pixel.
/// Used for statistical color clamping (mean, variance, clip box, etc.).
/// @param params Reprojection settings controlling depth rejection, history weighting,
/// color-clamping mode, and clip-box parameters.
/// @return The final TAA-filtered color for the pixel, combining the reprojected
/// history with the current frame's color after optional clamping.  
/// If history is rejected, returns the current color.
float4 BlendHistoryWorldPosXR(TEXTURE2D_X_PARAM(historyTexture, historySampler), float2 currentUV, float currentDepth, float4 currentNeighborhood[9], ReprojectionParams params TAA_WP_ARGS_X_DECL)
{
    float4 result = (float4)0;

    float4 currentColor = currentNeighborhood[4];
    float4 history;
    float historyDepth;
    SetupWorldPosPipelineXR(TEXTURE2D_X_ARGS(historyTexture, historySampler), currentUV, currentDepth, currentColor, history TAA_WP_ARGS_X_PASS);
    
    float curDepth = currentDepth;
    if (ApplyDepthRejection(currentColor, history, historyDepth, params, result DEPTH_INPUT_PASS))
    {
        return result;
    }

    ApplyColorClamping(history.rgb, currentNeighborhood, params);
    ApplyFinalBlend(currentColor, history, params, result);

    return result;
}