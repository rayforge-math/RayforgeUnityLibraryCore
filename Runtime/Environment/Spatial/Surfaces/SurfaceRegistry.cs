using Rayforge.Core.Environment.Spatial;
using Rayforge.Core.Environment.Spatial.Surfaces;
using System.Collections.Generic;
using UnityEngine;

namespace Rayforge.Core.Environment.Spatial.Surface
{
    /// <summary>
    /// Manages the registration and state tracking of spatial objects for heightmap projection.
    /// English: Only accepts objects with valid Mesh data. Uses anchor-relative coordinates 
    /// to remain stable during Origin Shifts by synchronizing with a LODChunkRegistry.
    /// </summary>
    public class SurfaceRegistry
    {
        #region Fields
        /// <summary> Internal storage for all tracked object states, indexed by their Unity InstanceID. </summary>
        private readonly Dictionary<int, SpatialObjectState> _registry = new Dictionary<int, SpatialObjectState>();

        /// <summary> 
        /// Collection of chunk coordinates that require a re-bake. 
        /// English: Uses Vector2Int for 2D XZ-grid mapping.
        /// </summary>
        private readonly HashSet<Vector2Int> _dirtyChunks = new HashSet<Vector2Int>();

        /// <summary> Reference to the spatial grid provider. </summary>
        private readonly LODChunkRegistry<SurfaceChunk> _chunkRegistry;

        /// <summary> Gets the current reference anchor from the ChunkRegistry. </summary>
        private Vector2 CurrentAnchor => new Vector2(_chunkRegistry.Anchor.x, _chunkRegistry.Anchor.z);

        /// <summary> Gets the world-scale size of a single chunk. </summary>
        private float ChunkSize => (float)_chunkRegistry.GridSize;
        #endregion

        public SurfaceRegistry(LODChunkRegistry<SurfaceChunk> chunkRegistry)
        {
            _chunkRegistry = chunkRegistry;
        }

        #region Registration Logic
        public bool TryRegisterSurface(GameObject obj, bool triggerImmediateBake = false)
        {
            if (obj == null) return false;

            int id = obj.GetInstanceID();

            if (!TryCreateRelativeState(obj, out SpatialObjectState newState))
                return false;

            bool changed = false;

            if (_registry.TryGetValue(id, out SpatialObjectState oldState))
            {
                if (!oldState.Equals(newState))
                {
                    MarkAreaDirty(oldState.anchorBounds);
                    _registry[id] = newState;
                    changed = true;
                }
            }
            else
            {
                _registry.Add(id, newState);
                changed = true;
            }

            if (changed)
            {
                MarkAreaDirty(newState.anchorBounds);
                if (triggerImmediateBake) ApplyChanges();
            }

            return true;
        }

        public bool UnregisterSurface(int id, bool triggerImmediateBake = false)
        {
            if (_registry.TryGetValue(id, out SpatialObjectState state))
            {
                MarkAreaDirty(state.anchorBounds);
                _registry.Remove(id);

                if (triggerImmediateBake)
                    ApplyChanges();

                return true;
            }
            return false;
        }

        public IEnumerable<int> GetAllIds() => _registry.Keys;
        #endregion

        #region Bake Logic
        public void ApplyChanges()
        {
            // 1. English: Sync with chunks that flagged themselves (e.g. LOD changed).
            foreach (var chunk in _chunkRegistry.AllEntries)
            {
                // English: Using the Vector2Int convenience property of the chunk.
                if (chunk != null && chunk.IsDirty)
                    _dirtyChunks.Add(chunk.GridKey2D);
            }

            if (_dirtyChunks.Count == 0) return;

            // 2. English: Bake all unique XZ-coordinates.
            foreach (Vector2Int key in _dirtyChunks)
            {
                BakeChunk(key);
            }

            _dirtyChunks.Clear();
        }

