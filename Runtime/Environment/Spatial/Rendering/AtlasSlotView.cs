using System;
using UnityEngine;

namespace Rayforge.Core.Environment.Spatial.Rendering
{
    /// <summary>
    /// A helper view that provides all necessary information to render directly into an atlas slot.
    /// </summary>
    [Serializable]
    public struct AtlasSlotView
    {
        /// <summary>
        /// The Z-index (layer) of the TextureArray.
        /// </summary>
        public int SliceIndex;

        /// <summary>
        /// The pixel-perfect region within the slice. 
        /// Defines both the offset (x,y) and the size (width, height).
        /// </summary>
        public Rect ViewportRect;
    }
}