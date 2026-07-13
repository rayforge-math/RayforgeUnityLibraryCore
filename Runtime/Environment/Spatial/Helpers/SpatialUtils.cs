using UnityEngine;
using System.Runtime.CompilerServices;

namespace Rayforge.Core.Environment.Spatial.Helpers
{
    /// <summary>
    /// Professional spatial utility for grid-based world generation.
    /// Handles 1D, 2D, and 3D conversions with support for custom anchors (origins).
    /// All calculations use Floor-logic (Key 0 = 0 to Size) for mathematical stability.
    /// </summary>
    public static class SpatialUtils
    {
        #region 1D CORE LOGIC

        /// <summary>
        /// Converts a 1D position to a grid key using Floor-logic.
        /// Formula: floor((position - anchor) / size)
        /// </summary>
        /// <param name="position">World space coordinate.</param>
        /// <param name="gridSize">The physical size of one chunk.</param>
        /// <param name="anchor">The origin offset (e.g., -gridSize/2 to center the grid).</param>
        /// <returns>The integer key of the cell.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int PositionToKey1D(float position, float gridSize, float anchor = 0)
        {
            return Mathf.FloorToInt((position - anchor) / gridSize);
        }

        /// <summary>
        /// Converts a 1D grid key back to a world position.
        /// </summary>
        /// <param name="key">The integer grid key.</param>
        /// <param name="gridSize">The physical size of one chunk.</param>
        /// <param name="anchor">The origin offset.</param>
        /// <param name="centered">If true, returns the center of the cell. If false, returns the minimum corner.</param>
        /// <returns>The world space coordinate.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float KeyToPosition1D(int key, float gridSize, float anchor = 0, bool centered = false)
        {
            float pos = key * gridSize + anchor;
            if (centered) pos += gridSize * 0.5f;
            return pos;
        }

        #endregion

        #region 2D CONVERSIONS

        /// <summary>
        /// Maps a 3D world position to a 2D grid key using the X and Z axes (top-down projection).
        /// </summary>
        /// <param name="position">The 3D world position.</param>
        /// <param name="gridSize">The physical size of one chunk.</param>
        /// <param name="anchor">The world-space origin of the grid.</param>
        /// <returns>A Vector2Int representing the X and Z grid coordinates.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector2Int PositionToKey2D(Vector3 position, float gridSize, Vector3 anchor = default)
        {
            return new Vector2Int(
                PositionToKey1D(position.x, gridSize, anchor.x),
                PositionToKey1D(position.z, gridSize, anchor.z)
            );
        }

        /// <summary>
        /// Maps a 2D world position (XY) to a 2D grid key.
        /// </summary>
        /// <param name="position">The 2D world position.</param>
        /// <param name="gridSize">The physical size of one chunk.</param>
        /// <param name="anchor">The 2D origin offset.</param>
        /// <returns>A Vector2Int representing the grid coordinates.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector2Int PositionToKey2D(Vector2 position, float gridSize, Vector2 anchor = default)
        {
            return new Vector2Int(
                PositionToKey1D(position.x, gridSize, anchor.x),
                PositionToKey1D(position.y, gridSize, anchor.y)
            );
        }

        #endregion

        #region 3D CONVERSIONS

        /// <summary>
        /// Maps a 3D world position to a 3D grid key.
        /// </summary>
        /// <param name="position">The 3D world position.</param>
        /// <param name="gridSize">The physical size of one chunk.</param>
        /// <param name="anchor">The world-space origin.</param>
        /// <returns>A Vector3Int representing the discrete grid coordinates.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector3Int PositionToKey3D(Vector3 position, float gridSize, Vector3 anchor = default)
        {
            return new Vector3Int(
                PositionToKey1D(position.x, gridSize, anchor.x),
                PositionToKey1D(position.y, gridSize, anchor.y),
                PositionToKey1D(position.z, gridSize, anchor.z)
            );
        }

        /// <summary>
        /// Converts a 3D grid key back to world space.
        /// </summary>
        /// <param name="key">The 3D grid coordinate.</param>
        /// <param name="gridSize">The physical size of one chunk.</param>
        /// <param name="anchor">The world-space origin.</param>
        /// <param name="centered">If true, returns the center of the cell volume. If false, returns the minimum corner.</param>
        /// <returns>A Vector3 world position.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector3 KeyToPosition3D(Vector3Int key, float gridSize, Vector3 anchor = default, bool centered = false)
        {
            return new Vector3(
                KeyToPosition1D(key.x, gridSize, anchor.x, centered),
                KeyToPosition1D(key.y, gridSize, anchor.y, centered),
                KeyToPosition1D(key.z, gridSize, anchor.z, centered)
            );
        }

        #endregion

        #region DISTANCE METRICS

        /// <summary>
        /// Calculates the squared distance between two 1D points.
        /// </summary>
        /// <param name="a">First point.</param>
        /// <param name="b">Second point.</param>
        /// <returns>The squared distance (a-b)^2.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float GetSqrDistance1D(float a, float b)
        {
            float delta = a - b;
            return delta * delta;
        }

