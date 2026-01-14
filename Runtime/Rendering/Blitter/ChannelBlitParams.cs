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
        public Channel SrcChannel;

        /// <summary>Source texture to use.</summary>
        public SourceTexture SrcTexture;

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
        /// Allows indexed access to the channel parameters (R, G, B, A) using a <see cref="Channel"/> enum.
        /// </summary>
        /// <param name="ch">The target channel to access (R, G, B, or A).</param>
        /// <returns>The <see cref="ChannelData"/> corresponding to the specified channel.</returns>
        /// <exception cref="ArgumentOutOfRangeException">
        /// Thrown if <paramref name="ch"/> is not one of the valid channels (R, G, B, A).
        /// </exception>
        public ChannelData this[Channel ch] => ch switch
        {
            Channel.R => R,
            Channel.G => G,
            Channel.B => B,
            Channel.A => A,
            _ => throw new ArgumentOutOfRangeException(nameof(ch))
        };

        /// <summary>
        /// Sets the parameters for a specific target channel using a <see cref="ChannelData"/> struct.
        /// </summary>
        /// <param name="ch">Target channel to set (R, G, B, A).</param>
        /// <param name="data">The <see cref="ChannelData"/> containing source channel, source texture, ops, and multiplier.</param>
        public void SetChannelData(Channel ch, ChannelData data)
        {
            switch (ch)
            {
                case Channel.R: R = data; break;
                case Channel.G: G = data; break;
                case Channel.B: B = data; break;
                case Channel.A: A = data; break;
                default: throw new ArgumentOutOfRangeException(nameof(ch));
            }
        }

        /// <summary>
        /// Bias applied to source coordinates after scaling.
        /// Shifts the sampling window.
        /// </summary>
        public Vector2 bias;
    }

    [StructLayout(LayoutKind.Sequential)]
    struct ChannelBlitterCB : IComputeData<ChannelBlitterCB>
    {
        public Vector4 SrcChannels;
        public Vector4 SrcTextures;
        public Vector4 ChannelOps;
        public Vector4 ChannelMults;
        public Vector4 ScaleBias;

        public ChannelBlitterCB RawData => this;

        public ChannelBlitterCB(ChannelBlitParams param)
        {
            SrcChannels = new Vector4(
                (int)param.R.SrcChannel,
                (int)param.G.SrcChannel,
                (int)param.B.SrcChannel,
                (int)param.A.SrcChannel
            );
            SrcTextures = new Vector4(
                (int)param.R.SrcTexture,
                (int)param.G.SrcTexture,
                (int)param.B.SrcTexture,
                (int)param.A.SrcTexture
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