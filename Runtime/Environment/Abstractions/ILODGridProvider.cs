using System;
using System.Collections.Generic;
using UnityEngine;

namespace Rayforge.Core.Environment.Abstractions
{
    public interface ILODGridProvider<TKey> : ISpatialGridProvider<TKey>
         where TKey : struct, IEquatable<TKey>
    {
        /// <summary> The current position of the player/camera focus. </summary>
        Vector3 ViewerPos { get; }

        /// <summary> The current LOD count. </summary>
        int LodCount { get; }

        /// <summary> The squared distance thresholds for each LOD level. </summary>
        ReadOnlySpan<float> LodSqrDistances { get; }

        /// <summary> The distance thresholds for each LOD level. </summary>
        ReadOnlySpan<float> LodDistances { get; }

        /// <summary> 
        /// Triggered when distance values change, but the count remains the same. 
        /// Requires only a re-evaluation of current chunk LODs.
        /// </summary>
        event Action<ILODGridProvider<TKey>> OnLODSettingsChanged;

        /// <summary> 
        /// Maps a squared distance to the corresponding LOD index. 
        /// Returns -1 if the distance exceeds all LOD ranges.
        /// </summary>
        int CalculateTargetLOD(float sqrDistance);

        /// <summary>
        /// Returns all grid keys that fall exactly into a specific LOD level index.
        /// Useful for atlas allocation and initial batch loading.
        /// </summary>
        /// <param name="lodIndex">The index of the LOD (0 to LodCount-1).</param>
        /// <param name="center">The world-space center of the LOD circles.</param>
        IEnumerable<TKey> GetKeysInLODLevel(int lodIndex, Vector3 center);

        /// <summary>
        /// Returns all grid keys that are within the maximum visible range (last LOD).
        /// </summary>
        IEnumerable<TKey> GetKeysInFullRange(Vector3 center);

        /// <summary>
        /// Returns the exact number of grid cells that fall into a specific LOD level.
        /// Ideal for pre-allocating memory in an Atlas.
        /// </summary>
        int GetKeyCountInLODLevel(int lodIndex, Vector3 center);

        /// <summary>
        /// Returns the total number of grid cells within the maximum visible range.
        /// </summary>
        int GetKeyCountInFullRange(Vector3 center);

        /// <summary>
        /// Calculates the maximum possible number of chunks any LOD level could ever cover,
        /// regardless of the viewer's position. Essential for safe pre-allocation.
        /// </summary>
        int GetMaxCapacityForLODLevel(int lodIndex);
    }
}