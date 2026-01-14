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
    bool useTex0 = (_ChannelSource.r == 0) || (_ChannelSource.g == 0) || (_ChannelSource.b == 0) || (_ChannelSource.a == 0);
    bool useTex1 = (_ChannelSource.r == 1) || (_ChannelSource.g == 1) || (_ChannelSource.b == 1) || (_ChannelSource.a == 1);
    bool useTex2 = (_ChannelSource.r == 2) || (_ChannelSource.g == 2) || (_ChannelSource.b == 2) || (_ChannelSource.a == 2);
    bool useTex3 = (_ChannelSource.r == 3) || (_ChannelSource.g == 3) || (_ChannelSource.b == 3) || (_ChannelSource.a == 3);

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
        samples[0] = _BlitTexture0[coords0];
    if (useTex1)
        samples[1] = _BlitTexture1[coords1];
    if (useTex2)
        samples[2] = _BlitTexture2[coords2];
    if (useTex3)
        samples[3] = _BlitTexture3[coords3];

    uint4 dest;
    dest.r = 
        (_ChannelSource.r == 0) ? samples[0][_ChannelMapping.r] :
        (_ChannelSource.r == 1) ? samples[1][_ChannelMapping.r] :
        (_ChannelSource.r == 2) ? samples[2][_ChannelMapping.r] :
        (_ChannelSource.r == 3) ? samples[3][_ChannelMapping.r] : 0;

    dest.g = 
        (_ChannelSource.g == 0) ? samples[0][_ChannelMapping.g] :
        (_ChannelSource.g == 1) ? samples[1][_ChannelMapping.g] :
        (_ChannelSource.g == 2) ? samples[2][_ChannelMapping.g] :
        (_ChannelSource.g == 3) ? samples[3][_ChannelMapping.g] : 0;

    dest.b = 
        (_ChannelSource.b == 0) ? samples[0][_ChannelMapping.b] :
        (_ChannelSource.b == 1) ? samples[1][_ChannelMapping.b] :
        (_ChannelSource.b == 2) ? samples[2][_ChannelMapping.b] :
        (_ChannelSource.b == 3) ? samples[3][_ChannelMapping.b] : 0;

    dest.a = 
        (_ChannelSource.a == 0) ? samples[0][_ChannelMapping.a] :
        (_ChannelSource.a == 1) ? samples[1][_ChannelMapping.a] :
        (_ChannelSource.a == 2) ? samples[2][_ChannelMapping.a] :
        (_ChannelSource.a == 3) ? samples[3][_ChannelMapping.a] : 0;
    
    dest.r = (_ChannelOps.r & BLIT_CHOP_INV) != 0 ? ~dest.r : dest.r;
    dest.g = (_ChannelOps.g & BLIT_CHOP_INV) != 0 ? ~dest.g : dest.g;
    dest.b = (_ChannelOps.b & BLIT_CHOP_INV) != 0 ? ~dest.b : dest.b;
    dest.a = (_ChannelOps.a & BLIT_CHOP_INV) != 0 ? ~dest.a : dest.a;

    return dest;
}