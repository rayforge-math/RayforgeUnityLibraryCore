#pragma once

#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"

/// @brief Parameters controlling channel remapping and source coordinate transformation.
///
/// @param _SrcChannels
/// Per-output-channel source channel selection (RGBA).
/// Each component specifies which source channel to copy:
/// 0 = R, 1 = G, 2 = B, 3 = A.
///
/// @param _SrcTextures
/// Per-output-channel source texture selection (RGBA).
/// Each component specifies which source texture to sample from
/// (e.g. 0–3 for up to four bound textures).
///
/// @param _ChannelOps
/// Per-output-channel operations to apply after sampling (RGBA).
/// Each component is a bitmask or enum specifying operations like invert, multiply, etc.
///
/// @param _BlitScaleBias
/// Affine transformation applied to source texture coordinates:
/// - xy = scale (scales the sampling region down)
/// - zw = bias  (shifts the sampling region)
///
/// Source coordinates are computed as:
///     srcCoord = dstCoord * _BlitScaleBias.xy + _BlitScaleBias.zw
CBUFFER_START(_ChannelBlitParams)
int4 _SrcChannels;
int4 _SrcTextures;
int4 _ChannelOps;
float4 _ChannelMults;
float4 _BlitScaleBias;
CBUFFER_END

