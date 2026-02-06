using Codice.CM.Common.Checkin.Partial;
using Rayforge.Core.Diagnostics;
using Rayforge.Core.Environment.Abstractions;
using System.Diagnostics;
using UnityEngine;

namespace Rayforge.Core.Environment.Spatial
{
    /// <summary>
    /// A high-performance registry that centralizes LOD logic for chunks.
    /// Chunks stay "dumb" while the registry dictates state changes based on distance, 
    /// automatically respecting ActiveAxes for 2D or 3D distance checks.
    /// </summary>
    /// <typeparam name="T">The chunk type implementing both spatial and LOD interfaces.</typeparam>
    public class LODChunkRegistry<T> : ChunkRegistry<T>
        where T : Chunk<T>, ISpatialLOD
    {
        #region Fields & Config
        private float[] _lodSqrDistances;
        public Transform Viewer { get; private set; }

        #region Debug Helper
        [Conditional("UNITY_EDITOR")]
        private void LogDebug(string message, string color = "#FFAB91")
        {
            DebugOutput.Log(message, showDebugLogs, color);
        }
        #endregion

        /// <summary> Helper to get the current focus position without repeating null-checks. </summary>
        private Vector3 ViewerPos => (Viewer != null) ? Viewer.position : Vector3.zero;
        #endregion

        public LODChunkRegistry(GridSize gridSize, Vector3 initialAnchor, float[] lodDistances, Transform viewer = null, Transform container = null)
            : base(gridSize, initialAnchor, container)
        {
            Viewer = viewer;
            UpdateLodDistances(lodDistances);
        }

        #region Factory Overrides
        /// <summary>
        /// Overrides the base factory to ensure a valid LOD is set immediately upon creation.
        /// Prevents visual popping by calculating the LOD before the first frame is rendered.
        /// </summary>
        public override T GetOrCreateChunk(Vector3Int key)
        {
            T chunk = base.GetOrCreateChunk(key);

            float sqrDist = chunk.GetSqrDistanceToClosestEdge(ViewerPos);
            int targetLod = CalculateTargetLOD(sqrDist);
            chunk.UpdateLOD(targetLod);

            LogDebug($"Created Chunk {key} with initial LOD {targetLod}");

            return chunk;
        }
        #endregion

        #region Core LOD Logic

        /// <summary> Triggers a full LOD update using the current viewer position. </summary>
        public void UpdateLODs() => UpdateLODs(ViewerPos);

        /// <summary>
        /// Evaluates and updates the LOD level for all active chunks.
        /// Only triggers the chunk's update logic if the LOD index actually changed.
        /// </summary>
        public void UpdateLODs(Vector3 focusPos)
        {
            int changeCount = 0;

            foreach (T chunk in AllEntries)
            {
                if (chunk == null) continue;

                float sqrDist = chunk.GetSqrDistanceToClosestEdge(focusPos);
                int targetLod = CalculateTargetLOD(sqrDist);

                if (chunk.CurrentLOD != targetLod)
                {
                    chunk.UpdateLOD(targetLod);
                    changeCount++;
                }
            }

            LogDebug($"LOD Update: {changeCount} chunks changed their LOD level.");
        }

        /// <summary>
        /// Maps a squared distance to an LOD index based on defined thresholds.
        /// Returns the index of the first threshold the distance falls under.
        /// Returns -1 if the distance exceeds all defined LOD ranges (Out of Range).
        /// </summary>
        /// <param name="sqrDistance">The squared distance to check against thresholds.</param>
        /// <returns>The LOD index (0 to N-1) or -1 for culled state.</returns>
        protected int CalculateTargetLOD(float sqrDistance)
        {
            for (int i = 0; i < _lodSqrDistances.Length; i++)
            {
                if (sqrDistance < _lodSqrDistances[i]) return i;
            }
            return -1;
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

            if (_lodSqrDistances != null && _lodSqrDistances.Length == newDistances.Length)
            {
                bool changed = false;
                for (int i = 0; i < newDistances.Length; i++)
                {
                    float sqrDist = newDistances[i] * newDistances[i];
                    if (!Mathf.Approximately(_lodSqrDistances[i], sqrDist))
                    {
                        changed = true;
                        break;
                    }
                }
                if (!changed) return false;
            }

            _lodSqrDistances = new float[newDistances.Length];
            for (int i = 0; i < newDistances.Length; i++)
            {
                _lodSqrDistances[i] = newDistances[i] * newDistances[i];
            }

            LogDebug($"LOD Distances synchronized. Levels: {newDistances.Length}");
            return true;
        }

        #endregion
    }
}
