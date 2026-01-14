using Rayforge.Core.ManagedResources.Abstractions;
using System;
using System.Runtime.InteropServices;
using UnityEngine;

namespace Rayforge.Core.Rendering.Blitter
{
    /// <summary>
    /// Represents all data needed for a single target channel in a blit operation.
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public struct ChannelData
    {
        /// <summary>Source channel to map from.</summary>
        public Channel Source;

        /// <summary>Source texture to use.</summary>
        public SourceTexture Texture;

        /// <summary>Operations to apply after sampling (e.g., invert, multiply).</summary>
        public ChannelOps Ops;

        /// <summary>Multiplier applied if Ops includes Multiply.</summary>
        public float Multiplier;
    }

    /// <summary>
    /// Parameter struct for channel blitting operations.
    /// Used to specify per-channel mapping and source rectangle within the source texture.
    /// Layout is sequential to match GPU cbuffer layout.
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public struct ChannelBlitParams
    {
        /// <summary>Red channel parameters.</summary>
        public ChannelData R;

        /// <summary>Green channel parameters.</summary>
        public ChannelData G;

        /// <summary>Blue channel parameters.</summary>
        public ChannelData B;

        /// <summary>Alpha channel parameters.</summary>
        public ChannelData A;

        /// <summary>
        /// Scale applied to source coordinates before bias.
        /// Scale < 1 shrinks sampling window, >1 expands.
        /// </summary>
        public Vector2 scale;

        /// <summary>
        /// Bias applied to source coordinates after scaling.
        /// Shifts the sampling window.
        /// </summary>
        public Vector2 bias;
    }

    [StructLayout(LayoutKind.Sequential)]
    struct ChannelBlitterCB : IComputeData<ChannelBlitterCB>
    {
        public Vector4 ChannelMapping;
        public Vector4 ChannelSource;
        public Vector4 ChannelOps;
        public Vector4 ChannelMults;
        public Vector4 ScaleBias;

        public ChannelBlitterCB RawData => this;

        public ChannelBlitterCB(ChannelBlitParams param)
        {
            ChannelMapping = new Vector4(
                (int)param.R.Source,
                (int)param.G.Source,
                (int)param.B.Source,
                (int)param.A.Source
            );
            ChannelSource = new Vector4(
                (int)param.R.Texture,
                (int)param.G.Texture,
                (int)param.B.Texture,
                (int)param.A.Texture
            );
            ChannelOps = new Vector4(
                (int)param.R.Ops,
                (int)param.G.Ops,
                (int)param.B.Ops,
                (int)param.A.Ops
            );
            ChannelMults = new Vector4(
                param.R.Multiplier,
                param.G.Multiplier,
                param.B.Multiplier,
                param.A.Multiplier
            );
            ScaleBias = new Vector4(
                param.scale.x, param.scale.y,
                param.bias.x, param.bias.y
            );
        }
    }
}