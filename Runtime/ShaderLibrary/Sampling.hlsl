#pragma once

#define CORE_SAMPLE_LOGIC(SAMPLER_MACRO, _COUNT, _OFFSETS, tex, smp, uv, texSize, outNeighbors) \
    [unroll] \
    for (int i = 0; i < _COUNT; ++i) { \
        float2 sUV = uv + _OFFSETS[i] * texSize; \
        outNeighbors[i] = SAMPLER_MACRO(tex, smp, sUV, 0); \
    }

#define BOX_OFFSETS_9 \
    static const float2 offsets9[9] = { \
        float2(-1, -1), float2(0, -1), float2(1, -1), \
        float2(-1, 0),  float2(0, 0),  float2(1, 0), \
        float2(-1, 1),  float2(0, 1),  float2(1, 1) \
    };

#define CROSS_OFFSETS_5 \
    static const float2 offsets5[5] = { \
        float2(-1, 0), float2(1, 0), float2(0, 0), float2(0, -1), float2(0, 1) \
    };

void SampleNeighborhoodXR(TEXTURE2D_X_PARAM(tex, smp), float2 uv, float2 texelSize, out float4 neighbors[9])
{
    BOX_OFFSETS_9
    CORE_SAMPLE_LOGIC(SAMPLE_TEXTURE2D_X_LOD, 9, offsets9, tex, smp, uv, texelSize, neighbors)
}

void SampleNeighborhood(TEXTURE2D_PARAM(tex, smp), float2 uv, float2 texelSize, out float4 neighbors[9])
{
    BOX_OFFSETS_9
    CORE_SAMPLE_LOGIC(SAMPLE_TEXTURE2D_LOD, 9, offsets9, tex, smp, uv, texelSize, neighbors)
}

void SampleNeighborhoodXR(TEXTURE2D_X_PARAM(tex, smp), float2 uv, float2 texelSize, out float4 neighbors[5])
{
    CROSS_OFFSETS_5
    CORE_SAMPLE_LOGIC(SAMPLE_TEXTURE2D_X_LOD, 5, offsets5, tex, smp, uv, texelSize, neighbors)
}

void SampleNeighborhood(TEXTURE2D_PARAM(tex, smp), float2 uv, float2 texelSize, out float4 neighbors[5])
{
    CROSS_OFFSETS_5
    CORE_SAMPLE_LOGIC(SAMPLE_TEXTURE2D_LOD, 5, offsets5, tex, smp, uv, texelSize, neighbors)
}

void SampleNeighborhoodXR(TEXTURE2D_X(tex), float2 uv, float2 texelSize, out float4 neighbors[9])
{
    SampleNeighborhoodXR(TEXTURE2D_X_ARGS(tex, sampler_PointClamp), uv, texelSize, neighbors);
}

void SampleNeighborhoodXR(TEXTURE2D_X(tex), float2 uv, float2 texelSize, out float4 neighbors[5])
{
    SampleNeighborhoodXR(TEXTURE2D_X_ARGS(tex, sampler_PointClamp), uv, texelSize, neighbors);
}

void SampleNeighborhood(TEXTURE2D(tex), float2 uv, float2 texelSize, out float4 neighbors[9])
{
    SampleNeighborhood(TEXTURE2D_ARGS(tex, sampler_PointClamp), uv, texelSize, neighbors);
}

void SampleNeighborhood(TEXTURE2D(tex), float2 uv, float2 texelSize, out float4 neighbors[5])
{
    SampleNeighborhood(TEXTURE2D_ARGS(tex, sampler_PointClamp), uv, texelSize, neighbors);
}