#pragma once

#include "../../ShaderLibrary/Sampling.hlsl"
#include "../../ShaderLibrary/Rendering/FullscreenTriangle.hlsl"

void SetupBlitPipeline(uint id, out float4 positionCS, out float2 texcoord)
{
    FullscreenTriangle(id, positionCS, texcoord);
    texcoord = texcoord * _BlitScaleBias.xy + _BlitScaleBias.zw;
}

float4 SampleBlitTexture(float2 texcoord)
{
    float4 sample = SAMPLE_TEXTURE2D(_BlitTexture, sampler_LinearClamp, texcoord);
                
    float4 dest = (float4) 0;
    if (_ChannelMapping.r != BLIT_CH_NONE)
        dest.r = sample[_ChannelMapping.r];
    if (_ChannelMapping.g != BLIT_CH_NONE)
        dest.g = sample[_ChannelMapping.g];
    if (_ChannelMapping.b != BLIT_CH_NONE)
        dest.b = sample[_ChannelMapping.b];
    if (_ChannelMapping.a != BLIT_CH_NONE)
        dest.a = sample[_ChannelMapping.a];
    
    return dest;
}