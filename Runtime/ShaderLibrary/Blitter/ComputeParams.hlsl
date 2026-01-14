#pragma once

CBUFFER_START(_ComputeBlitParams)
uint2 _BlitDest_Res;
uint _BlitStretchToFit;
int _ComputeBlitParams_padding;
float4 _BlitTexture0_TexelSize;
float4 _BlitTexture1_TexelSize;
float4 _BlitTexture2_TexelSize;
float4 _BlitTexture3_TexelSize;
CBUFFER_END

Texture2D<uint4> _BlitTexture0;
Texture2D<uint4> _BlitTexture1;
Texture2D<uint4> _BlitTexture2;
Texture2D<uint4> _BlitTexture3;
RWTexture2D<uint4> _BlitDestination;