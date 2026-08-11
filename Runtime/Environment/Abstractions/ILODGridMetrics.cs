using System;
using UnityEngine;

namespace Rayforge.Core.Environment.Abstractions
{
    /// <summary>
    /// Provides capacity metrics and element counts for LOD grid levels.
    /// </summary>
    /// <typeparam name="TKey">The unique identifier type for grid cells (must be an equatable struct).</typeparam>
    public interface ILODGridMetrics<TKey>
        where TKey : struct, IEquatable<TKey>
    {
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
    }
}
