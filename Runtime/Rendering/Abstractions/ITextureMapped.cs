using Rayforge.Core.Rendering.Textures;

namespace Rayforge.Core.Rendering.Abstractions
{
    /// <summary>
    /// Defines a contract for objects that can hold and update atlas mapping metadata.
    /// English: Focuses strictly on the storage and modification of TextureMappingData.
    /// </summary>
    public interface ITextureMapped
    {
        /// <summary>
        /// Gets or sets the atlas mapping metadata.
        /// English: Provides direct access to slice, scale, and offset information for GPU rendering.
        /// </summary>
        TextureMappingData Mapping { get; }

        /// <summary>
        /// Indicates whether the current mapping is valid and assigned.
        /// English: Typically checks if SliceIndex is non-negative.
        /// </summary>
        bool HasMapping { get; }

        /// <summary>
        /// Updates the view coordinates for this chunk.
        /// </summary>
        /// <param name="data">The mapping data provided by the AtlasController.</param>
        public void SetTextureMapping(TextureMappingData data);

        /// <summary>
        /// Resets the mapping to its default, unassigned state.
        /// English: Clears the metadata to prevent rendering with obsolete atlas coordinates.
        /// </summary>
        void ClearMapping();
    }
}