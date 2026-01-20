#pragma once

// ============================================================================
// Rayforge Unity Library Core - Blur Shader Include
// Author: Matthew
// Description: pipeline independant HLSL blur functions
// ============================================================================

// ============================================================================
// 1. Includes
// ============================================================================

#include "../Common.hlsl"
#include "../Rendering/Uv.hlsl"

// ============================================================================
// 2. Defines
// ============================================================================

/// BLUR_MAX_RADIUS defines the static upper bound for the loop unrolling.
///    
///    PERFORMANCE NOTE: 
///    Keep this value as low as possible. The compiler uses this constant to 
///    allocate registers. Setting this to an unnecessarily high value (e.g., 128) 
///    can lead to "Register Spilling," significantly degrading GPU performance.
///
///    USAGE:
///    #define BLUR_MAX_RADIUS 8
///    #include "<path>/Blur.hlsl"
#ifndef BLUR_MAX_RADIUS
    #define BLUR_MAX_RADIUS 16
#endif

#define BLUR_BUFFER_SIZE BLUR_MAX_RADIUS + 1

// ============================================================================
// 3. Utility Functions
// ============================================================================

/// @brief Applies a 1D box blur along a given direction.
///        All samples within the radius contribute equally, making this a simple and fast blur.
/// @param BlitTexture The input texture to read from
/// @param samplerState The sampler state used for texture access
/// @param texcoord UV coordinate of the current pixel
/// @param direction Blur direction, e.g., (1,0) for horizontal or (0,1) for vertical
/// @param texelSize Size of a single texel in UV space
/// @param cutoff If true, samples outside UV range are discarded to avoid leaking colors
/// @param radius Number of samples taken to each side of the center pixel
/// @return The averaged result of all valid samples in the 1D box kernel
float4 BoxBlur(TEXTURE2D(BlitTexture), SAMPLER(samplerState), float2 texcoord, float2 direction, float2 texelSize, bool cutoff, int radius)
{
    float4 result = SAMPLE_TEXTURE2D(BlitTexture, samplerState, texcoord);
    //float weight = 1.0 / (2 * radius + 1);
    float count = 1.0;

    [unroll(BLUR_MAX_RADIUS)]
    for (int i = 1; i <= BLUR_MAX_RADIUS; ++i)
    {
        if (i > radius) break;

        float2 offset = direction * float(i) * texelSize;

        float2 uvPos = texcoord + offset;
        if (UvInBounds(uvPos, cutoff))
        {
            result += SAMPLE_TEXTURE2D(BlitTexture, samplerState, uvPos); // * weight;
            count += 1.0;
        }

        float2 uvNeg = texcoord - offset;
        if (UvInBounds(uvNeg, cutoff))
        {
            result += SAMPLE_TEXTURE2D(BlitTexture, samplerState, uvNeg); // * weight;
            count += 1.0;
        }
    }

    return result / count;
}

/// @brief Performs a separable approximation of a 2D box blur by applying
///        one horizontal and one vertical 1D blur pass, averaging results.
///        This is cheaper than a full 2D kernel.
/// @param BlitTexture Texture being blurred
/// @param samplerState Sampler used for texture reads
/// @param texcoord Current pixel UV
/// @param texelSize UV size of one texel
/// @param cutoff If enabled, prevents sampling outside valid UV bounds
/// @param radius Kernel radius for each 1D blur pass
/// @return The average of horizontal and vertical box-blur passes, approximating a 2D box blur
float4 BoxBlurSeparableApprox(TEXTURE2D(BlitTexture), SAMPLER(samplerState), float2 texcoord, float2 texelSize, bool cutoff, int radius)
{
    float4 result = (float4) 0;
    result += BoxBlur(BlitTexture, samplerState, texcoord, float2(1, 0), texelSize, cutoff, radius);
    result += BoxBlur(BlitTexture, samplerState, texcoord, float2(0, 1), texelSize, cutoff, radius);
    return result * 0.5;
}

