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
        private readonly Vector3 _localCentre;
        private readonly float _sqrRadius;
        private readonly Vector3 _gridSize;
        private readonly Vector3 _halfSizes;
        private readonly SpatialAxes _activeAxes;
        private readonly bool _useEdge;

        private Vector3Int _cachedValue;
        private bool _hasCachedValue;

        /// <summary>
        /// Initializes a new radial filter state with non-uniform grid size.
        /// </summary>
        /// <param name="min">The minimum inclusive unit-vector key of the search volume.</param>
        /// <param name="max">The maximum inclusive unit-vector key of the search volume.</param>
        /// <param name="localCentre">The radius centre in Local Space.</param>
        /// <param name="radius">The radius in World Space units.</param>
        /// <param name="useEdge">If <see langword="true"/>, measures to the cell's AABB edge; otherwise to the cell center.</param>
        /// <param name="gridSize">Scale factor for each axis, transforms keys into World Space.</param>
        /// <param name="activeAxes">Determines which axes are active.</param>
        public GridRadiusState(
            Vector3Int min, Vector3Int max,
            Vector3 localCentre, float radius, bool useEdge,
            Vector3 gridSize,
            SpatialAxes activeAxes)
        {
            _rangeState = new GridRangeState(min, max);
            _localCentre = localCentre;
            _sqrRadius = radius * radius;
            _gridSize = gridSize;
            _halfSizes = gridSize * 0.5f;
            _activeAxes = activeAxes;
            _useEdge = useEdge;

            _cachedValue = default;
            _hasCachedValue = false;
        }

        /// <summary>
        /// Initializes a new radial filter state.
        /// </summary>
        /// <param name="min">The minimum inclusive unit-vector key of the search volume.</param>
        /// <param name="max">The maximum inclusive unit-vector key of the search volume.</param>
        /// <param name="localCentre">The radius centre in Local Space.</param>
        /// <param name="radius">The radius in World Space units.</param>
        /// <param name="useEdge">If <see langword="true"/>, measures to the cell's AABB edge; otherwise to the cell center.</param>
        /// <param name="gridSize">The bridge/scale factor for each axis, transforms keys into World Space</param>
        /// <param name="activeAxes">Determines which axes are active.</param>
        public GridRadiusState(
            Vector3Int min, Vector3Int max,
            Vector3 localCentre, float radius, bool useEdge,
            float gridSize,
            SpatialAxes activeAxes)
            : this(min, max, localCentre, radius, useEdge, new Vector3(gridSize, gridSize, gridSize), activeAxes)
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

            bool xAct = (self._activeAxes & SpatialAxes.X) != 0;
            bool yAct = (self._activeAxes & SpatialAxes.Y) != 0;
            bool zAct = (self._activeAxes & SpatialAxes.Z) != 0;

            while (self._rangeState.MoveNext(ref self._rangeState, out Vector3Int candidate))
            {
                Vector3 cellPos = new Vector3(
                    self._gridSize.x * candidate.x + self._halfSizes.x,
                    self._gridSize.y * candidate.y + self._halfSizes.y,
                    self._gridSize.z * candidate.z + self._halfSizes.z
                );

                float sqrDist = 0;

                if (self._useEdge)
                {
                    if (xAct) sqrDist += SpatialUtils.GetSqrDistanceToClosestEdge1D(self._localCentre.x, cellPos.x, self._halfSizes.x);
                    if (yAct) sqrDist += SpatialUtils.GetSqrDistanceToClosestEdge1D(self._localCentre.y, cellPos.y, self._halfSizes.y);
                    if (zAct) sqrDist += SpatialUtils.GetSqrDistanceToClosestEdge1D(self._localCentre.z, cellPos.z, self._halfSizes.z);
                }
                else
                {
                    if (xAct) sqrDist += SpatialUtils.GetSqrDistance1D(self._localCentre.x, cellPos.x);
                    if (yAct) sqrDist += SpatialUtils.GetSqrDistance1D(self._localCentre.y, cellPos.y);
                    if (zAct) sqrDist += SpatialUtils.GetSqrDistance1D(self._localCentre.z, cellPos.z);
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