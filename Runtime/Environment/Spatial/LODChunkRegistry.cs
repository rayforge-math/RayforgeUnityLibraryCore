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
        where T : Chunk<T>, ILODSpatialEntry
    {
        #region Fields & Config
        private float[] _lodSqrDistances;
        private Transform _viewer;

        #region Debug Helper
        [Conditional("UNITY_EDITOR")]
        private void LogDebug(string message, string color = "#FFAB91")
        {
            DebugOutput.Log(message, showDebugLogs, color);
        }
        #endregion

        /// <summary> Helper to get the current focus position without repeating null-checks. </summary>
        private Vector3 ViewerPos => (_viewer != null) ? _viewer.position : Vector3.zero;
        #endregion

        public LODChunkRegistry(ChunkSize gridSize, Vector3 initialAnchor, float[] lodDistances, Transform viewer = null, Transform container = null)
            : base(gridSize, initialAnchor, container)
        {
            _viewer = viewer;
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

            if (changeCount > 0)
            {
                LogDebug($"LOD Update: {changeCount} chunks changed their LOD level.");
            }
        }

        /// <summary>
        /// Maps a squared distance to an LOD index.
        /// </summary>
        protected int CalculateTargetLOD(float sqrDistance)
        {
            for (int i = 0; i < _lodSqrDistances.Length; i++)
            {
                if (sqrDistance < _lodSqrDistances[i]) return i;
            }
            return _lodSqrDistances.Length;
        }
        #endregion

        #region Management & Origin Shift

        /// <summary> Updates the viewer reference (e.g., when switching cameras). </summary>
        public void SetViewer(Transform viewer)
        {
            LogDebug($"Viewer changed to: {(viewer != null ? viewer.name : "NULL")}");
            _viewer = viewer;
        }

        /// <summary>
        /// Updates the internal squared distance thresholds.
        /// Re-calculates squared values to keep the Update loop math simple and fast.
        /// </summary>
        public void UpdateLodDistances(float[] newDistances)
        {
            if (newDistances == null) return;

            _lodSqrDistances = new float[newDistances.Length];
            for (int i = 0; i < newDistances.Length; i++)
            {
                _lodSqrDistances[i] = newDistances[i] * newDistances[i];
            }

            LogDebug($"LOD Distances synchronized. Levels: {newDistances.Length}");
            UpdateLODs();
        }

        #endregion
    }
}
