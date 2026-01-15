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
    if (_SrcChannels.r != BLIT_CH_NONE)
        dest.r = sample[_SrcChannels.r];
    if (_SrcChannels.g != BLIT_CH_NONE)
        dest.g = sample[_SrcChannels.g];
    if (_SrcChannels.b != BLIT_CH_NONE)
        dest.b = sample[_SrcChannels.b];
    if (_SrcChannels.a != BLIT_CH_NONE)
        dest.a = sample[_SrcChannels.a];
    
    dest.r = (_ChannelOps.r & BLIT_CHOP_INV) != 0 ? (1.0 - dest.r) : dest.r;
    dest.g = (_ChannelOps.g & BLIT_CHOP_INV) != 0 ? (1.0 - dest.g) : dest.g;
    dest.b = (_ChannelOps.b & BLIT_CHOP_INV) != 0 ? (1.0 - dest.b) : dest.b;
    dest.a = (_ChannelOps.a & BLIT_CHOP_INV) != 0 ? (1.0 - dest.a) : dest.a;
    
    dest.r *= (_ChannelOps.r & BLIT_CHOP_MULT) != 0 ? _ChannelMults.r : 1.0;
    dest.g *= (_ChannelOps.g & BLIT_CHOP_MULT) != 0 ? _ChannelMults.g : 1.0;
    dest.b *= (_ChannelOps.b & BLIT_CHOP_MULT) != 0 ? _ChannelMults.b : 1.0;
    dest.a *= (_ChannelOps.a & BLIT_CHOP_MULT) != 0 ? _ChannelMults.a : 1.0;
    
    return dest;
}