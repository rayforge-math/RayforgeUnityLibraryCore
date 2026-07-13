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
    public struct GridRadiusCentreState : IIterationLogic<Vector3Int, GridRadiusCentreState>
    {
        private GridRangeState _rangeState;
        private readonly Vector3 _worldCentre;
        private readonly float _sqrRadius;
        private readonly Vector3 _gridSize;
        private readonly Vector3 _halfSizes;
        private readonly SpatialAxes _activeAxes;

        private Vector3Int _cachedValue;
        private bool _hasCachedValue;

        /// <summary>
        /// Initializes a new radial filter state with non-uniform grid size.
        /// </summary>
        /// <param name="min">The minimum inclusive unit-vector key of the search volume.</param>
        /// <param name="max">The maximum inclusive unit-vector key of the search volume.</param>
        /// <param name="worldCentre">The radius centre in World Space.</param>
        /// <param name="radius">The radius in World Space units.</param>
        /// <param name="gridSize">Scale factor for each axis, transforms keys into World Space.</param>
        /// <param name="activeAxes">Determines which axes are active.</param>
        public GridRadiusCentreState(
            Vector3Int min, Vector3Int max,
            Vector3 worldCentre, float radius,
            Vector3 gridSize,
            SpatialAxes activeAxes)
        {
            if (gridSize.x <= 0 || gridSize.y <= 0 || gridSize.z <= 0)
                throw new System.ArgumentOutOfRangeException(nameof(gridSize), "Grid size must be greater than zero on all active axes.");

            if (radius < 0)
                throw new System.ArgumentOutOfRangeException(nameof(radius), "Radius cannot be negative.");

            if (activeAxes == SpatialAxes.None)
                throw new System.ArgumentException("At least one axis must be active.", nameof(activeAxes));

            _rangeState = new GridRangeState(min, max);
            _worldCentre = worldCentre;
            _sqrRadius = radius * radius;
            _gridSize = gridSize;
            _halfSizes = gridSize * 0.5f;
            _activeAxes = activeAxes;

            _cachedValue = default;
            _hasCachedValue = false;
        }

        /// <summary>
        /// Initializes a new radial filter state.
        /// </summary>
        /// <param name="min">The minimum inclusive unit-vector key of the search volume.</param>
        /// <param name="max">The maximum inclusive unit-vector key of the search volume.</param>
        /// <param name="worldCentre">The radius centre in World Space.</param>
        /// <param name="radius">The radius in World Space units.</param>
        /// <param name="gridSize">The bridge/scale factor for each axis, transforms keys into World Space</param>
        /// <param name="activeAxes">Determines which axes are active.</param>
        public GridRadiusCentreState(
            Vector3Int min, Vector3Int max,
            Vector3 worldCentre, float radius,
            float gridSize,
            SpatialAxes activeAxes)
            : this(min, max, worldCentre, radius, new Vector3(gridSize, gridSize, gridSize), activeAxes)
        { }

        /// <summary>
        /// Advances the iterator to the next grid key that satisfies the radial constraint.
        /// </summary>
        public bool MoveNext(ref GridRadiusCentreState self, out Vector3Int result)
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
        public bool TryPeekNext(ref GridRadiusCentreState self, out Vector3Int result)
        {
            CalculateNext(ref self);
            result = self._cachedValue;
            return self._hasCachedValue;
        }

        /// <summary>
        /// Determines if a subsequent valid coordinate exists within the radial boundary.
        /// </summary>
        public bool HasNext(ref GridRadiusCentreState self)
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
        private static void CalculateNext(ref GridRadiusCentreState self)
        {
            if (self._hasCachedValue) return;

            while (self._rangeState.MoveNext(ref self._rangeState, out Vector3Int candidate))
            {
                Vector3 cellPos = new Vector3(
                    self._gridSize.x * candidate.x + self._halfSizes.x,
                    self._gridSize.y * candidate.y + self._halfSizes.y,
                    self._gridSize.z * candidate.z + self._halfSizes.z
                );

                if (SpatialUtils.GetSqrDistanceCentre(self._worldCentre, cellPos, self._activeAxes) <= self._sqrRadius)
                {
                    self._cachedValue = candidate;
                    self._hasCachedValue = true;
                    return;
                }
            }

            self._cachedValue = default;
            self._hasCachedValue = false;
        }
    }
}