using Rayforge.Core.ManagedResources.Abstractions;
using System;
using System.Runtime.InteropServices;
using UnityEngine;

namespace Rayforge.Core.Rendering.Blitter
{
    /// <summary>
    /// Parameter struct for channel blitting operations.
    /// Used to specify per-channel mapping and source rectangle within the source texture.
    /// Layout is sequential to match GPU cbuffer layout.
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public struct ChannelBlitParams : IComputeData<ChannelBlitParams>
    {
        /// <summary>Target red channel is mapped from this source channel.</summary>
        public Channel R;
        /// <summary>Target green channel is mapped from this source channel.</summary>
        public Channel G;
        /// <summary>Target blue channel is mapped from this source channel.</summary>
        public Channel B;
        /// <summary>Target alpha channel is mapped from this source channel.</summary>
        public Channel A;

        /// <summary>
        /// Source texture to use for target red channel.
        /// Only relevant for ComputeBlit. Raster blits always use the single source texture.
        /// </summary>
        public SourceTexture RSource;
        /// <summary>
        /// Source texture to use for target green channel.
        /// Only relevant for ComputeBlit. Raster blits always use the single source texture.
        /// </summary>
        public SourceTexture GSource;
        /// <summary>
        /// Source texture to use for target blue channel.
        /// Only relevant for ComputeBlit. Raster blits always use the single source texture.
        /// </summary>
        public SourceTexture BSource;
        /// <summary>
        /// Source texture to use for target alpha channel.
        /// Only relevant for ComputeBlit. Raster blits always use the single source texture.
        /// </summary>
        public SourceTexture ASource;

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

        /// <summary>Operations to apply to the red channel after sampling (e.g., invert, multiply).</summary>
        public ChannelOps ROps;
        /// <summary>Operations to apply to the green channel after sampling (e.g., invert, multiply).</summary>
        public ChannelOps GOps;
        /// <summary>Operations to apply to the blue channel after sampling (e.g., invert, multiply).</summary>
        public ChannelOps BOps;
        /// <summary>Operations to apply to the alpha channel after sampling (e.g., invert, multiply).</summary>
        public ChannelOps AOps;

        /// <summary>Red multiplier (used only if ROps has Multiply flag).</summary>
        public float RMultiplier;
        /// <summary>Green multiplier (used only if GOps has Multiply flag).</summary>
        public float GMultiplier;
        /// <summary>Blue multiplier (used only if BOps has Multiply flag).</summary>
        public float BMultiplier;
        /// <summary>Alpha multiplier (used only if AOps has Multiply flag).</summary>
        public float AMultiplier;

        /// <summary>
        /// Returns data as raw compute data struct.
        /// </summary>
        public ChannelBlitParams RawData => this;

        /// <summary>
        /// Sets the mapping for a specific target channel in the blit parameters.
        /// </summary>
        /// <param name="target">The target channel to set (R, G, B, A).</param>
        /// <param name="source">The source channel to map from.</param>
        /// <param name="sourceTexture">The source texture to use for this channel.</param>
        /// <param name="ops">Operations to apply to the channel after sampling (e.g., invert, multiply).</param>
        /// <param name="multiplier">Multiplier applied if the operations include Multiply.</param>
        /// <exception cref="ArgumentOutOfRangeException">Thrown if the target channel is not R, G, B, or A.</exception>
        public void SetChannel(Channel target, Channel source, SourceTexture sourceTexture, ChannelOps ops, float multiplier)
        {
            switch (target)
            {
                case Channel.R:
                    R = source;
                    RSource = sourceTexture;
                    ROps = ops;
                    RMultiplier = multiplier;
                    break;

                case Channel.G:
                    G = source;
                    GSource = sourceTexture;
                    GOps = ops;
                    GMultiplier = multiplier;
                    break;

                case Channel.B:
                    B = source;
                    BSource = sourceTexture;
                    BOps = ops;
                    BMultiplier = multiplier;
                    break;

                case Channel.A:
                    A = source;
                    ASource = sourceTexture;
                    AOps = ops;
                    AMultiplier = multiplier;
                    break;

                default:
                    throw new ArgumentOutOfRangeException(nameof(target), "Unknown channel");
            }
        }
    }
}