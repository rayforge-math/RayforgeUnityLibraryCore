using Rayforge.Core.Collections.Abstractions;
using Rayforge.Core.Environment.Spatial.Chunks;
using Rayforge.Core.Execution.Abstractions;
using System;
using UnityEngine;

namespace Rayforge.Core.Environment.Abstractions
{
    /// <summary>
    /// Provides the fundamental spatial indexing logic for a grid-based system.
    /// Handles coordinate transformations and provides optimized spatial queries (Bounds, Radius).
    /// </summary>
    /// <typeparam name="TKey">The unique identifier type for grid cells (must be an equatable struct).</typeparam>
    public interface ISpatialGridProvider<TKey>
        where TKey : struct, IEquatable<TKey>
    {
        #region Configuration & State

        /// <summary> Gets the size of a single grid cell (usually width, height, depth). </summary>
        GridSize GridSize { get; }

        /// <summary> Gets the world-space origin (0,0,0) of the grid system. </summary>
        Vector3 Anchor { get; }

        /// <summary> Checks whether the underlying spatial system is ready for queries. </summary>
        bool IsInitialized { get; }

        /// <summary> Gets the total number of cells currently tracked or present in the spatial index. </summary>
        int TotalCellCount { get; }

        /// <summary>
        /// Occurs when the grid's scale or fundamental structure changes.
        /// </summary>
        event Action<ISpatialGridProvider<TKey>> OnGridStructureChanged;

        /// <summary> 
        /// Occurs when the grid origin (Anchor) shifts.
        /// </summary>
        event Action<ISpatialGridProvider<TKey>, Vector3> OnAnchorChanged;

        #endregion

        #region Coordinate Mapping

        /// <summary>
        /// Maps a world-space position to its corresponding discrete grid coordinate.
        /// </summary>
        /// <param name="worldPos">The position in world space.</param>
        /// <returns>The discrete grid key for the containing cell.</returns>
        TKey WorldToGrid(Vector3 worldPos);

        /// <summary>
        /// Returns the world-space center position of a specific grid cell.
        /// </summary>
        /// <param name="key">The grid coordinate key.</param>
        /// <returns>The center point of the cell in world space.</returns>
        Vector3 GetCellCenter(TKey key);

        /// <summary>
        /// Snaps a world position to the center of the cell it resides in.
        /// </summary>
        /// <param name="worldPos">The arbitrary world position.</param>
        /// <returns>The center point of the containing cell.</returns>
        Vector3 GetCellCenter(Vector3 worldPos);

        /// <summary>
        /// Returns the world-space Axis-Aligned Bounding Box (AABB) of a specific grid cell.
        /// </summary>
        /// <param name="key">The grid coordinate key.</param>
        /// <returns>The bounds of the cell in world space.</returns>
        Bounds GetCellBounds(TKey key);

        /// <summary>
        /// Returns the world-space AABB of the cell that contains the given world position.
        /// </summary>
        /// <param name="worldPos">The arbitrary world position.</param>
        /// <returns>The bounds of the containing cell.</returns>
        Bounds GetCellBounds(Vector3 worldPos);

        #endregion

        #region Distance Calculations

        /// <summary> 
        /// Calculates the squared distance from a world position to the closest point or edge of a grid cell.
        /// Highly optimized for LOD evaluation.
        /// </summary>
        /// <param name="key">The grid cell key to check.</param>
        /// <param name="worldPos">The position to measure from.</param>
        /// <returns>The minimum squared distance to the cell's volume.</returns>
        float GetSqrDistanceToClosestEdge(TKey key, Vector3 worldPos);

        /// <summary> 
        /// Calculates the squared distance from a world position to the center of a specific grid cell.
        /// </summary>
        /// <param name="key">The grid cell key to check.</param>
        /// <param name="worldPos">The position to measure from.</param>
        /// <returns>The squared distance to the cell's center.</returns>
        float GetSqrDistanceToCenter(TKey key, Vector3 worldPos);

        #endregion

        #region High-Performance Iteration (Zero-Allocation)

        /// <summary>
        /// Executes a specialized action for all grid keys that intersect the given world-space bounds.
        /// </summary>
        /// <typeparam name="TAction">A struct implementing IIterationAction for the keys.</typeparam>
        /// <param name="worldBounds">The AABB in world space to query.</param>
        /// <param name="action">The action to execute for each found key. Passed by reference.</param>
        void ForEachKeyInBounds<TAction>(Bounds worldBounds, ref TAction action)
            where TAction : struct, IExecutionHandler<TKey>;

        /// <summary>
        /// Executes a specialized action for all grid keys that intersect the given relative bounds (relative to Anchor).
        /// </summary>
        /// <typeparam name="TAction">A struct implementing IIterationAction for the keys.</typeparam>
        /// <param name="relativeBounds">The AABB relative to the grid anchor.</param>
        /// <param name="action">The action to execute for each found key. Passed by reference.</param>
        void ForEachKeyInRelativeBounds<TAction>(Bounds relativeBounds, ref TAction action)
            where TAction : struct, IExecutionHandler<TKey>;

        /// <summary>
        /// Executes a specialized action for all grid keys within a world-space radius.
        /// </summary>
        /// <typeparam name="TAction">A struct implementing IIterationAction for the keys.</typeparam>
        /// <param name="worldCenter">The center of the query sphere in world space.</param>
        /// <param name="radius">The radius of the query sphere.</param>
        /// <param name="action">The action to execute for each found key. Passed by reference.</param>
        /// <param name="useEdgeDistance">If true, checks distance to the cell's edge; if false, uses the cell center.</param>
        void ForEachKeyInRadius<TAction>(Vector3 worldCenter, float radius, ref TAction action, bool useEdgeDistance = true)
            where TAction : struct, IExecutionHandler<TKey>;

        /// <summary>
        /// Executes a specialized action for all grid keys within a radius relative to the Anchor.
        /// </summary>
        /// <typeparam name="TAction">A struct implementing IIterationAction for the keys.</typeparam>
        /// <param name="relativeCenter">The center of the query sphere relative to the grid anchor.</param>
        /// <param name="radius">The radius of the query sphere.</param>
        /// <param name="action">The action to execute for each found key. Passed by reference.</param>
        /// <param name="useEdgeDistance">If true, checks distance to the cell's edge; if false, uses the cell center.</param>
        void ForEachKeyInRelativeRadius<TAction>(Vector3 relativeCenter, float radius, ref TAction action, bool useEdgeDistance = true)
            where TAction : struct, IExecutionHandler<TKey>;

        #endregion

        #region Flexible Iteration (Boxing)

        /// <summary> 
        /// Returns an iterator for all grid keys touched by world-space bounds. 
        /// </summary>
        /// <param name="worldBounds">The AABB in world space.</param>
        /// <returns>A boxed IIterator instance. Use ForEachKeyInBounds for hot paths.</returns>
        IIterator<TKey> GetKeysInBounds(Bounds worldBounds);

        /// <summary> 
        /// Returns an iterator for all grid keys touched by bounds relative to the Anchor. 
        /// </summary>
        /// <param name="relativeBounds">The AABB relative to the grid anchor.</param>
        /// <returns>A boxed IIterator instance.</returns>
        IIterator<TKey> GetKeysInRelativeBounds(Bounds relativeBounds);

        /// <summary>
        /// Returns an iterator for all grid keys within a world-space radius.
        /// </summary>
        /// <param name="worldCenter">The center of the query sphere in world space.</param>
        /// <param name="radius">The radius of the sphere.</param>
        /// <param name="useEdgeDistance">If true, uses distance to the closest edge; if false, uses cell center.</param>
        /// <returns>A boxed IIterator instance.</returns>
        IIterator<TKey> GetKeysInRadius(Vector3 worldCenter, float radius, bool useEdgeDistance = true);

        /// <summary>
        /// Returns an iterator for all grid keys within a radius relative to the Anchor.
        /// </summary>
        /// <param name="relativeCenter">The center relative to the grid anchor.</param>
        /// <param name="radius">The radius of the sphere.</param>
        /// <param name="useEdgeDistance">If true, uses distance to the closest edge; if false, uses cell center.</param>
        /// <returns>A boxed IIterator instance.</returns>
        IIterator<TKey> GetKeysInRelativeRadius(Vector3 relativeCenter, float radius, bool useEdgeDistance = true);

        #endregion
    }
}
