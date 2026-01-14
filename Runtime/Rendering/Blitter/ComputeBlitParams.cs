using Rayforge.Core.ManagedResources.Abstractions;
using System.Runtime.InteropServices;
using UnityEngine;

namespace Rayforge.Core.Rendering.Blitter
{
    /// <summary>
    /// Struct for ComputeBlit CBuffer.
    /// Layout is sequential to match GPU CBUFFER `_ComputeBlitParams`.
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public struct ComputeBlitParams : IComputeData<ComputeBlitParams>
    {
        /// <summary>
        /// Resolution of the destination render texture in pixels (width, height).
        /// Corresponds to `_BlitDest_Res` in the CBUFFER.
        /// </summary>
        public Vector2Int BlitDest_Res;

        /// <summary>
        /// Makes sure the source and destination textures properly align.
        /// </summary>
        public int BlitStretchToFit;

        /// <summary>
        /// Ensures 16 byte alignment for CBuffer usage.
        /// </summary>
        private int _padding;

        /// <summary>
        /// Texel size of source textures:
        /// xy = 1/width, 1/height for normalized coordinates.
        /// zw = width, height for pixel-based calculations.
        /// </summary>
        public Vector4 BlitTexture0_TexelSize;
        public Vector4 BlitTexture1_TexelSize;
        public Vector4 BlitTexture2_TexelSize;
        public Vector4 BlitTexture3_TexelSize;

        /// <summary>
        /// Returns the raw struct data for ComputeBuffer upload.
        /// </summary>
        public ComputeBlitParams RawData => this;
    }
}
