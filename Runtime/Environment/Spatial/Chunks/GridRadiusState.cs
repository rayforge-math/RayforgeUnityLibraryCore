using Rayforge.Core.Collections.Abstractions;
using Rayforge.Core.Environment.Abstractions;
using UnityEngine;

namespace Rayforge.Core.Environment.Spatial.Chunks
{
    /// <summary>
    /// Wraps a range state and filters keys based on a radial distance check.
    /// Implements IIterationLogic to integrate with the universal Iterator.
    /// </summary>
    public struct GridRadiusState : IIterationLogic<Vector3Int, GridRadiusState>
    {
        private GridRangeState _rangeState;
        private readonly Vector3 _center;
        private readonly float _sqrRadius;
        private readonly bool _useEdge;
        private readonly ISpatialGridProvider<Vector3Int> _registry;

        private Vector3Int _cachedValue;
        private bool _hasCachedValue;

        /// <summary>
        /// Initializes the radius filter with a range, center point, and distance logic provider.
        /// </summary>
        public GridRadiusState(GridRangeState range, Vector3 center, float radius, bool useEdge, ISpatialGridProvider<Vector3Int> registry)
        {
            _rangeState = range;
            _center = center;
            _sqrRadius = radius * radius;
            _useEdge = useEdge;
            _registry = registry;

            _cachedValue = default;
            _hasCachedValue = false;
        }

        /// <summary>
        /// Checks if a next coordinate within the radius exists by pre-scanning the range.
        /// </summary>
        public bool HasNext(ref GridRadiusState self)
        {
            FetchNext(ref self);
            return self._hasCachedValue;
        }

        /// <summary>
        /// Provides a look-ahead at the next valid tile without consuming it.
        /// Useful for coordinating radial updates across different systems.
        /// </summary>
        public bool TryPeekNext(ref GridRadiusState self, out Vector3Int result)
        {
            FetchNext(ref self);
            result = self._cachedValue;
            return self._hasCachedValue;
        }

        /// <summary>
        /// Returns the pre-validated coordinate or advances the range until one is found.
        /// </summary>
        public bool MoveNext(ref GridRadiusState self, out Vector3Int result)
        {
            FetchNext(ref self);

            if (self._hasCachedValue)
            {
                result = self._cachedValue;
                self._cachedValue = default;
                self._hasCachedValue = false;
                return true;
            }

            result = default;
            return false;
        }

        /// <summary>
        /// Core filter logic: Advances the internal range state and performs the distance check.
        /// English: Moves the pointer to the next valid coordinate that falls within the radius.
        /// </summary>
        private static void FetchNext(ref GridRadiusState self)
        {
            if (self._hasCachedValue) return;

            while (self._rangeState.MoveNext(ref self._rangeState, out Vector3Int candidate))
            {
                float sqrDist = self._useEdge
                    ? self._registry.GetSqrDistanceToClosestEdge(candidate, self._center)
                    : self._registry.GetSqrDistanceToCenter(candidate, self._center);

                if (sqrDist <= self._sqrRadius)
                {
                    self._cachedValue = candidate;
                    self._hasCachedValue = true;
                    return;
                }
            }
        }
    }
}