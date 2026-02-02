#pragma once

#include "../Common.hlsl"

CBUFFER_START(_Rayforge_TaaCamera)
float4x4 _Rayforge_Matrix_Prev_VP;
float4x4 _Rayforge_Matrix_Inv_VP;
CBUFFER_END

/// @brief Parameter block controlling temporal reprojection behavior, including
/// depth rejection, motion-vector-based disocclusion, history weighting,
/// and optional neighborhood-based color clamping.
/// @note Intentionally made to be 16 byte aligned, fitting within 2 4-component 32 bit vector registers.
struct ReprojectionParams
{
    bool depthRejection;
    float depthThreshold;
    bool velocityDisocclusion;
    float velocityThreshold;
    float velocityScale;
    float historyWeight;
    int colorClampingMode;
    float clipBoxScale;
};