/// @brief Computes a full 2D box blur by sampling in both X and Y directions.
///        All samples within the square kernel have equal weight.
///        Produces a uniform blur but is more expensive than the separable version.
/// @param BlitTexture Texture that will be blurred
/// @param samplerState Sampler state for texture access
/// @param texcoord UV of the current pixel
/// @param texelSize UV size of a texel
/// @param cutoff If true, samples outside UV range are ignored
/// @param radius Box kernel radius in both dimensions
/// @return Normalized sum of all box-filter samples within the square kernel
float4 BoxBlur2d(TEXTURE2D(BlitTexture), SAMPLER(samplerState), float2 texcoord, float2 texelSize, bool cutoff, int radius)
{
    float4 result = 0;
    //float weight = 1.0 / pow(2.0 * radius + 1.0, 2.0);
    float count = 0;

    [loop]
    for (int y = -radius;y <=radius; y++)
    {
        [loop]
        for (int x = -radius;x <=radius; x++)
        {
            float2 uv = texcoord + float2(x, y) * texelSize;
            if (UvInBounds(uv, cutoff))
            {
                result += SAMPLE_TEXTURE2D(BlitTexture, samplerState, uv); // * weight;
                count += 1.0;
            }
        }
    }

    return result / count;
}

/// @brief Applies a 1D Gaussian blur along a specified direction using a supplied kernel.
///        Gaussian weights emphasize the center and fade smoothly outward.
/// @param BlitTexture Source texture
/// @param samplerState Sampler used when reading texels
/// @param texcoord UV coordinate of the pixel
/// @param direction Direction of blur (e.g., horizontal or vertical)
/// @param texelSize UV size of a texel
/// @param cutoff If true, samples outside valid UV area are excluded
/// @param kernel Precomputed Gaussian kernel values for offsets 0..radius
/// @param radius Number of Gaussian samples to each side
/// @return Gaussian-filtered pixel value normalized by sum of valid weights
float4 GaussianBlur(TEXTURE2D(BlitTexture), SAMPLER(samplerState), float2 texcoord, float2 direction, float2 texelSize, bool cutoff, float kernel[BLUR_BUFFER_SIZE], int radius)
{
    float sum = kernel[0];
    float4 result = SAMPLE_TEXTURE2D(BlitTexture, samplerState, texcoord) * sum;

    [unroll(BLUR_MAX_RADIUS)]
    for (int i = 1; i <= BLUR_MAX_RADIUS; ++i)
    {
        if (i > radius) break;

        float w = kernel[i];
        float2 offset = direction * float(i) * texelSize;

        float2 uv = texcoord - offset;
        if (UvInBounds(uv, cutoff))
        {
            result += SAMPLE_TEXTURE2D(BlitTexture, samplerState, uv) * w;
            sum += w;
        }
        uv = texcoord + offset;
        if (UvInBounds(uv, cutoff))
        {
            result += SAMPLE_TEXTURE2D(BlitTexture, samplerState, uv) * w;
            sum += w;
        }
    }
            
    return result / sum;
}

/// @brief Approximates a full 2D Gaussian blur using two 1D passes.
/// @param BlitTexture Texture to blur
/// @param samplerState Sampler for texture access
/// @param texcoord UV of the processed pixel
/// @param texelSize Size of a texel in UV coordinates
/// @param cutoff Prevents reading outside UV boundaries if true
/// @param kernel Gaussian kernel containing weights for 0..radius
/// @param radius Blur radius
/// @return Average of horizontal and vertical Gaussian blur passes
float4 GaussianBlurSeparableApprox(TEXTURE2D(BlitTexture), SAMPLER(samplerState), float2 texcoord, float2 texelSize, bool cutoff, float kernel[BLUR_BUFFER_SIZE], int radius)
{
    float4 result = (float4) 0;
    result += GaussianBlur(BlitTexture, samplerState, texcoord, float2(1, 0), texelSize, cutoff, kernel, radius);
    result += GaussianBlur(BlitTexture, samplerState, texcoord, float2(0, 1), texelSize, cutoff, kernel, radius);
    return result * 0.5;
}

/// @brief Computes a full 2D Gaussian blur using a separable kernel product: weight(x,y) = kernel[x] * kernel[y].
///        Produces a high-quality isotropic blur.
/// @param BlitTexture Input texture
/// @param samplerState Sampler for texture reads
/// @param texcoord UV of the pixel
/// @param texelSize UV size of a texel
/// @param cutoff Reject UVs outside [0,1] range if true
/// @param kernel 1D Gaussian kernel
/// @param radius Kernel radius
/// @return Normalized weighted sum of all samples in the 2D Gaussian kernel
float4 GaussianBlur2D(TEXTURE2D(BlitTexture), SAMPLER(samplerState), float2 texcoord, float2 texelSize, bool cutoff, float kernel[BLUR_BUFFER_SIZE], int radius)
{
    float4 result = 0;
    float sum = 0;

    [loop]
    for (int y = -radius;y <=radius;++y)
    {
        [loop]
        for (int x = -radius;x <=radius;++x)
        {
            float2 offset = float2(x, y) * texelSize;

            float w = kernel[abs(x)] * kernel[abs(y)];

            float2 uv = texcoord + offset;
            if (UvInBounds(uv, cutoff))
            {
                result += SAMPLE_TEXTURE2D(BlitTexture, samplerState, uv) * w;
                sum += w;
            }
        }
    }

    return result / sum;
}

