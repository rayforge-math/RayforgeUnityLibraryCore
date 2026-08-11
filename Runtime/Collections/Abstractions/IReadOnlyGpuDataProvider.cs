using Rayforge.Core.Execution.Abstractions;
using System;
using UnityEngine;

namespace Rayforge.Core.Collections.Abstractions
{
    /// <summary>
    /// Provides read-only diagnostic and access capabilities for a metadata registry.
    /// Acts as the safe, immutable view for external systems.
    /// </summary>
    /// <typeparam name="TKey">The unique identifier type.</typeparam>
    public interface IReadOnlyGpuDataProvider<TKey>
        where TKey : struct, IEquatable<TKey>
    {
        #region Properties

        /// <summary> Gets the maximum number of slots currently supported by the stores. </summary>
        int Capacity { get; }

        /// <summary> Gets the granularity of dirty-tracking segments. </summary>
        int BatchSize { get; }

        /// <summary> Gets the number of currently active keys in the registry. </summary>
        int Count { get; }

        /// <summary> Gets the highest index currently allocated, used for optimizing GPU compute dispatches. </summary>
        int HighestIndex { get; }

        #endregion

        #region Access

        /// <summary>
        /// Attempts to retrieve a value for the given key.
        /// </summary>
        /// <returns>True if the key exists and the store is registered, false otherwise.</returns>
        public bool TryGet<T>(TKey key, out T value) where T : unmanaged;

        /// <summary>
        /// Gets the value for the given key. Returns default if not found.
        /// </summary>
        public T Get<T>(TKey key) where T : unmanaged;
        
        /// <summary> Tries to retrieve the current slot index for a given key. </summary>
                bool TryGetIndex(TKey key, out int index);

        /// <summary>
        /// Provides a read-only buffer view for a specific type.
        /// Useful for external systems that require span-based access 
        /// without allowing modification of the underlying store.
        /// </summary>
        /// <typeparam name="T">The unmanaged data type.</typeparam>
        /// <returns>A read-only buffer view, or null if no store is registered.</returns>
        IReadOnlyRawBuffer<T> GetReadOnlyBuffer<T>() where T : unmanaged;

        /// <summary>
        /// Uploads the data from the registered store into the provided <see cref="ComputeBuffer"/>.
        /// <para>
        /// This method synchronizes the CPU-side metadata with the GPU buffer.
        /// It typically uses the buffer's stride and capacity to perform a data transfer.
        /// </para>
        /// </summary>
        /// <param name="target">The target GPU buffer to receive the data.</param>
        /// <typeparam name="T">The unmanaged data type.</typeparam>
        void Upload<T>(ComputeBuffer target) where T : unmanaged;

        /// <summary>
        /// Uploads a partial segment from the registered store into the provided <see cref="ComputeBuffer"/>.
        /// </summary>
        /// <typeparam name="T">The unmanaged data type.</typeparam>
        /// <param name="target">The target GPU buffer.</param>
        /// <param name="srcOffset">The start index in the store's buffer.</param>
        /// <param name="destOffset">The start index in the GPU ComputeBuffer.</param>
        /// <param name="count">The number of elements to copy.</param>
        void Upload<T>(ComputeBuffer target, int srcOffset, int destOffset, int count) where T : unmanaged;
        
        #endregion

        #region Management

        /// <summary> Checks if the store for type T has pending dirty segments that require synchronization. </summary>
        /// <typeparam name="T">The unmanaged data type to check.</typeparam>
        bool IsDirty<T>() where T : unmanaged;

        /// <summary> 
        /// Returns true if any of the registered stores have pending dirty segments 
        /// that require synchronization. 
        /// </summary>
        bool AnyDirty { get; }

        /// <summary>
        /// Retrieves read-only metadata for a specific store.
        /// Useful for diagnostics, UI rendering, or synchronization monitoring.
        /// </summary>
        IBufferMetadata GetBufferMetadata<T>() where T : unmanaged;

        #endregion

        #region Iteration

        /// <summary>
        /// Executes a specialized action for every dirty segment in the store.
        /// Uses ref TAction to ensure zero-allocation processing on the stack.
        /// </summary>
        void ForEachDirtySegment<T, TAction>(ref TAction action, bool mergeContiguous = true)
            where T : unmanaged
            where TAction : struct, IExecutionHandler<BufferSegmentMeta<T>>;

        /// <summary>
        /// Executes a specialized action for every dirty batch index.
        /// </summary>
        void ForEachDirtyIndex<T, TAction>(ref TAction action)
            where T : unmanaged
            where TAction : struct, IExecutionHandler<int>;

        /// <summary>
        /// Returns an iterator over dirty segments.
        /// CAUTION: Causes boxing of the internal iterator struct.
        /// </summary>
        IIterator<BufferSegmentMeta<T>> GetDirtySegmentIterator<T>(bool mergeContiguous = true)
            where T : unmanaged;

        /// <summary>
        /// Returns an iterator over indices of dirty segments.
        /// CAUTION: Causes boxing of the internal iterator struct.
        /// </summary>
        IIterator<int> GetDirtySegmentIndices<T>()
            where T : unmanaged;

        #endregion
    }
}
