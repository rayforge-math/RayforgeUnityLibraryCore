using System;
using UnityEngine;

namespace Rayforge.Core.Environment.Abstractions
{
    /// <summary>
    /// Handles coordinate transformations and spatial distance calculations for grid cells.
    /// </summary>
    /// <typeparam name="TKey">The unique identifier type for grid cells.</typeparam>
    public interface ISpatialCoordinateMapper<TKey>
        where TKey : struct, IEquatable<TKey>
    {
        /// <summary> Maps a world-space position to its corresponding discrete grid coordinate. </summary>
        TKey WorldToGrid(Vector3 worldPos);

        /// <summary> Returns the world-space center position of a specific grid cell. </summary>
        Vector3 GetCellCenter(TKey key);

        /// <summary> Snaps a world position to the center of the cell it resides in. </summary>
        Vector3 GetCellCenter(Vector3 worldPos);

        /// <summary> Returns the world-space Axis-Aligned Bounding Box (AABB) of a specific grid cell. </summary>
        Bounds GetCellBounds(TKey key);

        /// <summary> Returns the world-space AABB of the cell that contains the given world position. </summary>
        Bounds GetCellBounds(Vector3 worldPos);

        /// <summary> Calculates the squared distance from a world position to the closest point or edge of a grid cell. </summary>
        float GetSqrDistanceToClosestEdge(TKey key, Vector3 worldPos);

        /// <summary> Calculates the squared distance from a world position to the center of a specific grid cell. </summary>
        float GetSqrDistanceToCenter(TKey key, Vector3 worldPos);
    }
}
