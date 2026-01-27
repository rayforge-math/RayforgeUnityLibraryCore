#pragma once

// ============================================================================
// 1. Includes
// ============================================================================

#include "../Maths/Statistics.hlsl"
#include "../Filter/DeviationFilter.hlsl"

// ============================================================================
// 2. Utility Functions
// ============================================================================

/// @brief Samples a history texture at the given UV coordinates.
/// Returns (0,0,0,0) if the UVs are outside [0,1].
/// @param historyTexture The history texture to sample.
/// @param historySampler Sampler state for the texture.
/// @param uv UV coordinates in [0,1].
/// @return The sampled color from the history texture, or zero if UV is invalid.
float4 SampleHistoryXR(TEXTURE2D_X_PARAM(historyTexture, historySampler), float2 uv)
{
    if (any(uv < 0.0) || any(uv > 1.0))
        return float4(0, 0, 0, 0);
    
    return SAMPLE_TEXTURE2D_X_LOD(historyTexture, historySampler, uv, 0);
}

/// @brief Samples a history texture using motion vectors to offset the current UV coordinates.
/// This is useful for temporal reprojection of screen-space effects.
/// @param historyTexture The history texture to sample.
/// @param historySampler Sampler state for the texture.
/// @param currentUV Current frame UV coordinates.
/// @param motionVector Motion vector to reproject the UV into the previous frame. Usually retrieved from the combined camera + object motion.
/// @return The sampled color from the history texture at the reprojected position.
float4 SampleHistoryMotionVectorsXR(TEXTURE2D_X_PARAM(historyTexture, historySampler), float2 currentUV, float2 motionVector)
{
    float2 uv = currentUV - DecodeMotionVector(motionVector) - (_TAA_Jitter - _TAA_JitterPrev);
    return SampleHistoryXR(TEXTURE2D_X_ARGS(historyTexture, historySampler), uv);
}

/// @brief Samples a history texture by projecting a world-space position into the previous frame.
/// Converts the world position to clip space using the previous view-projection matrix,
/// performs perspective division, and converts to UV coordinates for sampling.
/// @param historyTexture The history texture to sample.
/// @param historySampler Sampler state for the texture.
/// @param worldPos World-space position to reproject into the previous frame. Usually reconstructed from depth buffer.
/// @return The sampled color from the history texture, or zero if the projected pixel is invalid.
float4 SampleHistoryWorldPosXR(TEXTURE2D_X_PARAM(historyTexture, historySampler), float3 worldPos)
{
    float4 clipPrev = mul(_Rayforge_Matrix_Prev_VP, float4(worldPos, 1.0));

    if (clipPrev.w <= 0.001f)
        return float4(0, 0, 0, 0);

    float2 ndcPrev = clipPrev.xy / clipPrev.w;
    float2 uv = ndcPrev * 0.5 + 0.5;

#if UNITY_UV_STARTS_AT_TOP
    if (_ProjectionParams.x < 0)
    {
        uv.y = 1.0 - uv.y;
    }
#endif

    return SampleHistoryXR(TEXTURE2D_X_ARGS(historyTexture, historySampler), uv);
}

/// @brief Determines whether a previous frame sample should be rejected based on depth difference. Useful to avoid blending history across surfaces at different depths, reducing ghosting.
/// @param currentDepth Depth of the current pixel in view or linear depth space.
/// @param previousDepth Depth of the corresponding pixel in the history buffer.
/// @param threshold Maximum allowed depth difference before rejecting the history sample.
/// @return true if the sample should be rejected; false if the depth difference is within the threshold (sample is valid).
bool DepthReject(float currentDepth, float previousDepth, float threshold)
{
    return abs(currentDepth - previousDepth) > threshold;
}

/// @brief Computes a disocclusion factor based solely on the motion magnitude of the current frame. High-motion pixels are treated as increasingly invalid to reduce ghosting, without needing previous frame motion.
/// @param velocityUV Motion vector of the current pixel in UV space (current frame).
/// @param threshold Velocity magnitude above which history starts to be ignored.
/// @param scale Scaling factor controlling how quickly the disocclusion ramps from 0 to 1.
/// @return A value in [0,1] representing the disocclusion factor: 0 = history fully valid, 1 = history fully ignored.
float VelocityMagnitudeDisocclusion(float2 velocityUV, float threshold, float scale)
{
    float speed = length(velocityUV);
    return saturate((speed - threshold) * scale);
}

