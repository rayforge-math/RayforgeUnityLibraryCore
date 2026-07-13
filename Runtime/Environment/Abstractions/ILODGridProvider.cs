using Rayforge.Core.Collections.Abstractions;

namespace Rayforge.Core.Environment.Abstractions
{
    using Rayforge.Core.Execution.Abstractions;
    using System;
    using UnityEngine;

    /// <summary>
    /// Provides Level of Detail (LOD) information and spatial iteration for grid-based systems.
    /// Handles distance-based LOD calculations and provides optimized iteration over cell keys.
    /// </summary>
    /// <typeparam name="TKey">The unique identifier type for grid cells (must be an equatable struct).</typeparam>
    public interface ILODGridProvider<TKey> : ISpatialGridProvider<TKey>
        where TKey : struct, IEquatable<TKey>
    {
        #region Configuration & State

        /// <summary> Gets the current world-space position of the player or camera focus. </summary>
        Vector3 ViewerPos { get; }

        /// <summary> Gets the total number of defined LOD levels. </summary>
        int LodCount { get; }

        /// <summary> Gets the number of cells currently active (within visible range). </summary>
        int ActiveCellCount { get; }

        /// <summary> Gets the squared distance thresholds for each LOD level. </summary>
        ReadOnlySpan<float> LodSqrDistances { get; }

        /// <summary> Gets the linear distance thresholds for each LOD level. </summary>
        ReadOnlySpan<float> LodDistances { get; }

        /// <summary> 
        /// Occurs when distance thresholds change while the LOD count remains constant.
        /// Use this to trigger a re-evaluation of existing cell LODs without rebuilding the grid.
        /// </summary>
        event Action<ILODGridProvider<TKey>> OnLODSettingsChanged;

        #endregion

        #region High-Performance Iteration (Zero-Allocation)

        /// <summary>
        /// Executes a specialized action for every grid key that falls exactly into the specified LOD level.
        /// This method is optimized for zero-allocation and allows the JIT to inline the iteration logic.
        /// </summary>
        /// <typeparam name="TAction">A struct implementing IIterationAction for the keys.</typeparam>
        /// <param name="lodIndex">The target LOD level index (0 to LodCount-1).</param>
        /// <param name="center">The world-space center for the LOD evaluation.</param>
        /// <param name="action">The action to execute for each found key. Passed by reference to avoid copying.</param>
        void ForEachKeyInLOD<TAction>(int lodIndex, Vector3 center, ref TAction action)
            where TAction : struct, IExecutionHandler<TKey>;

        /// <summary>
        /// Executes a specialized action for all grid keys within the maximum visible range (all LODs combined).
        /// </summary>
        /// <typeparam name="TAction">A struct implementing IIterationAction for the keys.</typeparam>
        /// <param name="center">The world-space center for the range evaluation.</param>
        /// <param name="action">The action to execute for each found key. Passed by reference to avoid copying.</param>
        void ForEachKeyInRange<TAction>(Vector3 center, ref TAction action)
            where TAction : struct, IExecutionHandler<TKey>;

        #endregion

        #region Flexible Iteration (Boxing)

        /// <summary>
        /// Returns an iterator for all grid keys in a specific LOD level.
        /// </summary>
        /// <param name="lodIndex">The target LOD level index (0 to LodCount-1).</param>
        /// <param name="center">The world-space center for the LOD evaluation.</param>
        /// <returns>A boxed IIterator instance. Note: Use ForEachKeyInLOD to avoid heap allocation.</returns>
        IIterator<TKey> GetKeysInLODLevel(int lodIndex, Vector3 center);

        /// <summary>
        /// Returns an iterator for all grid keys within the full visible range.
        /// </summary>
        /// <param name="center">The world-space center for the range evaluation.</param>
        /// <returns>A boxed IIterator instance containing all keys within the maximum distance.</returns>
        IIterator<TKey> GetKeysInFullRange(Vector3 center);

        #endregion

        #region Capacity & Metrics

        /// <summary>
        /// Gets the exact number of grid cells currently falling into a specific LOD level.
        /// </summary>
        /// <param name="lodIndex">The target LOD level index.</param>
        /// <param name="center">The world-space center for the evaluation.</param>
        /// <returns>The number of keys currently contained in this LOD ring.</returns>
        int GetKeyCountInLODLevel(int lodIndex, Vector3 center);

        /// <summary>
        /// Gets the total number of grid cells within the maximum visible range.
        /// </summary>
        /// <param name="center">The world-space center for the evaluation.</param>
        /// <returns>The total number of active keys in all LOD levels combined.</returns>
        int GetKeyCountInFullRange(Vector3 center);

        /// <summary>
        /// Calculates the maximum theoretical number of cells any LOD level could cover, regardless of viewer position.
        /// Essential for safe pre-allocation of fixed-size GPU buffers or native arrays.
        /// </summary>
        /// <param name="lodIndex">The target LOD level index.</param>
        /// <returns>The maximum possible cell count for the given LOD level.</returns>
        int GetMaxCapacityForLODLevel(int lodIndex);

        #endregion
    }
}