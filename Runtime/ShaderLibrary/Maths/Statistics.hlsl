#pragma once

#define IMPLEMENT_STATS_LOGIC(_SAMPLES) \
    mean = 0; \
    [unroll] for (int i = 0; i < _SAMPLES; ++i) mean += neighborhood[i].rgb; \
    mean /= (float)_SAMPLES; \
    float3 var = 0; \
    [unroll] for (int j = 0; j < _SAMPLES; ++j) { \
        float3 d = neighborhood[j].rgb - mean; \
        var += d * d; \
    } \
    stdDev = sqrt(max(0, var / (float)_SAMPLES));

#define IMPLEMENT_STATS_EXCL_LOGIC(_SAMPLES, _CENTER_IDX) \
    mean = 0; \
    [unroll] for (int i = 0; i < _SAMPLES; ++i) { \
        if (i == _CENTER_IDX) continue; \
        mean += neighborhood[i].rgb; \
    } \
    mean /= (float)(_SAMPLES - 1); \
    float3 var = 0; \
    [unroll] for (int j = 0; j < _SAMPLES; ++j) { \
        if (j == _CENTER_IDX) continue; \
        float3 d = neighborhood[j].rgb - mean; \
        var += d * d; \
    } \
    stdDev = sqrt(max(0, var / (float)(_SAMPLES - 1)));

// --- 9-Tap / 8-Tap (3x3 Box) ---

/// @brief Computes mean and stdDev from a 3x3 neighborhood (9 samples).
void ComputeMeanAndStdDev(in float4 neighborhood[9], out float3 mean, out float3 stdDev)
{
    IMPLEMENT_STATS_LOGIC(9)
}

void ComputeMeanAndStdDev(in float3 neighborhood[9], out float3 mean, out float3 stdDev)
{
    IMPLEMENT_STATS_LOGIC(9)
}

/// @brief Computes mean and stdDev from a 3x3 neighborhood, excluding the center (8 samples).
void ComputeMeanAndStdDevExcl(in float4 neighborhood[9], out float3 mean, out float3 stdDev)
{
    IMPLEMENT_STATS_EXCL_LOGIC(9, 4)
}

void ComputeMeanAndStdDevExcl(in float3 neighborhood[9], out float3 mean, out float3 stdDev)
{
    IMPLEMENT_STATS_EXCL_LOGIC(9, 4)
}

// --- 5-Tap / 4-Tap (Cross) ---

/// @brief Computes mean and stdDev from a 5-tap cross neighborhood.
void ComputeMeanAndStdDev(in float4 neighborhood[5], out float3 mean, out float3 stdDev)
{
    IMPLEMENT_STATS_LOGIC(5)
}

void ComputeMeanAndStdDev(in float3 neighborhood[5], out float3 mean, out float3 stdDev)
{
    IMPLEMENT_STATS_LOGIC(5)
}

/// @brief Computes mean and stdDev from a 5-tap cross, excluding the center (4 samples).
void ComputeMeanAndStdDevExcl(in float4 neighborhood[5], out float3 mean, out float3 stdDev)
{
    IMPLEMENT_STATS_EXCL_LOGIC(5, 2)
}

void ComputeMeanAndStdDevExcl(in float3 neighborhood[5], out float3 mean, out float3 stdDev)
{
    IMPLEMENT_STATS_EXCL_LOGIC(5, 2)
}