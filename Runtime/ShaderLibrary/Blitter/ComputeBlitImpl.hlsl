#pragma once

uint2 GetCoords(float2 pixelCoords, float4 texelSize, int stretch, float2 outputSize)
{
    if (stretch != 0)
    {
        float2 scale = texelSize.zw / outputSize;
        return uint2(pixelCoords * scale);
    }
    else
    {
        return uint2(pixelCoords * _BlitScaleBias.xy + texelSize.zw * _BlitScaleBias.zw);
    }
}

uint4 SampleBlitTextures(uint2 pixelCoords)
{
    bool useTex0 = (_SrcTextures.r == TEX_0) || (_SrcTextures.g == TEX_0) || (_SrcTextures.b == TEX_0) || (_SrcTextures.a == TEX_0);
    bool useTex1 = (_SrcTextures.r == TEX_1) || (_SrcTextures.g == TEX_1) || (_SrcTextures.b == TEX_1) || (_SrcTextures.a == TEX_1);
    bool useTex2 = (_SrcTextures.r == TEX_2) || (_SrcTextures.g == TEX_2) || (_SrcTextures.b == TEX_2) || (_SrcTextures.a == TEX_2);
    bool useTex3 = (_SrcTextures.r == TEX_3) || (_SrcTextures.g == TEX_3) || (_SrcTextures.b == TEX_3) || (_SrcTextures.a == TEX_3);

    uint4 samples[4] = {
        uint4(0, 0, 0, 0),
        uint4(0, 0, 0, 0),
        uint4(0, 0, 0, 0),
        uint4(0, 0, 0, 0)
    };
    
    uint2 coords0 = GetCoords(pixelCoords, _BlitTexture0_TexelSize, _BlitStretchToFit, _BlitDest_Res);
    uint2 coords1 = GetCoords(pixelCoords, _BlitTexture1_TexelSize, _BlitStretchToFit, _BlitDest_Res);
    uint2 coords2 = GetCoords(pixelCoords, _BlitTexture2_TexelSize, _BlitStretchToFit, _BlitDest_Res);
    uint2 coords3 = GetCoords(pixelCoords, _BlitTexture3_TexelSize, _BlitStretchToFit, _BlitDest_Res);

    if (useTex0)
        samples[TEX_0] = _BlitTexture0[coords0];
    if (useTex1)
        samples[TEX_1] = _BlitTexture1[coords1];
    if (useTex2)
        samples[TEX_2] = _BlitTexture2[coords2];
    if (useTex3)
        samples[TEX_3] = _BlitTexture3[coords3];

    uint4 dest;
    dest.r = 
        (_SrcTextures.r == TEX_0) ? samples[TEX_0][_SrcChannels.r] :
        (_SrcTextures.r == TEX_1) ? samples[TEX_1][_SrcChannels.r] :
        (_SrcTextures.r == TEX_2) ? samples[TEX_2][_SrcChannels.r] :
        (_SrcTextures.r == TEX_3) ? samples[TEX_3][_SrcChannels.r] : 0;

    dest.g = 
        (_SrcTextures.g == TEX_0) ? samples[TEX_0][_SrcChannels.g] :
        (_SrcTextures.g == TEX_1) ? samples[TEX_1][_SrcChannels.g] :
        (_SrcTextures.g == TEX_2) ? samples[TEX_2][_SrcChannels.g] :
        (_SrcTextures.g == TEX_3) ? samples[TEX_3][_SrcChannels.g] : 0;

    dest.b = 
        (_SrcTextures.b == TEX_0) ? samples[TEX_0][_SrcChannels.b] :
        (_SrcTextures.b == TEX_1) ? samples[TEX_1][_SrcChannels.b] :
        (_SrcTextures.b == TEX_2) ? samples[TEX_2][_SrcChannels.b] :
        (_SrcTextures.b == TEX_3) ? samples[TEX_3][_SrcChannels.b] : 0;

    dest.a = 
        (_SrcTextures.a == TEX_0) ? samples[TEX_0][_SrcChannels.a] :
        (_SrcTextures.a == TEX_1) ? samples[TEX_1][_SrcChannels.a] :
        (_SrcTextures.a == TEX_2) ? samples[TEX_2][_SrcChannels.a] :
        (_SrcTextures.a == TEX_3) ? samples[TEX_3][_SrcChannels.a] : 0;
    
    dest.r = (_ChannelOps.r & BLIT_CHOP_INV) != 0 ? ~dest.r : dest.r;
    dest.g = (_ChannelOps.g & BLIT_CHOP_INV) != 0 ? ~dest.g : dest.g;
    dest.b = (_ChannelOps.b & BLIT_CHOP_INV) != 0 ? ~dest.b : dest.b;
    dest.a = (_ChannelOps.a & BLIT_CHOP_INV) != 0 ? ~dest.a : dest.a;

    return dest;
}