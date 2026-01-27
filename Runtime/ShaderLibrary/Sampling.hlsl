#pragma once

#define CORE_SAMPLE_NEIGHBORHOOD_LOGIC(SAMPLER_MACRO, tex, smp, uv, texSize, outNeighbors) \
    static const float2 offsets[9] = { \
        float2(-1, -1), float2(0, -1), float2(1, -1), \
        float2(-1, 0),  float2(0, 0),  float2(1, 0), \
        float2(-1, 1),  float2(0, 1),  float2(1, 1) \
    }; \
    [unroll] \
    for (int i = 0; i < 9; ++i) { \
        float2 sUV = uv + offsets[i] * texSize; \
        sUV = clamp(sUV, 0.0, 1.0); \
        outNeighbors[i] = SAMPLER_MACRO(tex, smp, sUV, 0); \
    }

void SampleNeighborhoodXR(TEXTURE2D_X_PARAM(tex, smp), float2 uv, float2 texelSize, out float4 neighbors[9])
{
    CORE_SAMPLE_NEIGHBORHOOD_LOGIC(SAMPLE_TEXTURE2D_X_LOD, tex, smp, uv, texelSize, neighbors)
}

void SampleNeighborhood(TEXTURE2D_PARAM(tex, smp), float2 uv, float2 texelSize, out float4 neighbors[9])
{
    CORE_SAMPLE_NEIGHBORHOOD_LOGIC(SAMPLE_TEXTURE2D_LOD, tex, smp, uv, texelSize, neighbors)
}

void SampleNeighborhoodXR(TEXTURE2D_X(tex), float2 uv, float2 texelSize, out float4 neighbors[9])
{
    SampleNeighborhoodXR(TEXTURE2D_X_ARGS(tex, sampler_LinearClamp), uv, texelSize, neighbors);
}

void SampleNeighborhood(TEXTURE2D(tex), float2 uv, float2 texelSize, out float4 neighbors[9])
{
    SampleNeighborhood(TEXTURE2D_ARGS(tex, sampler_LinearClamp), uv, texelSize, neighbors);
}