/// @brief Applies a 1D tent filter blur along a specified direction.
///        Weights decrease linearly from the center outward.
/// @param BlitTexture Input texture to sample from
/// @param samplerState Sampler state for texture reads
/// @param texcoord UV of the current pixel
/// @param direction Blur direction (e.g., (1,0) horizontal, (0,1) vertical)
/// @param texelSize Size of one texel in UV space
/// @param cutoff If true, excludes pixels outside [0,1] UV range
/// @param radius Blur radius defining kernel size
/// @return Tent-filtered pixel color along the specified axis
float4 TentBlur(TEXTURE2D(BlitTexture), SAMPLER(samplerState), float2 texcoord, float2 direction, float2 texelSize, bool cutoff, int radius)
{
    float sum = radius + 1;
    float4 result = SAMPLE_TEXTURE2D(BlitTexture, samplerState, texcoord) * sum;

    [unroll(BLUR_MAX_RADIUS)]
    for (int i = 1; i <= BLUR_MAX_RADIUS; ++i)
    {
        if (i > radius) break;

        float w = radius - i + 1;
        float2 offset = direction * float(i) * texelSize;

        float2 uv = texcoord - offset;
        if (UvInBounds(uv, cutoff))
        {
            result += SAMPLE_TEXTURE2D(BlitTexture, samplerState, uv) * w;
            sum += w;
        }
        uv = texcoord + offset;
        if (UvInBounds(uv, cutoff))
        {
            result += SAMPLE_TEXTURE2D(BlitTexture, samplerState, uv) * w;
            sum += w;
        }
    }

    return result / sum;
}

/// @brief Applies a separable approximation of a 2D tent blur.
///        Performs horizontal and vertical 1D tent blurs and averages them.
/// @param BlitTexture Input texture
/// @param samplerState Sampler for texture reads
/// @param texcoord UV of the current pixel
/// @param texelSize Size of one texel in UV space
/// @param cutoff If true, excludes samples outside [0,1] UV
/// @param radius Tent blur radius
/// @return Average of horizontal and vertical tent blur passes
float4 TentBlurSeparableApprox(TEXTURE2D(BlitTexture), SAMPLER(samplerState), float2 texcoord, float2 texelSize, bool cutoff, int radius)
{
    float4 result = (float4) 0;
    result += TentBlur(BlitTexture, samplerState, texcoord, float2(1, 0), texelSize, cutoff, radius);
    result += TentBlur(BlitTexture, samplerState, texcoord, float2(0, 1), texelSize, cutoff, radius);
    return result * 0.5;
}

/// @brief Performs a full 2D tent blur using a square kernel.
///        Weights decrease linearly with distance from the center pixel.
/// @param BlitTexture Input texture
/// @param samplerState Sampler for reading texels
/// @param texcoord UV of the current pixel
/// @param texelSize Size of one texel in UV space
/// @param cutoff If true, discards samples outside [0,1] UV
/// @param radius Blur radius defining kernel extent
/// @return Normalized tent-filtered pixel color using a 2D kernel
float4 TentBlur2D(TEXTURE2D(BlitTexture), SAMPLER(samplerState), float2 texcoord, float2 texelSize, bool cutoff, int radius)
{
    float4 result = (float4) 0;
    float sum = 0.0;

    [loop]
    for (int y = -radius;y <=radius;++y)
    {
        [loop]
        for (int x = -radius;x <=radius;++x)
        {
            float w = float((radius + 1) - max(abs(x), abs(y)));

            w = max(w, 0.0);

            float2 offset = float2(x, y) * texelSize;
            float2 uv = texcoord + offset;

            if (UvInBounds(uv, cutoff))
            {
                result += SAMPLE_TEXTURE2D(BlitTexture, samplerState, uv) * w;
                sum += w;
            }
        }
    }

    return result / sum;
}

