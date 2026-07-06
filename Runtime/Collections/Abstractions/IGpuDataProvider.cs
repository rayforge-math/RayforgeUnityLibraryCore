using Rayforge.Core.Execution.Abstractions;
using System;
using UnityEngine;

namespace Rayforge.Core.Collections.Abstractions
{
    /// <summary>
    /// Extends the read-only metadata provider with full management and write capabilities.
    /// Reserved for systems responsible for registry orchestration.
    /// </summary>
    /// <typeparam name="TKey">The unique identifier type.</typeparam>
    public interface IGpuDataProvider<TKey> : IReadOnlyGpuDataProvider<TKey>
        where TKey : struct, IEquatable<TKey>
    {
        #region Data Access

        /// <summary>
        /// Gets the full, writable raw buffer interface for a specific store.
        /// Reserved for systems that need both read access and modification capabilities.
        /// </summary>
        /// <typeparam name="T">The unmanaged data type.</typeparam>
        /// <returns>The raw buffer interface, or null if no store is registered.</returns>
        /// <exception cref="InvalidOperationException">Thrown when no raw buffer is registered for the specified type T.</exception>
        IRawBuffer<T> GetRawBuffer<T>() where T : unmanaged;

        /// <summary> Updates or adds a value for a specific key. Allocates a slot if necessary. </summary>
        /// <returns>The absolute index where the value is stored.</returns>
        int Set<T>(TKey key, T value) where T : unmanaged;

        #endregion

        #region Management

        /// <summary> Resets the registry by clearing all key-to-slot mappings and resetting all data stores. </summary>
        void Clear();

        /// <summary> Acknowledges all processed data by resetting the dirty tracking state for all stores. </summary>
        void ClearDirtyState();

        /// <summary>
        /// Resets the dirty state for the store of the specified type.
        /// </summary>
        /// <typeparam name="T">The unmanaged data type.</typeparam>
        /// <exception cref="InvalidOperationException">Thrown if no store is registered for type T.</exception>
        void ClearDirty<T>() where T : unmanaged;

        /// <summary> Releases the key and its associated index; the slot becomes available for reuse. </summary>
        /// <returns>The index that was released, or -1 if the key was not found.</returns>
        int Release(TKey key);

        /// <summary> Retrieves an existing index or allocates a new slot for the key if it doesn't exist. </summary>
        int GetOrAllocateIndex(TKey key);

        /// <summary> Performs a structural reconfiguration, potentially resizing or resetting the registry state. </summary>
        /// <param name="capacity">The target total slot capacity.</param>
        /// <param name="batchSize">The target synchronization granularity.</param>
        /// <returns>True if a structural re-allocation occurred.</returns>
        bool Reconfigure(int capacity, int batchSize);

        /// <summary> Resizes all underlying stores and the mapper to a new capacity. </summary>
        bool Resize(int newCapacity);

        /// <summary> Updates the granularity for dirty-tracking segments. </summary>
        bool UpdateBatchSize(int newBatchSize);

        #endregion
    }
}