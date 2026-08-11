using UnityEngine;
using UnityEngine.Rendering;

namespace Rayforge.Core.Rendering.Helpers
{
    public static class DescriptorFormatter
    {
        /// <summary>
        /// Configures an existing descriptor to work as a Texture2DArray.
        /// Overwrites dimension and volumeDepth.
        /// </summary>
        /// <param name="desc">The base descriptor to modify.</param>
        /// <param name="sliceCount">The number of slices required.</param>
        /// <returns>The modified descriptor configured as a Tex2DArray.</returns>
        public static RenderTextureDescriptor ToTextureArray(this RenderTextureDescriptor desc, int sliceCount)
        {
            desc.volumeDepth = Mathf.Max(1, sliceCount);
            desc.dimension = TextureDimension.Tex2DArray;

            return desc;
        }

        /// <summary>
        /// Configures a descriptor for an Atlas system.
        /// Disables MSAA to prevent bleeding artifacts and optionally sets resolution.
        /// </summary>
        /// <param name="desc">The base descriptor to modify.</param>
        /// <param name="sliceCount">The number of slices required.</param>
        /// <param name="resolution">Optional: The pixel resolution. If 0 (default), existing width/height are kept.</param>
        /// <returns>The modified descriptor optimized for atlas usage.</returns>
        public static RenderTextureDescriptor ToAtlasArray(
            this RenderTextureDescriptor desc,
            int sliceCount,
            int resolution = 0)
        {
            if (resolution > 0)
            {
                desc.width = resolution;
                desc.height = resolution;
            }

            desc.msaaSamples = 1;

            return desc.ToTextureArray(sliceCount);
        }
    }
}