/// @brief Applies a Kawase blur, an efficient multi-tap downsample-style blur.
///        Offsets samples outward in successive passes with decreasing weight,
///        producing a soft bloom-like effect at low cost.
/// @param BlitTexture Texture to blur
/// @param samplerState Sampler state for texture fetches
/// @param texcoord UV coordinate of the pixel
/// @param texelSize Size of one texel in UV space
/// @param cutoff If true, ignores samples outside [0,1] UV
/// @param radius Number of passes; higher values increase blur spread
/// @return Kawase-blurred pixel color
float4 KawaseBlur(TEXTURE2D(BlitTexture), SAMPLER(samplerState), float2 texcoord, float2 texelSize, bool cutoff, int radius)
{
    float4 result = 0;
    float totalWeight = 0;

    [unroll(BLUR_MAX_RADIUS)]
    for (int i = 0; i < BLUR_MAX_RADIUS; ++i)
    {
        if (i > radius) break;

        float passWeight = 1.0 / (i + 1.0);
        float2 scaledTexel = texelSize * (i + 1);

        float2 offsets[4] =
        {
            float2(1, 1),
                    float2(-1, 1),
                    float2(1, -1),
                    float2(-1, -1)
        };

        [unroll]
        for (int j = 0; j < 4; ++j)
        {
            float2 uv = texcoord + offsets[j] * scaledTexel;
            if (UvInBounds(uv, cutoff))
            {
                float4 s = SAMPLE_TEXTURE2D(BlitTexture, samplerState, uv);
                result += s * passWeight;
                totalWeight += passWeight;
            }
        }
    }

    return result / max(totalWeight, 1e-5);
}

/// @brief Applies a 1D directional blur in the given direction using the selected blur mode.
/// @param BlitTexture Source texture
/// @param samplerState Sampler state for texture access
/// @param texcoord UV coordinate to sample
/// @param direction Normalized blur direction
/// @param blurMode 0=None, 1=Box, 2=Gaussian, 3=Tent, 4=Kawase
/// @param texelSize Size of one pixel in UV space
/// @param cutoff If true, ignores samples outside [0,1] UV
/// @param kernel Kernel used for Gaussian blur (ignored for other modes)
/// @param radius Blur radius / number of samples
/// @return Blurred pixel along the specified direction
float4 DirectionalBlur(TEXTURE2D(BlitTexture), SAMPLER(samplerState), float2 texcoord, float2 direction, int blurMode, float2 texelSize, bool cutoff, float kernel[BLUR_BUFFER_SIZE], int radius)
{
    float4 color = (float4) 0;

    [branch]
    switch (blurMode)
    {
        case 0:
            color = SAMPLE_TEXTURE2D(BlitTexture, samplerState, texcoord);
            break;
        case 1:
            color = BoxBlur(BlitTexture, samplerState, texcoord, direction, texelSize, cutoff, radius);
            break;
        case 2:
            color = GaussianBlur(BlitTexture, samplerState, texcoord, direction, texelSize, cutoff, kernel, radius);
            break;
        case 3:
            color = TentBlur(BlitTexture, samplerState, texcoord, direction, texelSize, cutoff, radius);
            break;
        case 4:
                    //color = KawaseBlur(BlitTexture, samplerState, texcoord, texelSize, cutoff, radius);
            break;
    }

    return color;
}

/// @brief Performs a separable 1D blur approximation along horizontal or vertical axis.
/// @param BlitTexture Source texture
/// @param samplerState Sampler used for texture access
/// @param texcoord UV to sample
/// @param blurMode Blur algorithm selector
/// @param texelSize UV size of a pixel
/// @param cutoff Early stop flag for out-of-range sampling
/// @param kernel Gaussian kernel weights
/// @param radius Blur radius
/// @return Approximate separable blurred color
float4 SeparableBlurApprox(TEXTURE2D(BlitTexture), SAMPLER(samplerState), float2 texcoord, int blurMode, float2 texelSize, bool cutoff, float kernel[BLUR_BUFFER_SIZE], int radius)
{
    float4 color = (float4) 0;

    [branch]
    switch (blurMode)
    {
        case 0:
            color = SAMPLE_TEXTURE2D(BlitTexture, samplerState, texcoord);
            break;
        case 1:
            color = BoxBlurSeparableApprox(BlitTexture, samplerState, texcoord, texelSize, cutoff, radius);
            break;
        case 2:
            color = GaussianBlurSeparableApprox(BlitTexture, samplerState, texcoord, texelSize, cutoff, kernel, radius);
            break;
        case 3:
            color = TentBlurSeparableApprox(BlitTexture, samplerState, texcoord, texelSize, cutoff, radius);
            break;
        case 4:
            color = KawaseBlur(BlitTexture, samplerState, texcoord, texelSize, cutoff, radius);
            break;
    }

    return color;
}

