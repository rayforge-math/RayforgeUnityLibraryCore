using System;
using Rayforge.Core.Collections.Abstractions;
using Rayforge.Core.Environment.Spatial.Helpers;
using UnityEngine;

namespace Rayforge.Core.Environment.Spatial.Chunks
{
    /// <summary>
    /// A high-performance radial LOD iterator that filters a 3D grid volume for keys 
    /// falling strictly within a specific LOD level or an inclusive range of LOD levels.
    /// <para>
    /// Priority Rule: Uses minimum AABB edge distance (<c>GetSqrDistanceEdge</c>). If any part 
    /// of a chunk protrudes into a higher-detail LOD level (smaller index), it is prioritized 
    /// by that higher LOD level and excluded from lower-detail levels.
    /// </para>
    /// </summary>
    public struct GridLODEdgeState : IIterationLogic<Vector3Int, GridLODEdgeState>
    {
        private GridRangeState _rangeState;
        private readonly Vector3 _worldCentre;
        private readonly float _minSqrRadius;
        private readonly float _maxSqrRadius;
        private readonly Vector3 _gridSize;
        private readonly Vector3 _halfSizes;
        private readonly SpatialAxes _activeAxes;

        private Vector3Int _cachedValue;
        private bool _hasCachedValue;

        /// <summary>
        /// Initializes a new LOD filter state using explicit squared distance bounds.
        /// </summary>
        /// <param name="min">The minimum inclusive unit-vector key of the search volume.</param>
        /// <param name="max">The maximum inclusive unit-vector key of the search volume.</param>
        /// <param name="worldCentre">The radius center in World Space.</param>
        /// <param name="minSqrRadius">The inner squared radius threshold (inclusive).</param>
        /// <param name="maxSqrRadius">The outer squared radius threshold (exclusive).</param>
        /// <param name="gridSize">Scale factor for each axis, transforms keys into World Space.</param>
        /// <param name="activeAxes">Determines which axes are active.</param>
        public GridLODEdgeState(
            Vector3Int min, Vector3Int max,
            Vector3 worldCentre,
            float minSqrRadius, float maxSqrRadius,
            Vector3 gridSize,
            SpatialAxes activeAxes)
        {
            // 1. Validate volume bounds
            if (min.x > max.x || min.y > max.y || min.z > max.z)
                throw new ArgumentException("Minimum volume key cannot exceed maximum volume key on any axis.", nameof(min));

            // 2. Validate active axes & grid size matching active axes
            if (activeAxes == SpatialAxes.None)
                throw new ArgumentException("At least one axis must be active.", nameof(activeAxes));

            if (((activeAxes & SpatialAxes.X) != 0 && gridSize.x <= 0f) ||
                ((activeAxes & SpatialAxes.Y) != 0 && gridSize.y <= 0f) ||
                ((activeAxes & SpatialAxes.Z) != 0 && gridSize.z <= 0f))
            {
                throw new ArgumentOutOfRangeException(nameof(gridSize), "Grid size must be greater than zero on all active axes.");
            }

            // 3. Validate radius thresholds
            if (float.IsNaN(minSqrRadius) || minSqrRadius < 0f)
                throw new ArgumentOutOfRangeException(nameof(minSqrRadius), "Minimum squared radius cannot be negative or NaN.");

            if (float.IsNaN(maxSqrRadius) || maxSqrRadius < 0f)
                throw new ArgumentOutOfRangeException(nameof(maxSqrRadius), "Maximum squared radius cannot be negative or NaN.");

            if (minSqrRadius > maxSqrRadius)
                throw new ArgumentException("Minimum squared radius cannot exceed maximum squared radius.", nameof(minSqrRadius));

            _rangeState = new GridRangeState(min, max);
            _worldCentre = worldCentre;
            _minSqrRadius = minSqrRadius;
            _maxSqrRadius = maxSqrRadius;
            _gridSize = gridSize;
            _halfSizes = gridSize * 0.5f;
            _activeAxes = activeAxes;

            _cachedValue = default;
            _hasCachedValue = false;
        }

