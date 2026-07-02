using Rayforge.Core.Execution.Abstractions;
using System;

namespace Rayforge.Core.Collections.Abstractions
{
    /// <summary>
    /// Provides read-only diagnostic and access capabilities for a metadata registry.
    /// Acts as the safe, immutable view for external systems.
    /// </summary>
    /// <typeparam name="TKey">The unique identifier type.</typeparam>
    public interface IReadOnlyMetadataProvider<TKey>
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

        #region Diagnostics & Access

        /// <summary> Gets the byte size of a single element for the specified type. </summary>
        /// <typeparam name="T">The unmanaged data type.</typeparam>
        int GetStride<T>() where T : unmanaged;

        /// <summary> Checks if the store for type T has pending dirty segments that require synchronization. </summary>
        /// <typeparam name="T">The unmanaged data type to check.</typeparam>
        bool IsDirty<T>() where T : unmanaged;

        /// <summary> Gets the raw store data as a read-only span for zero-allocation access. </summary>
        /// <typeparam name="T">The unmanaged data type.</typeparam>
        ReadOnlySpan<T> AsSpan<T>() where T : unmanaged;

        /// <summary> Tries to retrieve the current slot index for a given key. </summary>
        bool TryGetIndex(TKey key, out int index);

        #endregion

        #region High-Performance Iteration

        /// <summary> Performs a zero-allocation iteration over dirty segments of a store. </summary>
        /// <param name="action">The handler to execute for each dirty segment.</param>
        /// <param name="mergeContiguous">If true, adjacent segments are merged to reduce draw calls.</param>
        void ForEachDirtySegment<T, TAction>(ref TAction action, bool mergeContiguous = true)
            where T : unmanaged
            where TAction : struct, IExecutionHandler<BufferSegmentMeta<T>>;

        /// <summary> Performs a high-performance iteration over individual dirty indices. </summary>
        /// <param name="action">The handler to execute for each dirty index.</param>
        void ForEachDirtyIndex<T, TAction>(ref TAction action)
            where T : unmanaged
            where TAction : struct, IExecutionHandler<int>;

        #endregion
    }
}