/// @brief Performs a full 2D convolution blur.
/// @param BlitTexture Source texture
/// @param samplerState Sampler for texture reads
/// @param texcoord UV position to sample
/// @param blurMode Blur type
/// @param texelSize Pixel step in UV space
/// @param cutoff Early exit flag
/// @param kernel Kernel for Gaussian blur
/// @param radius Blur radius
/// @return Fully 2D blurred color
float4 Blur2D(TEXTURE2D(BlitTexture), SAMPLER(samplerState), float2 texcoord, int blurMode, float2 texelSize, bool cutoff, float kernel[BLUR_BUFFER_SIZE], int radius)
{
    float4 color = (float4) 0;

    [branch]
    switch (blurMode)
    {
        case 0:
            color = SAMPLE_TEXTURE2D(BlitTexture, samplerState, texcoord);
            break;
        case 1:
            color = BoxBlur2d(BlitTexture, samplerState, texcoord, texelSize, cutoff, radius);
            break;
        case 2:
            color = GaussianBlur2D(BlitTexture, samplerState, texcoord, texelSize, cutoff, kernel, radius);
            break;
        case 3:
            color = TentBlur2D(BlitTexture, samplerState, texcoord, texelSize, cutoff, radius);
            break;
        case 4:
            color = KawaseBlur(BlitTexture, samplerState, texcoord, texelSize, cutoff, radius);
            break;
    }

    return color;
}

/// @brief Applies a radial blur centered on the screen origin.
/// @param BlitTexture Input texture
/// @param samplerState Sampler state
/// @param texcoord UV of the pixel
/// @param blurMode Blur type
/// @param texelSize Pixel step size in UV
/// @param cutoff Early stop flag
/// @param kernel Gaussian kernel weights
/// @param radius Number of samples
/// @return Radially blurred color
float4 RadialBlur(TEXTURE2D(BlitTexture), SAMPLER(samplerState), float2 texcoord, int blurMode, float2 texelSize, bool cutoff, float kernel[BLUR_BUFFER_SIZE], int radius)
{
    float2 direction = texcoord * 2.0 - 1.0;

    float4 color = (float4) 0;

    [branch]
    switch (blurMode)
    {
        case 0:
            color = SAMPLE_TEXTURE2D(BlitTexture, samplerState, texcoord);
            break;
        case 1:
            color = BoxBlur(BlitTexture, samplerState, texcoord, direction, texelSize, cutoff, radius);
            break;
        case 2:
            color = GaussianBlur(BlitTexture, samplerState, texcoord, direction, texelSize, cutoff, kernel, radius);
            break;
        case 3:
            color = TentBlur(BlitTexture, samplerState, texcoord, direction, texelSize, cutoff, radius);
            break;
        case 4:
                    //color = KawaseBlur(BlitTexture, samplerState, texcoord, texelSize, cutoff, radius);
            break;
    }

    return color;
}

/// @brief Applies a band-pass filter isolating mid-frequency image details.
/// @remarks Performs two separable blur passes: 
/// a short-radius blur capturing fine details and a long-radius blur capturing low frequencies.
/// The band-pass is obtained by subtracting the long blur from the short blur, clamping negatives to zero.
/// @param BlitTexture Source texture
/// @param samplerState Sampler state for texture access
/// @param blurMode Blur kernel mode (used by SeparableBlurApprox)
/// @param texcoord UV coordinates to sample
/// @param texelSize Pixel size in UV space
/// @param shortKernel Kernel for short-radius blur
/// @param shortRadius Radius of the short blur
/// @param longKernel Kernel for long-radius blur
/// @param longRadius Radius of the long blur
/// @return float3 containing mid-frequency (band-pass) filtered result
float3 BandPass(TEXTURE2D(BlitTexture), SAMPLER(samplerState), int blurMode, float2 texcoord, float2 texelSize, float shortKernel[BLUR_BUFFER_SIZE], int shortRadius, float longKernel[BLUR_BUFFER_SIZE], int longRadius)
{
    float3 blurShort = SeparableBlurApprox(BlitTexture, samplerState, texcoord, blurMode, texelSize, true, shortKernel, shortRadius).rgb;
    float3 blurLong = SeparableBlurApprox(BlitTexture, samplerState, texcoord, blurMode, texelSize, true, longKernel, longRadius).rgb;
    return max((float3) 0, blurShort - blurLong);
}