using Rayforge.Core.Collections.Abstractions;
using System;
using System.Runtime.InteropServices;
using UnityEngine;

namespace Rayforge.Core.Rendering.Abstractions
{
    /// <summary>
    /// The master data for a chunk's location in the atlas.
    /// Uses normalized coordinates (0-1) which are resolution-independent.
    /// </summary>
    [Serializable]
    [StructLayout(LayoutKind.Sequential)]
    public struct TextureMappingData : IGpuData<TextureMappingData>
    {
        /// <summary>
        /// The Z-index (layer) of the Texture2DArray where the data is stored.
        /// A value of -1 typically represents an unassigned or inactive slot.
        /// </summary>
        public int SliceIndex;

        /// <summary>
        /// The normalized scale of the slot relative to the full slice (e.g., 0.5 for a 2x2 grid slot).
        /// Used by shaders to multiply the incoming UV coordinates.
        /// </summary>
        public float RelativeScale;

        /// <summary>
        /// The normalized UV offset [0..1] within the slice where the slot starts.
        /// Used by shaders to shift the UV coordinates after scaling.
        /// </summary>
        public Vector2 RelativeOffset;

        /// <summary>
        /// Returns true if the SliceIndex is 0 or greater, indicating a valid assigned slot.
        /// </summary>
        public bool IsValid => SliceIndex >= 0;

        /// <inheritdoc />
        public TextureMappingData InvalidData()
        {
            return new TextureMappingData
            {
                RelativeOffset = Vector2.zero,
                RelativeScale = 1f,
                SliceIndex = -1
            };
        }
    }
}