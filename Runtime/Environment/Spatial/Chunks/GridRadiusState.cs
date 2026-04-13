using Rayforge.Core.Collections.Abstractions;
using Rayforge.Core.Environment.Spatial.Helpers;
using UnityEngine;

namespace Rayforge.Core.Environment.Spatial.Chunks
{
    /// <summary>
    /// A high-performance radial iterator that filters a 3D grid volume.
    /// <para>
    /// Logic: Keys operate as discrete unit vectors, while the center and radius operate in a continuous space. 
    /// The grid size acts as the bridge (transformation scale) between these two worlds.
    /// </para>
    /// <para>
    /// Design Philosophy: This implementation intentionally avoids non-orthogonal basis vectors (shearing). 
    /// By assuming an axis-aligned grid, we maintain linear O(1) distance calculations and avoid 
    /// the computational complexity of parallelepiped volume checks.
    /// </para>
    /// </summary>
    public struct GridRadiusState : IIterationLogic<Vector3Int, GridRadiusState>
    {
        private GridRangeState _rangeState;
        private readonly Vector3 _localCenter;
        private readonly float _sqrRadius;
        private readonly bool _useEdge;
        private readonly Vector3 _gridSize;
        private readonly bool _xActive, _yActive, _zActive;

        private Vector3Int _cachedValue;
        private bool _hasCachedValue;

        /// <summary>
        /// Initializes a new radial filter state with non-uniform grid size.
        /// </summary>
        /// <param name="min">The minimum inclusive unit-vector key of the search volume.</param>
        /// <param name="max">The maximum inclusive unit-vector key of the search volume.</param>
        /// <param name="anchor">The world-space origin of the grid.</param>
        /// <param name="worldCenter">The radius center in World Space.</param>
        /// <param name="radius">The radius in World Space units.</param>
        /// <param name="useEdge">If <see langword="true"/>, measures to the cell's AABB edge; otherwise to the cell center.</param>
        /// <param name="gridSize">Scale factor for each axis, transforms keys into World Space.</param>
        /// <param name="xActive">Whether the X-axis contributes to the distance calculation.</param>
        /// <param name="yActive">Whether the Y-axis contributes to the distance calculation.</param>
        /// <param name="zActive">Whether the Z-axis contributes to the distance calculation.</param>
        public GridRadiusState(
            Vector3Int min, Vector3Int max,
            Vector3 anchor, Vector3 worldCenter, float radius, bool useEdge,
            Vector3 gridSize,
            bool xActive, bool yActive, bool zActive)
        {
            _rangeState = new GridRangeState(min, max);

            _localCenter = worldCenter - anchor;
            _sqrRadius = radius * radius;
            _useEdge = useEdge;
            _gridSize = gridSize;

            _xActive = xActive;
            _yActive = yActive;
            _zActive = zActive;

            _cachedValue = default;
            _hasCachedValue = false;
        }

        /// <summary>
        /// Initializes a new radial filter state.
        /// </summary>
        /// <param name="min">The minimum inclusive unit-vector key of the search volume.</param>
        /// <param name="max">The maximum inclusive unit-vector key of the search volume.</param>
        /// <param name="localCenter">The radius center in World Space.</param>
        /// <param name="radius">The radius in World Space units.</param>
        /// <param name="useEdge">If <see langword="true"/>, measures to the cell's AABB edge; otherwise to the cell center.</param>
        /// <param name="gridSize">The bridge/scale factor for each axis, transforms keys into World Space</param>
        /// <param name="xActive">Whether the X-axis contributes to the distance calculation.</param>
        /// <param name="yActive">Whether the Y-axis contributes to the distance calculation.</param>
        /// <param name="zActive">Whether the Z-axis contributes to the distance calculation.</param>
        public GridRadiusState(
            Vector3Int min, Vector3Int max,
            Vector3 anchor, Vector3 localCenter, float radius, bool useEdge,
            float gridSize,
            bool xActive, bool yActive, bool zActive)
            : this(min, max, anchor, localCenter, radius, useEdge, new Vector3(gridSize, gridSize, gridSize), xActive, yActive, zActive)
        { }

        /// <summary>
        /// Advances the iterator to the next grid key that satisfies the radial constraint.
        /// </summary>
        public bool MoveNext(ref GridRadiusState self, out Vector3Int result)
        {
            CalculateNext(ref self);

            if (self._hasCachedValue)
            {
                result = self._cachedValue;
                self._hasCachedValue = false;
                return true;
            }

            result = default;
            return false;
        }

        /// <summary>
        /// Provides a look-ahead at the next valid coordinate without advancing the iterator.
        /// </summary>
        public bool TryPeekNext(ref GridRadiusState self, out Vector3Int result)
        {
            CalculateNext(ref self);
            result = self._cachedValue;
            return self._hasCachedValue;
        }

        /// <summary>
        /// Determines if a subsequent valid coordinate exists within the radial boundary.
        /// </summary>
        public bool HasNext(ref GridRadiusState self)
        {
            CalculateNext(ref self);
            return self._hasCachedValue;
        }

        /// <summary>
        /// Internal logic: Projects unit-vector keys into the scaled space and performs the distance check.
        /// </summary>
        /// <remarks>
        /// By scaling the keys into world-space before checking the distance, any anisotropy
        /// (stretched or squashed axes) is correctly handled.
        /// </remarks>
        private static void CalculateNext(ref GridRadiusState self)
        {
            if (self._hasCachedValue) return;

            Vector3 halfSizes = self._gridSize * 0.5f;

            while (self._rangeState.MoveNext(ref self._rangeState, out Vector3Int candidate))
            {
                Vector3 cellPosInLocalSpace = new Vector3(
                    self._gridSize.x * candidate.x + halfSizes.x,
                    self._gridSize.y * candidate.y + halfSizes.y,
                    self._gridSize.z * candidate.z + halfSizes.z
                );

                float sqrDist = 0;

                if (self._useEdge)
                {
                    if (self._xActive) sqrDist += SpatialUtils.GetSqrDistanceToClosestEdge1D(self._localCenter.x, cellPosInLocalSpace.x, halfSizes.x);
                    if (self._yActive) sqrDist += SpatialUtils.GetSqrDistanceToClosestEdge1D(self._localCenter.y, cellPosInLocalSpace.y, halfSizes.y);
                    if (self._zActive) sqrDist += SpatialUtils.GetSqrDistanceToClosestEdge1D(self._localCenter.z, cellPosInLocalSpace.z, halfSizes.z);
                }
                else
                {
                    if (self._xActive) sqrDist += SpatialUtils.GetSqrDistance1D(self._localCenter.x, cellPosInLocalSpace.x);
                    if (self._yActive) sqrDist += SpatialUtils.GetSqrDistance1D(self._localCenter.y, cellPosInLocalSpace.y);
                    if (self._zActive) sqrDist += SpatialUtils.GetSqrDistance1D(self._localCenter.z, cellPosInLocalSpace.z);
                }

                if (sqrDist <= self._sqrRadius)
                {
                    self._cachedValue = candidate;
                    self._hasCachedValue = true;
                    return;
                }
            }

            self._hasCachedValue = false;
        }
    }
}