using Rayforge.Core.Collections.Abstractions;
using Rayforge.Core.Environment.Spatial.Chunks;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Rayforge.Core.Environment.Abstractions
{
    public interface ISpatialGridProvider<TKey>
         where TKey : struct, IEquatable<TKey>
    {
        /// <summary> The size of one grid cell. </summary>
        GridSize GridSize { get; }

        /// <summary> The world-space origin of the grid. </summary>
        Vector3 Anchor { get; }

        /// <summary>
        /// Triggered when the grid's scale or fundamental structure changes.
        /// Passes the provider itself as the sender.
        /// </summary>
        event Action<ISpatialGridProvider<TKey>> OnGridStructureChanged;

        /// <summary> 
        /// Triggered when the grid origin shifts. 
        /// Passes the provider and the delta movement.
        /// </summary>
        event Action<ISpatialGridProvider<TKey>, Vector3> OnAnchorChanged;

        /// <summary>
        /// Maps a world position to its corresponding grid coordinate (key).
        /// </summary>
        /// <param name="worldPos">The position in world space.</param>
        /// <returns>The discrete grid coordinate.</returns>
        TKey WorldToGrid(Vector3 worldPos);

        /// <summary> 
        /// Returns all grid coordinates (keys) that are touched by the given world bounds. 
        /// </summary>
        IIterator<TKey> GetKeysInBounds(Bounds worldBounds);

        /// <summary> 
        /// Returns all grid coordinates (keys) that are touched by the given local bounds. 
        /// </summary>
        public IIterator<TKey> GetKeysInRelativeBounds(Bounds relativeBounds);

        /// <summary>
        /// Returns all grid keys within a certain world-space radius.
        /// </summary>
        /// <param name="useEdgeDistance">If true, uses distance to the closest edge (AABB). If false, uses cell center.</param>
        IIterator<TKey> GetKeysInRadius(Vector3 worldCenter, float radius, bool useEdgeDistance = true);

        /// <summary>
        /// Returns all grid keys within a certain radius relative to the Anchor.
        /// </summary>
        /// <param name="useEdgeDistance">If true, uses distance to the closest edge (AABB). If false, uses cell center.</param>
        IIterator<TKey> GetKeysInRelativeRadius(Vector3 relativeCenter, float radius, bool useEdgeDistance = true);

        /// <summary> 
        /// Calculates the squared distance from a world position to the closest point/edge of a grid cell.
        /// This is what your ChunkManager uses.
        /// </summary>
        float GetSqrDistanceToClosestEdge(TKey key, Vector3 worldPos);

        /// <summary> Calculate square distance to centre.</summary>
        float GetSqrDistanceToCenter(TKey key, Vector3 worldPos);

        /// <summary>
        /// Returns the center position of a specific grid cell.
        /// </summary>
        Vector3 GetCellCenter(TKey key);

        /// <summary>
        /// Returns the world-space center of the cell that contains the given world position.
        /// </summary>
        Vector3 GetCellCenter(Vector3 worldPos);

        /// <summary>
        /// Returns the world-space AABB of a specific grid cell.
        /// </summary>
        Bounds GetCellBounds(TKey key);

        /// <summary>
        /// Returns the world-space AABB of a specific grid cell.
        /// </summary>
        Bounds GetCellBounds(Vector3 worldPos);
    }
}
