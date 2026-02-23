using UnityEngine;

namespace Rayforge.Core.Rendering.Helpers
{
    public static class DescriptorExtensions
    {
        /// <summary>
        /// Checks if two descriptors are compatible for the Atlas system.
        /// Compares only the properties that would require a GPU re-allocation.
        /// </summary>
        public static bool IsCompatible(this RenderTextureDescriptor current, RenderTextureDescriptor other)
        {
            return current.width == other.width &&
           current.height == other.height &&
           current.volumeDepth == other.volumeDepth &&
           current.graphicsFormat == other.graphicsFormat &&
           current.dimension == other.dimension &&
           current.msaaSamples == other.msaaSamples &&
           current.sRGB == other.sRGB &&
           current.useMipMap == other.useMipMap &&
           current.autoGenerateMips == other.autoGenerateMips &&
           current.mipCount == other.mipCount &&
           current.depthBufferBits == other.depthBufferBits;
        }
    }
}