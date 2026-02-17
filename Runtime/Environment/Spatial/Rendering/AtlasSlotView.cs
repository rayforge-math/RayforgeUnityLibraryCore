using UnityEngine;

namespace Rayforge.Core.Environment.Spatial.Rendering
{
    /// <summary>
    /// A helper view that provides all necessary information to render directly into an atlas slot.
    /// </summary>
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

        public AtlasSlotView(AtlasMappingData mapping, int atlasTotalResolution)
        {
            SliceIndex = (int)mapping.SliceIndex;

            float size = atlasTotalResolution * mapping.RelativeScale;
            float x = mapping.RelativeOffset.x * atlasTotalResolution;
            float y = mapping.RelativeOffset.y * atlasTotalResolution;

            ViewportRect = new Rect(x, y, size, size);
        }
    }
}