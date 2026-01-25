#pragma once

// ============================================================================
// CustomUnityLibrary - Common Shader Include
// Author: Matthew
// Description: blue noise functionality
// ============================================================================

// ============================================================================
// 1. Includes
// ============================================================================

#include "../Utils/Hashes.hlsl"

#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"

// ============================================================================
// 1. Utility Functions
// ============================================================================

/// @brief Samples a blue-noise texture in screen space.
/// @param screenUV UV coordinate in screen space (0�1)
/// @param screenSize Screen resolution in pixels
/// @return The red channel value of the sampled blue-noise texture
float SampleBlueNoise(float2 screenUV, float2 screenSize)
{
    screenUV.x *= screenSize.x / screenSize.y;
    return SAMPLE_TEXTURE2D(_Rayforge_BlueNoise, sampler_Rayforge_BlueNoise, screenUV).r * 2 - 1;
}

/// @brief Samples a blue-noise texture in screen space and offsets it over time
/// to produce continuous temporal variation (e.g., for dithering or noise animation).
/// @param screenUV UV coordinate in screen space (0..1)
/// @param screenSize Screen resolution in pixels
/// @return The red channel value of the sampled blue-noise texture
float SampleBlueNoiseTimeOffset(float2 screenUV, float2 screenSize)
{
    screenUV.x *= screenSize.x / screenSize.y;

    float offset = Hash01(_Time.x);
    screenUV += offset;
    screenUV = frac(screenUV);

    return SAMPLE_TEXTURE2D(_Rayforge_BlueNoise, sampler_Rayforge_BlueNoise, screenUV).r * 2 - 1;
}