using Rayforge.Core.Collections.Abstractions;
using Rayforge.Core.Execution.Abstractions;
using System;
using UnityEngine;

namespace Rayforge.Core.Environment.Abstractions
{
    /// <summary>
    /// Provides high-performance, zero-allocation iteration queries for grid-based LOD levels.
    /// </summary>
    /// <typeparam name="TKey">The unique identifier type for grid cells (must be an equatable struct).</typeparam>
    public interface ILODGridQuery<TKey> : ISpatialGridQuery<TKey>
        where TKey : struct, IEquatable<TKey>
    {
        #region Target LOD Evaluation

        /// <summary>
        /// Calculates the target LOD index for a given squared distance.
        /// </summary>
        /// <param name="sqrDistance">The squared distance to evaluate against threshold values.</param>
        /// <returns>The zero-based LOD index, or -1 if the distance exceeds all thresholds (culled).</returns>
        int CalculateTargetLODSqr(float sqrDistance);

        /// <summary>
        /// Calculates the target LOD index for a given linear distance.
        /// </summary>
        /// <param name="distance">The linear distance to evaluate against threshold values.</param>
        /// <returns>The zero-based LOD index, or -1 if the distance exceeds all thresholds (culled).</returns>
        int CalculateTargetLOD(float distance);

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
        void ForEachKeyInFullRange<TAction>(Vector3 center, ref TAction action)
            where TAction : struct, IExecutionHandler<TKey>;

        #endregion

        #region Flexible Iteration (Boxing)

        /// <summary>
        /// Returns an iterator for all grid keys in a specific LOD level.
        /// </summary>
        /// <param name="lodIndex">The target LOD level index (0 to LodCount-1).</param>
        /// <param name="center">The world-space center for the LOD evaluation.</param>
        /// <returns>A boxed IIterator instance. Note: Use ForEachKeyInLOD to avoid heap allocation.</returns>
        IIterator<TKey> GetKeysInLOD(int lodIndex, Vector3 center);

        /// <summary>
        /// Returns an iterator for all grid keys within the full visible range.
        /// </summary>
        /// <param name="center">The world-space center for the range evaluation.</param>
        /// <returns>A boxed IIterator instance containing all keys within the maximum distance.</returns>
        IIterator<TKey> GetKeysInFullRange(Vector3 center);

        #endregion
    }
}