        private void BakeChunk(Vector2Int key)
        {
            // English: ChunkRegistry handles the conversion from Vector2Int key to world position internally.
            SurfaceChunk chunk = _chunkRegistry.GetOrCreateChunk(key);
            int resolution = GetResolutionForLOD(chunk.CurrentLOD);

            List<SpatialObjectState> relevantStates = new List<SpatialObjectState>();
            foreach (var state in _registry.Values)
            {
                if (IsObjectInChunk(state, key))
                    relevantStates.Add(state);
            }

            // HeightmapBaker.Render(chunk, relevantStates, resolution);
            chunk.ClearDirty();
        }
        #endregion

        #region Helpers (XZ-Grid & Mesh Aware)
        /// <summary>
        /// English: Marks all XZ-grid cells overlapping the bounds as dirty.
        /// </summary>
        private void MarkAreaDirty(Bounds relativeBounds)
        {
            Vector2Int min = _chunkRegistry.WorldToGrid(relativeBounds.min);
            Vector2Int max = WorldToGridRelative(relativeBounds.max);

            for (int x = min.x; x <= max.x; x++)
            {
                for (int z = min.y; z <= max.y; z++) // Note: Vector2Int.y maps to World Z
                {
                    _dirtyChunks.Add(new Vector2Int(x, z));
                }
            }
        }

        private bool IsObjectInChunk(SpatialObjectState state, Vector2Int key)
        {
            float half = ChunkSize * 0.5f;
            // English: Reconstruct center in anchor-relative space.
            Vector3 relCenter = new Vector3(key.x * ChunkSize + half, 0, key.y * ChunkSize + half);

            // English: Huge Y-extent to capture any object height for projection.
            Bounds chunkRelBounds = new Bounds(relCenter, new Vector3(ChunkSize, 4000f, ChunkSize));

            return state.anchorBounds.Intersects(chunkRelBounds);
        }

        /// <summary>
        /// Validates and prepares the spatial state of a GameObject relative to the current world anchor.
        /// Only succeeds if the object provides valid mesh geometry for heightmap projection.
        /// </summary>
        /// <param name="obj">The target GameObject to register.</param>
        /// <param name="state">The resulting relative state containing bounds, matrix, and mesh reference.</param>
        /// <returns>True if the object is a valid surface candidate; otherwise false.</returns>
        private bool TryCreateRelativeState(GameObject obj, out SpatialObjectState state)
        {
            state = default;

            if (!TryGetMesh(obj, out Mesh mesh))
                return false;

            if (!TryGetMeshWorldBounds(obj, out Bounds worldBounds))
                return false;

            state = SpatialObjectState.Create(
                worldBounds,
                obj.transform.localToWorldMatrix,
                CurrentAnchor,
                mesh
            );

            return true;
        }

        /// <summary>
        /// Attempts to retrieve a valid Mesh from either graphics (MeshFilter) or physics (MeshCollider).
        /// </summary>
        private bool TryGetMesh(GameObject obj, out Mesh mesh)
        {
            mesh = null;

            if (obj.TryGetComponent<MeshFilter>(out var filter) && filter.sharedMesh != null)
            {
                mesh = filter.sharedMesh;
                return true;
            }

            if (obj.TryGetComponent<MeshCollider>(out var meshCol) && meshCol.sharedMesh != null)
            {
                mesh = meshCol.sharedMesh;
                return true;
            }

            return false;
        }

        /// <summary>
        /// Retrieves the world-space bounds from the Renderer or MeshCollider.
        /// </summary>
        private bool TryGetMeshWorldBounds(GameObject obj, out Bounds bounds)
        {
            // Priority: Renderer (actual visual volume) > MeshCollider (physical volume).
            if (obj.TryGetComponent<Renderer>(out var r))
            {
                bounds = r.bounds;
                return true;
            }

            if (obj.TryGetComponent<MeshCollider>(out var mc))
            {
                bounds = mc.bounds;
                return true;
            }

            bounds = default;
            return false;
        }

        #endregion
    }
}