        /// <summary>
        /// Initializes a new LOD filter state for an inclusive LOD index range (from <paramref name="minLodIndex"/> to <paramref name="maxLodIndex"/>).
        /// </summary>
        public GridLODEdgeState(
            Vector3Int min, Vector3Int max,
            Vector3 worldCentre,
            int minLodIndex, int maxLodIndex,
            ReadOnlySpan<float> lodSqrDistances,
            Vector3 gridSize,
            SpatialAxes activeAxes)
            : this(
                min, max, worldCentre,
                GetMinSqrRadius(minLodIndex, maxLodIndex, lodSqrDistances),
                GetMaxSqrRadius(minLodIndex, maxLodIndex, lodSqrDistances),
                gridSize, activeAxes)
        { }

        /// <summary>
        /// Initializes a new LOD filter state for a single target LOD index.
        /// </summary>
        public GridLODEdgeState(
            Vector3Int min, Vector3Int max,
            Vector3 worldCentre,
            int lodIndex,
            ReadOnlySpan<float> lodSqrDistances,
            Vector3 gridSize,
            SpatialAxes activeAxes)
            : this(min, max, worldCentre, lodIndex, lodIndex, lodSqrDistances, gridSize, activeAxes)
        { }

        private static float GetMinSqrRadius(int minLodIndex, int maxLodIndex, ReadOnlySpan<float> lodSqrDistances)
        {
            ValidateLodIndices(minLodIndex, maxLodIndex, lodSqrDistances);
            return minLodIndex <= 0 ? 0f : lodSqrDistances[minLodIndex - 1];
        }

        private static float GetMaxSqrRadius(int minLodIndex, int maxLodIndex, ReadOnlySpan<float> lodSqrDistances)
        {
            ValidateLodIndices(minLodIndex, maxLodIndex, lodSqrDistances);
            return lodSqrDistances[maxLodIndex];
        }

        private static void ValidateLodIndices(int minLodIndex, int maxLodIndex, ReadOnlySpan<float> lodSqrDistances)
        {
            if (lodSqrDistances.IsEmpty)
                throw new ArgumentException("LOD squared distances collection cannot be empty.", nameof(lodSqrDistances));

            if (minLodIndex < 0 || minLodIndex >= lodSqrDistances.Length)
                throw new ArgumentOutOfRangeException(nameof(minLodIndex), "Invalid minimum LOD index.");

            if (maxLodIndex < minLodIndex || maxLodIndex >= lodSqrDistances.Length)
                throw new ArgumentOutOfRangeException(nameof(maxLodIndex), "Invalid maximum LOD index.");
        }

        /// <summary>
        /// Advances the iterator to the next grid key that falls within the specified LOD range.
        /// </summary>
        public bool MoveNext(ref GridLODEdgeState self, out Vector3Int result)
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
        public bool TryPeekNext(ref GridLODEdgeState self, out Vector3Int result)
        {
            CalculateNext(ref self);
            result = self._cachedValue;
            return self._hasCachedValue;
        }

        /// <summary>
        /// Determines if a subsequent valid coordinate exists within the LOD boundary.
        /// </summary>
        public bool HasNext(ref GridLODEdgeState self)
        {
            CalculateNext(ref self);
            return self._hasCachedValue;
        }

        /// <summary>
        /// Internal logic: Evaluates candidate keys against LOD boundaries using minimum AABB distance.
        /// </summary>
        private static void CalculateNext(ref GridLODEdgeState self)
        {
            if (self._hasCachedValue) return;

            while (self._rangeState.MoveNext(ref self._rangeState, out Vector3Int candidate))
            {
                Vector3 cellPos = new Vector3(
                    self._gridSize.x * candidate.x + self._halfSizes.x,
                    self._gridSize.y * candidate.y + self._halfSizes.y,
                    self._gridSize.z * candidate.z + self._halfSizes.z
                );

                // Computes the minimum squared distance from worldCentre to the chunk's AABB
                float sqrDist = SpatialUtils.GetSqrDistanceEdge(self._worldCentre, cellPos, self._halfSizes, self._activeAxes);

                // Priority check:
                // 1. sqrDist >= _minSqrRadius ensures the chunk doesn't touch any inner (higher detail) LOD level.
                // 2. sqrDist < _maxSqrRadius ensures the nearest point of the chunk is within this LOD's outer bound.
                if (sqrDist >= self._minSqrRadius && sqrDist < self._maxSqrRadius)
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