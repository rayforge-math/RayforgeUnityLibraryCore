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
        }

        /// <summary>
        /// Advances the iteration by finding the next coordinate within the specified radius.
        /// Delegates the heavy lifting to the internal _rangeState.
        /// </summary>
        public bool MoveNext(ref GridRadiusState self, out Vector3Int result)
        {
            while (self._rangeState.MoveNext(ref self._rangeState, out result))
            {
                float sqrDist = self._useEdge
                    ? self._registry.GetSqrDistanceToClosestEdge(result, self._center)
                    : self._registry.GetSqrDistanceToCenter(result, self._center);

                if (sqrDist <= self._sqrRadius)
                    return true;
            }

            result = default;
            return false;
        }
    }
}