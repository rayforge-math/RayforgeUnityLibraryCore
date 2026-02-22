using Rayforge.Core.Environment.Abstractions;
using UnityEngine;

namespace Rayforge.Core.Environment.Spatial.Chunks
{
    /// <summary>
    /// Filters keys from a radius iterator to only return those matching a specific LOD level.
    /// </summary>
    public struct GridLodLevelState
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

        public bool MoveNext(out Vector3Int result)
        {
            while (_rangeState.MoveNext(out result))
            {
                float sqrDist = _registry.GetSqrDistanceToClosestEdge(result, _center);

                if (sqrDist > _maxSqrDist) continue;

                if (_registry.CalculateTargetLOD(sqrDist) == _targetLod)
                {
                    return true;
                }
            }

            result = default;
            return false;
        }
    }
}