        /// <summary>
        /// Calculates the squared distance between two 2D points.
        /// </summary>
        /// <param name="a">First point.</param>
        /// <param name="b">Second point.</param>
        /// <returns>The squared distance (L2-norm squared).</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float GetSqrDistance2D(Vector2 a, Vector2 b)
        {
            return GetSqrDistance1D(a.x, b.x) +
                   GetSqrDistance1D(a.y, b.y);
        }

        /// <summary>
        /// Calculates the squared distance between two 3D points.
        /// </summary>
        /// <param name="a">First point.</param>
        /// <param name="b">Second point.</param>
        /// <returns>The squared distance (L2-norm squared).</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float GetSqrDistance3D(Vector3 a, Vector3 b)
        {
            return GetSqrDistance1D(a.x, b.x) +
                   GetSqrDistance1D(a.y, b.y) +
                   GetSqrDistance1D(a.z, b.z);
        }

        /// <summary>
        /// Calculates the squared distance from a 1D position to a segment defined by center and half-size.
        /// Returns 0 if the position is within the segment boundaries.
        /// </summary>
        /// <param name="pos">The world-space coordinate to check.</param>
        /// <param name="center">The center point of the 1D segment.</param>
        /// <param name="halfSize">The half-extent (radius) of the segment.</param>
        /// <returns>The squared distance to the closest boundary.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float GetSqrDistanceToClosestEdge1D(float pos, float center, float halfSize)
        {
            float closest = Mathf.Clamp(pos, center - halfSize, center + halfSize);
            return GetSqrDistance1D(pos, closest);
        }

        /// <summary>
        /// Calculates the squared distance from a 2D position to an AABB.
        /// Returns 0 if the position is inside the box.
        /// </summary>
        /// <param name="pos">The reference position.</param>
        /// <param name="center">The center of the AABB.</param>
        /// <param name="halfExtents">The half-extents (extents) of the box.</param>
        /// <returns>The minimum squared distance to the box's volume.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float GetSqrDistanceToClosestEdge2D(Vector2 pos, Vector2 center, Vector2 halfExtents)
        {
            return GetSqrDistanceToClosestEdge1D(pos.x, center.x, halfExtents.x) +
                   GetSqrDistanceToClosestEdge1D(pos.y, center.y, halfExtents.y);
        }

        /// <summary>
        /// Calculates the squared distance from a 3D position to an AABB.
        /// Returns 0 if the position is inside the box.
        /// </summary>
        /// <param name="pos">The reference position in world space.</param>
        /// <param name="center">The center of the AABB.</param>
        /// <param name="halfExtents">The half-extents (extents) of the box.</param>
        /// <returns>The minimum squared distance to the box's volume.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float GetSqrDistanceToClosestEdge3D(Vector3 pos, Vector3 center, Vector3 halfExtents)
        {
            return GetSqrDistanceToClosestEdge1D(pos.x, center.x, halfExtents.x) +
                   GetSqrDistanceToClosestEdge1D(pos.y, center.y, halfExtents.y) +
                   GetSqrDistanceToClosestEdge1D(pos.z, center.z, halfExtents.z);
        }

        /// <summary>
        /// Calculates squared distance to center using only active axes.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float GetSqrDistanceCentre(Vector3 a, Vector3 b, SpatialAxes axes)
        {
            float dist = 0;
            if ((axes & SpatialAxes.X) != 0) dist += GetSqrDistance1D(a.x, b.x);
            if ((axes & SpatialAxes.Y) != 0) dist += GetSqrDistance1D(a.y, b.y);
            if ((axes & SpatialAxes.Z) != 0) dist += GetSqrDistance1D(a.z, b.z);
            return dist;
        }

        /// <summary>
        /// Calculates squared distance to edge using only active axes.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float GetSqrDistanceEdge(Vector3 pos, Vector3 center, Vector3 halfExtents, SpatialAxes axes)
        {
            float dist = 0;
            if ((axes & SpatialAxes.X) != 0) dist += GetSqrDistanceToClosestEdge1D(pos.x, center.x, halfExtents.x);
            if ((axes & SpatialAxes.Y) != 0) dist += GetSqrDistanceToClosestEdge1D(pos.y, center.y, halfExtents.y);
            if ((axes & SpatialAxes.Z) != 0) dist += GetSqrDistanceToClosestEdge1D(pos.z, center.z, halfExtents.z);
            return dist;
        }

        #endregion

        #region ADDITIONAL HELPERS

        /// <summary>
        /// Returns the local 0.0 to 1.0 interpolation value of a point within its cell.
        /// Useful for UV mapping or noise sampling.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector3 GetCellLocalAlpha(Vector3 worldPos, float gridSize, Vector3 anchor = default)
        {
            Vector3 relativePos = worldPos - anchor;
            return new Vector3(
                (relativePos.x / gridSize) - Mathf.Floor(relativePos.x / gridSize),
                (relativePos.y / gridSize) - Mathf.Floor(relativePos.y / gridSize),
                (relativePos.z / gridSize) - Mathf.Floor(relativePos.z / gridSize)
            );
        }

        #endregion
    }
}