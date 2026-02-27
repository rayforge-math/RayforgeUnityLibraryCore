using Rayforge.Core.Collections.Abstractions;
using Rayforge.Core.Rendering.Collections.Iterator;
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
        /// Provides a direct sync iterator for a specific metadata type.
        /// Use this to target a specific ComputeBuffer for a specific data stream.
        /// </summary>
        /// <typeparam name="T">The metadata struct type.</typeparam>
        public IIterator<BufferSegmentMeta> GetDirtyBatchIterator<T>() where T : unmanaged;

        /// <summary>
        /// Provides a direct, element-wise iterator for a specific metadata type.
        /// Use this for CPU-side logic that requires reading all stored data sequentially,
        /// such as serialization, validation, or global data analysis.
        /// </summary>
        /// <typeparam name="T">The unmanaged metadata struct type (e.g., SpatialData).</typeparam>
        /// <returns>
        /// An <see cref="IIterator{T}"/> over the underlying CPU array. 
        /// Returns an empty iterator if no store is registered for the specified type.
        /// </returns>
        /// <remarks>
        /// Unlike the Batch-Iterator, this does not group changes and ignores the dirty state. 
        /// It performs a full sweep over the allocated capacity.
        /// </remarks>
        public IIterator<T> GetIterator<T>() where T : unmanaged;
    }
}