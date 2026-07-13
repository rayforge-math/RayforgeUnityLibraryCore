using Rayforge.Core.Collections.Abstractions;
using Rayforge.Core.Environment.Spatial.Helpers;
using UnityEngine;

namespace Rayforge.Core.Environment.Spatial.Chunks
{
    /// <summary>
    /// Filters keys from a radius iterator to only return those matching a specific LOD level.
    /// </summary>
    public struct GridLodState : IIterationLogic<Vector3Int, GridLodState>
    {
        private GridRangeState _rangeState;
        private readonly int _targetLod;
        private readonly Vector3 _center;
        private readonly float _maxSqrDist;
        private readonly float[] _sqrThresholds;
        private readonly Vector3 _gridSize;
        private readonly Vector3 _halfSize;
        private readonly SpatialAxes _axes;

        private Vector3Int _cachedTile;
        private bool _hasCachedTile;

        public GridLodState(
            GridRangeState range,
            int targetLod,
            Vector3 center,
            float outerRadius,
            float[] sqrThresholds,
            Vector3 gridSize,
            SpatialAxes axes = SpatialAxes.X | SpatialAxes.Y | SpatialAxes.Z)
        {
            _rangeState = range;
            _targetLod = targetLod;
            _center = center;
            _maxSqrDist = outerRadius * outerRadius;
            _sqrThresholds = sqrThresholds;
            _gridSize = gridSize;
            _halfSize = gridSize * 0.5f;
            _axes = axes;
            _cachedTile = default;
            _hasCachedTile = false;
        }

        /// <summary>
        /// Checks if another tile matching the target LOD is available.
        /// </summary>
        public bool HasNext(ref GridLodState self)
        {
            FetchNextValid(ref self);
            return self._hasCachedTile;
        }

        /// <summary>
        /// Provides a peek at the next tile that matches the LOD criteria.
        /// Crucial for synchronizing LOD-based updates with GPU buffer segments.
        /// </summary>
        public bool TryPeekNext(ref GridLodState self, out Vector3Int result)
        {
            FetchNextValid(ref self);
            result = self._cachedTile;
            return self._hasCachedTile;
        }

        /// <summary>
        /// Returns the pre-filtered tile or scans ahead until one is found.
        /// </summary>
        public bool MoveNext(ref GridLodState self, out Vector3Int result)
        {
            FetchNextValid(ref self);

            if (self._hasCachedTile)
            {
                result = self._cachedTile;
                self._cachedTile = default;
                self._hasCachedTile = false;
                return true;
            }

            result = default;
            return false;
        }

        /// <summary>
        /// The core filter logic. English: Advances the inner range state until a tile 
        /// within the radius and matching the target LOD is found.
        /// </summary>
        private static void FetchNextValid(ref GridLodState self)
        {
            if (self._hasCachedTile) return;

            while (self._rangeState.MoveNext(ref self._rangeState, out Vector3Int candidate))
            {
                Vector3 cellPos = new Vector3(
                    candidate.x * self._gridSize.x + self._halfSize.x,
                    candidate.y * self._gridSize.y + self._halfSize.y,
                    candidate.z * self._gridSize.z + self._halfSize.z
                );

                float sqrDist = SpatialUtils.GetSqrDistanceEdge(self._center, cellPos, self._halfSize, self._axes);

                if (sqrDist <= self._maxSqrDist && LodUtils.CalculateTargetLOD(sqrDist, self._sqrThresholds) == self._targetLod)
                {
                    self._cachedTile = candidate;
                    self._hasCachedTile = true;
                    return;
                }
            }

            self._cachedTile = default;
            self._hasCachedTile = false;
        }
    }
}