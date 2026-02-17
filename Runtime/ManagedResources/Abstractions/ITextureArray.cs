using UnityEngine;

namespace Rayforge.Core.ManagedResources.Abstractions
{
    /// <summary>
    /// A specialized interface for dynamic texture collections.
    /// It simplifies the API by fixing TIn to <see cref="Texture"/> and TOut to <see cref="RenderTexture"/>.
    /// </summary>
    public interface ITextureArray : IArray<Texture, RenderTexture>
    {
        /// <summary>
        /// Gets the horizontal resolution of the texture resource in pixels.
        /// </summary>
        int Width { get; }

        /// <summary>
        /// Gets the vertical resolution of the texture resource in pixels.
        /// </summary>
        int Height { get; }

        /// <summary>
        /// Specific helper for texture updates, matching the Texture2DArray logic.
        /// </summary>
        void SetSlice(int index, Texture source);

        /// <summary>
        /// Specific helper for extracting a slice into a render target.
        /// </summary>
        void CopySliceToRenderTexture(int index, RenderTexture destination);
    }
}