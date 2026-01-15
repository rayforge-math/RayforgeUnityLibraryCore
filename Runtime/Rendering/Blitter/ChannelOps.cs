using System;

namespace Rayforge.Core.Rendering.Blitter
{
    /// <summary>
    /// Channel operation flags for post-sample modifications.
    /// Use <see cref="Invert"/> to invert the channel value, <see cref="Multiply"/> to apply the multiplier.
    /// </summary>
    [Flags]
    public enum ChannelOps
    {
        /// <summary>No operation.</summary>
        None = 0,
        /// <summary>Invert the channel value (1.0 - value for raster, bitwise ~ for compute).</summary>
        Invert = 1 << 0,
        /// <summary>Multiply the channel value by the corresponding multiplier in ChannelBlitParams.</summary>
        Multiply = 1 << 1
    }
}