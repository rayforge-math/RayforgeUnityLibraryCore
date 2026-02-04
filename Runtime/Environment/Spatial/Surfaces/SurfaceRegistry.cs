using Rayforge.Core.Environment.Spatial;
using Rayforge.Core.Environment.Spatial.Surfaces;
using System.Collections.Generic;
using UnityEngine;

namespace Rayforge.Core.Environment.Spatial.Surface
{
    /// <summary>
    /// Manages the registration and state tracking of spatial objects for heightmap projection.
    /// Uses a spatial hash (buckets) to avoid O(N) searches during baking.
    /// </summary>
    public class SurfaceRegistry
    {
        #region Fields
        /// <summary> Primary storage: InstanceID -> State. </summary>
        private readonly Dictionary<int, SpatialObjectState> _registry = new Dictionary<int, SpatialObjectState>();

        /// <summary> 
        /// Spatial Index: ChunkKey -> Set of InstanceIDs.
        /// Allows O(1) access to objects relevant to a specific chunk.
        /// </summary>
        private readonly Dictionary<Vector2Int, HashSet<int>> _spatialBuckets = new Dictionary<Vector2Int, HashSet<int>>();

        /// <summary> Collection of chunk coordinates that require a re-bake. </summary>
        private readonly HashSet<Vector2Int> _dirtyChunks = new HashSet<Vector2Int>();

        /// <summary> Reference to the spatial grid provider. </summary>
        private readonly LODChunkRegistry<SurfaceChunk> _chunkRegistry;

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

            // Create the relative state (Assumes this method exists in your context).
            if (!TryCreateRelativeState(obj, out SpatialObjectState newState))
                return false;

            bool changed = false;

            if (_registry.TryGetValue(id, out SpatialObjectState oldState))
            {
                if (!oldState.Equals(newState))
                {
                    // Position or mesh changed. Clear old buckets, add to new ones.
                    RemoveFromBuckets(id, oldState.anchorBounds);
                    MarkAreaDirty(oldState.anchorBounds);

                    _registry[id] = newState;
                    AddToBuckets(id, newState.anchorBounds);
                    changed = true;
                }
            }
            else
            {
                // New object registration.
                _registry.Add(id, newState);
                AddToBuckets(id, newState.anchorBounds);
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
                RemoveFromBuckets(id, state.anchorBounds);
                MarkAreaDirty(state.anchorBounds);
                _registry.Remove(id);

                if (triggerImmediateBake) ApplyChanges();
                return true;
            }
            return false;
        }

        #endregion

        #region Bucket Management

        private void AddToBuckets(int id, Bounds bounds)
        {
            // Use Registry to find all affected keys.
            foreach (var key3D in _chunkRegistry.GetKeysInBounds(bounds))
            {
                Vector2Int key2D = new Vector2Int(key3D.x, key3D.z);
                if (!_spatialBuckets.TryGetValue(key2D, out var bucket))
                {
                    bucket = new HashSet<int>();
                    _spatialBuckets[key2D] = bucket;
                }
                bucket.Add(id);
            }
        }

        private void RemoveFromBuckets(int id, Bounds bounds)
        {
            foreach (var key3D in _chunkRegistry.GetKeysInBounds(bounds))
            {
                Vector2Int key2D = new Vector2Int(key3D.x, key3D.z);
                if (_spatialBuckets.TryGetValue(key2D, out var bucket))
                {
                    bucket.Remove(id);
                    if (bucket.Count == 0) _spatialBuckets.Remove(key2D);
                }
            }
        }

        #endregion

        #region Bake Logic

        public void ApplyChanges()
        {
            // 1. Collect chunks that requested a bake (e.g. via LOD change).
            foreach (var chunk in _chunkRegistry.AllEntries)
            {
                if (chunk != null && chunk.IsDirty)
                    _dirtyChunks.Add(chunk.GridKey2D);
            }

            if (_dirtyChunks.Count == 0) return;

            // 2. Process all dirty coordinates.
            foreach (Vector2Int key in _dirtyChunks)
            {
                BakeChunk(key);
            }

            _dirtyChunks.Clear();
        }

        private void BakeChunk(Vector2Int key)
        {
            SurfaceChunk chunk = _chunkRegistry.GetOrCreateChunk(key);
            // Resolution logic depends on your specific LOD implementation.
            int resolution = 512; // Example fallback

            List<SpatialObjectState> relevantStates = new List<SpatialObjectState>();

            // 3. Instead of checking ALL objects, we only look at this chunk's bucket.
            if (_spatialBuckets.TryGetValue(key, out HashSet<int> objectIds))
            {
                foreach (int id in objectIds)
                {
                    if (_registry.TryGetValue(id, out var state))
                        relevantStates.Add(state);
                }
            }

            // Ready for rendering.
            // HeightmapBaker.Render(chunk, relevantStates, resolution);

            chunk.ClearDirty();
        }

        private void MarkAreaDirty(Bounds relativeBounds)
        {
            foreach (var key3D in _chunkRegistry.GetKeysInBounds(relativeBounds))
            {
                _dirtyChunks.Add(new Vector2Int(key3D.x, key3D.z));
            }
        }
        #endregion

        #region Helpers

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