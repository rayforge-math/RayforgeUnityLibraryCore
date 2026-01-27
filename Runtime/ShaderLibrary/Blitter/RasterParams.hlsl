#pragma once

#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"

TEXTURE2D(_BlitTexture);
CBUFFER_START(UnityPerMaterial)
float4 _BlitTexture_TexelSize;
CBUFFER_END