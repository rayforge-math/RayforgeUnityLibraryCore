using Rayforge.Core.Environment.Abstractions;
using UnityEngine;

namespace Rayforge.Core.Environment.Spatial
{
    /// <summary>
    /// A high-performance registry that centralizes LOD logic for chunks.
    /// Chunks stay "dumb" while the registry dictates state changes based on distance.
    /// </summary>
    /// <typeparam name="T">The chunk type implementing both spatial and LOD interfaces.</typeparam>
    public class LODChunkRegistry<T> : ChunkRegistry<T>
        where T : Chunk3D<T>, ILODSpatialEntry
    {
        #region Fields & Config
        private readonly float[] _lodSqrDistances;
        private Transform _viewer;

        // Helper to get the current focus position without repeating null-checks.
        private Vector3 ViewerPos => (_viewer != null) ? _viewer.position : Vector3.zero;
        #endregion

        public LODChunkRegistry(ChunkSize gridSize, Vector3 initialAnchor, float[] lodDistances, Transform viewer = null, Transform container = null)
            : base(gridSize, initialAnchor, container)
        {
            _viewer = viewer;
            _lodSqrDistances = new float[lodDistances.Length];
            for (int i = 0; i < lodDistances.Length; i++)
            {
                _lodSqrDistances[i] = lodDistances[i] * lodDistances[i];
            }
        }

        #region Factory Overrides
        /// <summary>
        /// Overrides the base factory to ensure a valid LOD is set immediately upon creation.
        /// This prevents visual popping where a chunk might exist for one frame without LOD.
        /// </summary>
        public override T GetOrCreateChunk(Vector3Int key)
        {
            T chunk = base.GetOrCreateChunk(key);

            float sqrDist = chunk.GetSqrDistanceTo(ViewerPos);
            chunk.UpdateLOD(CalculateTargetLOD(sqrDist));

            return chunk;
        }
        #endregion

        #region Core LOD Logic
        public void UpdateLODs() => UpdateLODs(ViewerPos);

        /// <summary>
        /// Evaluates and updates the LOD level for all active chunks.
        /// Only triggers the chunk's logic if the LOD actually changed.
        /// </summary>
        public void UpdateLODs(Vector3 focusPos)
        {
            foreach (T chunk in AllEntries)
            {
                float sqrDist = chunk.GetSqrDistanceTo(focusPos);
                int targetLod = CalculateTargetLOD(sqrDist);

                if (chunk.CurrentLOD != targetLod)
                {
                    chunk.UpdateLOD(targetLod);
                }
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

        public void SetViewer(Transform viewer) => _viewer = viewer;

        #endregion
    }
}
