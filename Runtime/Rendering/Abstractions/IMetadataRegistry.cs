using System;

namespace Rayforge.Core.Rendering.Collections.Buffered
{
    /// <summary>
    /// Provides read-only access to a centralized metadata registry.
    /// This interface allows external systems (like renderers or UI) 
    /// to query registry state and dispatch GPU updates without being able to 
    /// modify the underlying mapping or data stores.
    /// </summary>
    public interface IMetadataRegistry
    {
        /// <summary>
        /// Gets the total capacity allocated for all metadata stores.
        /// Represents the maximum number of unique keys that can be registered.
        /// </summary>
        int Capacity { get; }

        /// <summary>
        /// Gets the size of the blocks used for dirty-tracking and GPU uploads.
        /// Metadata is synchronized in chunks of this size to optimize bus bandwidth.
        /// </summary>
        int BatchSize { get; }

        /// <summary>
        /// Gets the current number of active keys tracked by the registry.
        /// </summary>
        int Count { get; }

        /// <summary>
        /// Gets the highest slot index currently in use.
        /// This is vital for optimizing GPU compute dispatches 
        /// (e.g., dispatching only enough thread groups to cover active data).
        /// </summary>
        int HighestIndex { get; }

        /// <summary>
        /// Triggers a synchronization of all modified data across all registered stores.
        /// While this triggers an action, it is considered "read-only access" 
        /// to the data flow, as it only facilitates the transfer to the GPU.
        /// </summary>
        /// <param name="uploadAction">
        /// Callback invoked for each dirty block: (Array source, int start, int count, Type storeType).
        /// </param>
        void SyncAllStores(Action<Array, int, int, Type> uploadAction);
    }
}