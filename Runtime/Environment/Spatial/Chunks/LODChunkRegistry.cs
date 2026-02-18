using Rayforge.Core.Diagnostics;
using Rayforge.Core.Environment.Abstractions;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using UnityEngine;

namespace Rayforge.Core.Environment.Spatial.Chunks
{
    /// <summary>
    /// A high-performance registry that centralizes LOD logic for chunks.
    /// Chunks stay "dumb" while the registry dictates state changes based on distance, 
    /// automatically respecting ActiveAxes for 2D or 3D distance checks.
    /// </summary>
    /// <typeparam name="T">The chunk type implementing both spatial and LOD interfaces.</typeparam>
    public class LODChunkRegistry<T> : ChunkRegistry<T>, ILODGridProvider
        where T : LODChunk<T>
    {
        #region Fields & Config

        private float[] _lodSqrDistances;
        private float[] _lodDistances;
        private readonly bool _deactivateOnCulled;

        public Transform Viewer { get; private set; }

        /// <summary> 
        /// High-performance access to the squared thresholds. 
        /// Avoids array copying and heap allocations.
        /// </summary>
        public ReadOnlySpan<float> LodSqrDistances => _lodSqrDistances;

        /// <summary> 
        /// High-performance access to the thresholds. 
        /// Avoids array copying and heap allocations.
        /// </summary>
        public ReadOnlySpan<float> LodDistances => _lodDistances;

        /// <summary> Implementation of ILODGridProvider. Returns current viewer position. </summary>
        public Vector3 ViewerPos => (Viewer != null) ? Viewer.position : Vector3.zero;

        /// <summary> Implementation of ILODGridProvider. Returns number of LOD levels. </summary>
        public int LodCount => _lodSqrDistances?.Length ?? 0;

        #endregion

        public LODChunkRegistry(GridSize gridSize, Vector3 initialAnchor, float[] lodDistances, bool deactivateOnCulled = true, Transform viewer = null, Transform container = null)
            : base(gridSize, initialAnchor, container)
        {
            _deactivateOnCulled = deactivateOnCulled;
            Viewer = viewer;
            UpdateLodDistances(lodDistances);
        }

        #region Factory Overrides

        /// <summary>
        /// Overrides the base factory to ensure a valid LOD is set immediately upon creation.
        /// Prevents visual popping by calculating the LOD before the first frame is rendered.
        /// </summary>
        public override bool GetOrCreateChunk(Vector3Int key, Action<T> onConfigure, out T chunk)
        {
            bool isNew = base.GetOrCreateChunk(key, onConfigure, out chunk);
            if (isNew)
            {
                UpdateChunkLOD(chunk, ViewerPos);
            }

            LogDebug($"Created Chunk {key} with initial LOD {chunk.CurrentLOD}");
            return isNew;
        }

        #endregion

        #region ILODGridProvider Implementation

        /// <summary>
        /// Maps a squared distance to an LOD index.
        /// Implements the core logic for both internal updates and external provider queries.
        /// </summary>
        public int CalculateTargetLOD(float sqrDistance)
        {
            ReadOnlySpan<float> thresholds = LodSqrDistances;
            for (int i = 0; i < thresholds.Length; i++)
            {
                if (sqrDistance < thresholds[i]) return i;
            }
            return -1;
        }

        /// <summary>
        /// Returns all grid keys that fall exactly into a specific LOD level.
        /// </summary>
        public IEnumerable<Vector3Int> GetKeysInLODLevel(int lodIndex, Vector3 center)
        {
            if (lodIndex < 0 || lodIndex >= LodCount) yield break;

            float outerRadius = LodDistances[lodIndex];

            foreach (var key in GetKeysInRadius(center, outerRadius, useEdgeDistance: true))
            {
                float sqrDist = GetSqrDistanceToClosestEdge(key, center);
                if (CalculateTargetLOD(sqrDist) == lodIndex)
                {
                    yield return key;
                }
            }
        }

        /// <summary>
        /// Returns the exact count of cells in an LOD level without allocations.
        /// Perfect for atlas memory planning.
        /// </summary>
        public int GetKeyCountInLODLevel(int lodIndex, Vector3 center)
        {
            if (lodIndex < 0 || lodIndex >= LodCount) return 0;

            float outerRadius = LodDistances[lodIndex];
            Bounds searchBounds = new Bounds(center, Vector3.one * outerRadius * 2f);
            int count = 0;

            foreach (var key in GetKeysInBounds(searchBounds))
            {
                float sqrDist = GetSqrDistanceToClosestEdge(key, center);
                if (CalculateTargetLOD(sqrDist) == lodIndex)
                {
                    count++;
                }
            }
            return count;
        }

        /// <summary>
        /// Returns all keys within the maximum visibility range.
        /// </summary>
        public IEnumerable<Vector3Int> GetKeysInFullRange(Vector3 center)
        {
            if (LodCount == 0) return Enumerable.Empty<Vector3Int>();

            float maxRadius = LodDistances[LodCount - 1];
            return GetKeysInRadius(center, maxRadius, useEdgeDistance: true);
        }

        /// <summary>
        /// Returns the total count of all cells within maximum range.
        /// </summary>
        public int GetKeyCountInFullRange(Vector3 center)
        {
            if (LodCount == 0) return 0;

            var distances = LodDistances;
            float maxRadius = distances[LodCount - 1];
            Bounds searchBounds = new Bounds(center, Vector3.one * maxRadius * 2f);
            int count = 0;

            var sqrDistances = LodSqrDistances;
            foreach (var key in GetKeysInBounds(searchBounds))
            {
                if (GetSqrDistanceToClosestEdge(key, center) <= sqrDistances[LodCount - 1])
                {
                    count++;
                }
            }
            return count;
        }

        /// <summary>
        /// Implements ILODGridProvider.GetMaxCapacityForLODLevel.
        /// Ensures the Atlas has enough slices even in the worst-case grid alignment.
        /// </summary>
        public int GetMaxCapacityForLODLevel(int lodIndex)
        {
            if (lodIndex < 0 || lodIndex >= LodCount) return 0;

            float outerRadius = LodDistances[lodIndex];
            float size = (float)GridSize;

            int outerMaxAxis = Mathf.CeilToInt((outerRadius * 2f) / size) + 1;
            int outerMaxCount = CalculateCountForActiveAxes(outerMaxAxis);

            int innerMinCount = 0;
            if (lodIndex > 0)
            {
                float innerRadius = LodDistances[lodIndex - 1];
                int innerMinAxis = Mathf.Max(0, Mathf.FloorToInt((innerRadius * 2f) / size) - 1);
                innerMinCount = CalculateCountForActiveAxes(innerMinAxis);
            }

            return Mathf.Max(0, outerMaxCount - innerMinCount);
        }

        /// <summary>
        /// Helper to calculate cell counts based on which axes are currently active.
        /// Handles 1D, 2D, and 3D configurations automatically.
        /// </summary>
        private int CalculateCountForActiveAxes(int axisCount)
        {
            if (axisCount <= 0) return 0;

            int total = 1;
            bool anyActive = false;

            if (IsXActive) { total *= axisCount; anyActive = true; }
            if (IsYActive) { total *= axisCount; anyActive = true; }
            if (IsZActive) { total *= axisCount; anyActive = true; }

            return anyActive ? total : 0;
        }

        #endregion

        #region Core LOD Logic

        public void UpdateLODs() => UpdateLODs(ViewerPos);

        public int UpdateLODs(Vector3 focusPos)
        {
            int changeCount = 0;
            foreach (T chunk in AllEntries)
            {
                if (chunk != null && UpdateChunkLOD(chunk, focusPos))
                {
                    changeCount++;
                }
            }
            return changeCount;
        }

        private bool UpdateChunkLOD(T chunk, Vector3 pos)
        {
            float sqrDist = GetSqrDistanceToClosestEdge(chunk.GridKey, pos);
            int targetLod = CalculateTargetLOD(sqrDist);
            return ((ILODReceiver)chunk).UpdateLOD(targetLod, _deactivateOnCulled);
        }

        #endregion

        #region Management & Origin Shift

        /// <summary> Updates the viewer reference (e.g., when switching cameras). </summary>
        public bool SetViewer(Transform viewer)
        {
            if (Viewer != viewer)
            {
                Viewer = viewer;
                LogDebug($"Viewer changed to: {(viewer != null ? viewer.name : "NULL")}");
                return true;
            }
            return false;
        }

        /// <summary>
        /// Updates the internal squared distance thresholds.
        /// Re-calculates squared values to keep the Update loop math simple and fast.
        /// </summary>
        public bool UpdateLodDistances(float[] newDistances)
        {
            if (newDistances == null) return false;

            if (_lodDistances != null && _lodDistances.Length == newDistances.Length)
            {
                bool changed = false;
                for (int i = 0; i < newDistances.Length; i++)
                {
                    if (!Mathf.Approximately(_lodDistances[i], newDistances[i]))
                    {
                        changed = true;
                        break;
                    }
                }
                if (!changed) return false;
            }

            int count = newDistances.Length;
            _lodDistances = new float[count];
            _lodSqrDistances = new float[count];

            for (int i = 0; i < count; i++)
            {
                float d = newDistances[i];
                _lodDistances[i] = d;
                _lodSqrDistances[i] = d * d;
            }

            LogDebug($"LOD Distances synchronized. Levels: {count}. Max Range: {newDistances[count - 1]}m");
            return true;
        }

        #endregion

        #region Debug Helper
        [Conditional("UNITY_EDITOR")]
        private void LogDebug(string message, string color = "#FFAB91")
        {
            DebugOutput.Log(message, showDebugLogs, color);
        }
        #endregion
    }
}
