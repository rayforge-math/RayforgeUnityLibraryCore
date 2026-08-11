using UnityEngine;

namespace Rayforge.Core.ManagedResources.Abstractions
{
    /// <summary>
    /// Specialized descriptor for texture-based resources.
    /// </summary>
    public interface ITextureDescriptor
    {
        /// <summary>
        /// The horizontal resolution in pixels.
        /// </summary>
        int Width { get; set; }

        /// <summary>
        /// The vertical resolution in pixels.
        /// </summary>
        int Height { get; set; }

        /// <summary>
        /// The graphics format of the texture (e.g., RGBA32, DXT5).
        /// </summary>
        TextureFormat Format { get; set; }
    }
}