/// @brief Blends the current frame color with a previous frame sample using a specified history weight.
/// @param current Color of the current frame.
/// @param previous Color sampled from the previous frame (possibly clamped or depth-rejected).
/// @param historyWeight Weight of the history sample in the final blend. Range [0,1].
/// @return The blended color.
float4 Blend(float4 current, float4 previous, float historyWeight)
{
    return lerp(current, previous, historyWeight);
}

/// @brief Samples the motion vector at the current UV and fetches the reprojected
/// history color from the previous frame, correcting for TAA jitter.
/// @param historyTexture The history color texture from the previous frame.
/// @param historySampler The sampler used to sample the history texture.
/// @param currentUV The UV coordinate of the current pixel in screen space [0..1].
/// @param motionVector Output: The motion vector retrieved from the motion vector buffer.
/// Unity motion vectors are based on unjittered projection matrices.
/// @param history Output: The reprojected history color from the previous frame, including
/// depth information typically stored in the alpha channel.
void SetupMotionVectorPipelineXR(TEXTURE2D_X_PARAM(historyTexture, historySampler), float2 currentUV, out float2 motionVector, out float4 history)
{
    motionVector = SAMPLE_TEXTURE2D_X_LOD(_TAA_MotionVectorTexture, sampler_TAA_MotionVectorTexture, currentUV, 0).rg;
    history = SampleHistoryMotionVectorsXR(TEXTURE2D_X_ARGS(historyTexture, historySampler), currentUV, motionVector);
}

/// @brief Reconstructs the world-space position of the current pixel using its UV
/// coordinate and depth value from the depth buffer.
/// @param uv The UV coordinate of the current pixel in normalized screen space [0..1].
/// @param depth The non-linear clip-space depth value sampled from the depth buffer.
/// @return The reconstructed world-space position of the pixel.
float3 ReconstructWorldPos(float2 uv, float depth)
{
    float2 ndc = uv * 2.0 - 1.0;
    float4 posCS = float4(ndc, depth, 1.0);

    float4 posWS = mul(_Rayforge_Matrix_Inv_VP, posCS);
    posWS /= posWS.w;

    return posWS.xyz;
}

/// @brief Reconstructs the world-space position of the current pixel and samples the
/// history buffer using that world position, enabling world-space based reprojection.
/// @param historyTexture The history buffer from the previous frame, storing color and depth (depth typically in the alpha channel).
/// @param historySampler The sampler used to sample the history texture.
/// @param currentUV The UV coordinate of the current pixel in the current frame.
/// @param depth The non-linear depth value for the current pixel, sampled from the current depth buffer.
/// @param history Output: The history sample reprojected using world-space position lookup, including stored depth in the alpha channel.
void SetupWorldPosPipelineXR(TEXTURE2D_X_PARAM(historyTexture, historySampler), float2 currentUV, float depth, out float4 history)
{
    float3 worldPos = ReconstructWorldPos(currentUV, depth);
    history = SampleHistoryWorldPosXR(TEXTURE2D_X_ARGS(historyTexture, historySampler), worldPos);
}

/// @brief Determines whether the given motion vector indicates noticeable motion.
/// @param motionVector The motion vector to test.
/// @return True if the magnitude of the motion vector exceeds a small threshold; otherwise false.
bool HasMotion(float2 motionVector)
{
    return dot(motionVector, motionVector) > 1e-6;
}

/// @brief Samples the TAA depth texture at the given UV coordinates and converts it
/// to linear 0–1 depth using the global z-buffer parameters.
/// @param uv UV coordinates to sample at.
/// @return Linear depth in the range [0,1].
float SampleLinear01DepthXR(float2 uv)
{
    float rawDepth = SAMPLE_TEXTURE2D_X_LOD(_TAA_DepthTexture, sampler_TAA_DepthTexture, uv, 0).r;
    return Linear01Depth(rawDepth, _ZBufferParams);
}

/// @brief Checks whether depth rejection should occur and updates the result accordingly.
/// If a depth mismatch is detected, the history is discarded and the current color is used.
/// @param currentColor The current frame's color at this pixel.
/// @param currentDepth The current frame's linear 0..1 depth value.
/// @param history The history color, where the alpha channel contains previous-frame depth.
/// @param threshold Depth threshold used to determine whether the history is valid.
/// @param result In/out: On rejection, this is filled with the current color and updated depth.
/// @return True if depth rejection occurred; otherwise false.
bool CheckAndSetupDepthRejection(float3 currentColor, float currentDepth, float4 history, float threshold, inout float4 result)
{
    float prevDepth = history.a;
    result.a = currentDepth;

    if (DepthReject(currentDepth, prevDepth, threshold))
    {
        result.rgb = currentColor;
        return true;
    }
    return false;
}

