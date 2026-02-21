using Rayforge.Core.Rendering.Textures;
using UnityEngine;

namespace Rayforge.Core.Environment.Spatial.Rendering.Helpers
{
    public static class AtlasCoordinateExtensions
    {
        /// <summary>
        /// Converts normalized atlas mapping data into a pixel-perfect view for baking.
        /// </summary>
        /// <param name="mapping">The normalized source data.</param>
        /// <param name="atlasResolution">The total resolution of the target TextureArray.</param>
        /// <returns>A view containing absolute pixel coordinates.</returns>
        public static AtlasSlotView ToSlotView(this TextureMappingData mapping, int atlasResolution)
        {
            float size = atlasResolution * mapping.RelativeScale;

            return new AtlasSlotView
            {
                SliceIndex = mapping.SliceIndex,
                ViewportRect = mapping.ToViewportRect(atlasResolution)
            };
        }

        /// <summary>
        /// Calculates the pixel-perfect Rect for a specific atlas slot.
        /// Useful for GL.Viewport or Graphics.SetScissorRect calls.
        /// </summary>
        /// <param name="mapping">The normalized source data.</param>
        /// <param name="atlasResolution">The resolution of the target texture array.</param>
        /// <returns>A Rect defined in absolute pixel coordinates.</returns>
        public static Rect ToViewportRect(this TextureMappingData mapping, int atlasResolution)
        {
            float size = atlasResolution * mapping.RelativeScale;
            return new Rect(
                mapping.RelativeOffset.x * atlasResolution,
                mapping.RelativeOffset.y * atlasResolution,
                size,
                size
            );
        }
    }
}