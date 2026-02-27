namespace Rayforge.Core.Rendering.Abstractions
{
    /// <summary>
    /// Internal administrative interface for metadata stores.
    /// Extends the public <see cref="IMetadataStore"/> with management operations.
    /// This interface is intended to be used only by the Registry to 
    /// maintain lifecycle and configuration without exposing destructive methods to the public API.
    /// </summary>
    internal interface IMetadataStoreController : IMetadataStore
    {
        /// <summary>
        /// Re-allocates the underlying data storage.
        /// This is a destructive operation that clears all existing data 
        /// while preserving the current batching configuration.
        /// </summary>
        /// <param name="newCapacity">The new maximum number of elements.</param>
        void Resize(int newCapacity);

        /// <summary>
        /// Updates the granularity of dirty tracking.
        /// Reconfigures how elements are grouped for GPU uploads. 
        /// Existing data is preserved and dirty states are migrated to the new layout.
        /// </summary>
        /// <param name="newBatchSize">The new size of a single tracking segment.</param>
        void UpdateBatchSize(int newBatchSize);
    }
}