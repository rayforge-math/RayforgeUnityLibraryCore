namespace Rayforge.Core.Rendering.Abstractions
{
    /// <summary>
    /// Defines a contract for objects that can hold and update atlas mapping metadata.
    /// English: Focuses strictly on the storage and modification of TextureMappingData.
    /// </summary>
    public interface ITextureMapped
    {
        /// <summary>
        /// Gets the atlas mapping metadata.
        /// </summary>
        TextureMappingData Mapping { get; }

        /// <summary>
        /// Indicates whether the current mapping is valid and assigned.
        /// </summary>
        bool HasMapping { get; }
    }
}