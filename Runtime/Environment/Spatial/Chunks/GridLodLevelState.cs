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

        public GridLodLevelState(GridRangeState range, int targetLod, Vector3 center, float outerRadius, ILODGridProvider<Vector3Int> registry)
        {
            _rangeState = range;
            _targetLod = targetLod;
            _center = center;
            _maxSqrDist = outerRadius * outerRadius;
            _registry = registry;
        }

        public bool MoveNext(ref GridLodLevelState self, out Vector3Int result)
        {
            while (self._rangeState.MoveNext(ref self._rangeState, out result))
            {
                float sqrDist = self._registry.GetSqrDistanceToClosestEdge(result, self._center);

                if (sqrDist > self._maxSqrDist) continue;

                if (self._registry.CalculateTargetLOD(sqrDist) == self._targetLod)
                {
                    return true;
                }
            }

            result = default;
            return false;
        }
    }
}