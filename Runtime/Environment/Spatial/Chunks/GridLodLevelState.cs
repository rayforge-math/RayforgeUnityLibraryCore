using Rayforge.Core.Collections.Abstractions;
using Rayforge.Core.Environment.Abstractions;
using UnityEngine;

namespace Rayforge.Core.Environment.Spatial.Chunks
{
    /// <summary>
    /// Filters keys from a radius iterator to only return those matching a specific LOD level.
    /// </summary>
    public struct GridLodLevelState : IIterationLogic<Vector3Int, GridLodLevelState>
    {
        private GridRangeState _rangeState;
        private readonly int _targetLod;
        private readonly Vector3 _center;
        private readonly float _maxSqrDist;
        private readonly ILODGridProvider<Vector3Int> _registry;

        private Vector3Int _cachedTile;
        private bool _hasCachedTile;

        public GridLodLevelState(GridRangeState range, int targetLod, Vector3 center, float outerRadius, ILODGridProvider<Vector3Int> registry)
        {
            _rangeState = range;
            _targetLod = targetLod;
            _center = center;
            _maxSqrDist = outerRadius * outerRadius;
            _registry = registry;
            _cachedTile = default;
            _hasCachedTile = false;
        }

        /// <summary>
        /// Checks if another tile matching the target LOD is available.
        /// </summary>
        public bool HasNext(ref GridLodLevelState self)
        {
            FetchNextValid(ref self);
            return self._hasCachedTile;
        }

        /// <summary>
        /// Provides a peek at the next tile that matches the LOD criteria.
        /// Crucial for synchronizing LOD-based updates with GPU buffer segments.
        /// </summary>
        public bool TryPeekNext(ref GridLodLevelState self, out Vector3Int result)
        {
            FetchNextValid(ref self);
            result = self._cachedTile;
            return self._hasCachedTile;
        }

        /// <summary>
        /// Returns the pre-filtered tile or scans ahead until one is found.
        /// </summary>
        public bool MoveNext(ref GridLodLevelState self, out Vector3Int result)
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
        private static void FetchNextValid(ref GridLodLevelState self)
        {
            if (self._hasCachedTile) return;

            while (self._rangeState.MoveNext(ref self._rangeState, out Vector3Int candidate))
            {
                float sqrDist = self._registry.GetSqrDistanceToClosestEdge(candidate, self._center);

                if (sqrDist > self._maxSqrDist) continue;

                if (self._registry.CalculateTargetLOD(sqrDist) == self._targetLod)
                {
                    self._cachedTile = candidate;
                    self._hasCachedTile = true;
                    return;
                }
            }
        }
    }
}