/// @brief Reduces the history weight if the pixel is likely disoccluded, based on motion vector magnitude.
/// @param motionVector The pixel's motion vector.
/// @param threshold Motion magnitude threshold for detecting disocclusion.
/// @param scale Additional scale factor to amplify or diminish disocclusion sensitivity.
/// @param historyWeight In/out: The blending weight applied to the history color. Reduced when disocclusion is detected.
void SetupVelocityDisocclusion(float2 motionVector, float threshold, float scale, inout float historyWeight)
{
    float disocclusion = VelocityMagnitudeDisocclusion(motionVector, threshold, scale);
    historyWeight *= (1.0 - disocclusion);
}

// ============================================================================
// Utility building blocks
// ============================================================================

inline bool ApplyDepthRejection(float4 currentColor, float currentDepth, float4 history, ReprojectionParams params, inout float4 result)
{
#if !defined(TAA_ALLOW_FULL_RGBA)
    if (params.depthRejection &&
        CheckAndSetupDepthRejection(currentColor.rgb, currentDepth, history, params.depthThreshold, result))
    {
        return true;
    }
#endif
    return false;
}

inline void ApplyVelocityDisocclusion(float2 motionVector, inout ReprojectionParams params)
{
    if (params.velocityDisocclusion && HasMotion(motionVector))
    {
        SetupVelocityDisocclusion(motionVector, params.velocityThreshold, params.velocityScale, params.historyWeight);
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
#if defined(TAA_ALLOW_FULL_RGBA)
    result = Blend(currentColor, history, params.historyWeight);
#else
    float depth = result.a;
    result = float4(Blend(currentColor, history, params.historyWeight).rgb, depth);
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
float4 BlendHistoryMotionVectorsXR(TEXTURE2D_X_PARAM(historyTexture, historySampler), float2 currentUV, float4 currentColor, ReprojectionParams params)
{
    float4 result = float4(0, 0, 0, 0);

    float2 motionVector;
    float4 history;

    SetupMotionVectorPipelineXR(TEXTURE2D_X_ARGS(historyTexture, historySampler), currentUV, motionVector, history);

    float currentDepth = SampleLinear01DepthXR(currentUV);

    if (ApplyDepthRejection(currentColor, currentDepth, history, params, result))
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
float4 BlendHistoryMotionVectorsXR(TEXTURE2D_X_PARAM(historyTexture, historySampler), float2 currentUV, float4 currentNeighborhood[9], ReprojectionParams params)
{
    float4 result = (float4) 0;

    float2 motionVector;
    float4 history;

    SetupMotionVectorPipelineXR(TEXTURE2D_X_ARGS(historyTexture, historySampler), currentUV, motionVector, history);

    float currentDepth = SampleLinear01DepthXR(currentUV);
    float4 currentColor = currentNeighborhood[4];

    if (ApplyDepthRejection(currentColor, currentDepth, history, params, result))
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
float4 BlendHistoryWorldPosXR(TEXTURE2D_X_PARAM(historyTexture, historySampler), float2 currentUV, float4 currentColor, ReprojectionParams params)
{
    float4 result = (float4) 0;

    float currentDepth = SampleLinear01DepthXR(currentUV);
    
    float4 history;
    SetupWorldPosPipelineXR(TEXTURE2D_X_ARGS(historyTexture, historySampler), currentUV, currentDepth, history);

    if (ApplyDepthRejection(currentColor, currentDepth, history, params, result))
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
float4 BlendHistoryWorldPosXR(TEXTURE2D_X_PARAM(historyTexture, historySampler), float2 currentUV, float4 currentNeighborhood[9], ReprojectionParams params)
{
    float4 result = (float4) 0;
    
    float4 currentColor = currentNeighborhood[4];
    float currentDepth = SampleLinear01DepthXR(currentUV);
    
    float4 history;
    SetupWorldPosPipelineXR(TEXTURE2D_X_ARGS(historyTexture, historySampler), currentUV, currentDepth, history);

    if (ApplyDepthRejection(currentColor, currentDepth, history, params, result))
    {
        return result;
    }

    ApplyColorClamping(history.rgb, currentNeighborhood, params);
    ApplyFinalBlend(currentColor, history, params, result);

    return result;
}
