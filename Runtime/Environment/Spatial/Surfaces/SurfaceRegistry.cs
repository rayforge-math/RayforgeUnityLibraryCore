using Rayforge.Core.Environment.Spatial;
using System.Collections.Generic;
using UnityEngine;

namespace Rayforge.Core.Environment.Spatial.Surface
{
    /// <summary>
    /// Manages the registration and state tracking of spatial objects.
    /// Uses anchor-relative coordinates to remain stable during Origin Shifts by synchronizing with a ChunkRegistry.
    /// </summary>
    public class SurfaceRegistry
    {
        /// <summary>
        /// Internal storage for all tracked object states, indexed by their Unity InstanceID.
        /// </summary>
        private readonly Dictionary<int, SpatialObjectState> _registry = new Dictionary<int, SpatialObjectState>();

        /// <summary>
        /// Collection of chunk coordinates that require a re-bake due to object changes.
        /// </summary>
        private readonly HashSet<Vector2Int> _dirtyChunks = new HashSet<Vector2Int>();

        /// <summary>
        /// Reference to the spatial grid provider.
        /// </summary>
        private readonly ChunkRegistry<> _chunkRegistry;

        /// <summary>
        /// Gets the current reference anchor from the ChunkRegistry.
        /// Used to calculate stable relative positions.
        /// </summary>
        private Vector3 CurrentAnchor => _chunkRegistry.Anchor;

        /// <summary>
        /// Gets the world-scale size of a single chunk from the ChunkRegistry.
        /// </summary>
        private float ChunkSize => (float)_chunkRegistry.chunk;

        /// <summary>
        /// Initializes a new instance of the SurfaceRegistry.
        /// </summary>
        /// <param name="chunkRegistry">The grid registry used for anchor and coordinate mapping.</param>
        public SurfaceRegistry(ChunkRegistry<> chunkRegistry)
        {
            _chunkRegistry = chunkRegistry;
        }

        /// <summary>
        /// Attempts to register or update a GameObject in the relative spatial grid.
        /// </summary>
        /// <param name="obj">The GameObject to track.</param>
        /// <param name="triggerImmediateBake">If true, ApplyChanges() is called immediately if the state changed.</param>
        /// <returns>True if the object was successfully processed and meets validation criteria.</returns>
        public bool TryRegisterSurface(GameObject obj, bool triggerImmediateBake = false)
        {
            if (obj == null) return false;

            int id = obj.GetInstanceID();

            // Create state relative to the ChunkRegistry's anchor to ensure Origin Shift immunity.
            if (!TryCreateRelativeState(obj, out SpatialObjectState newState))
                return false;

            bool changed = false;

            if (_registry.TryGetValue(id, out SpatialObjectState oldState))
            {
                if (oldState != newState)
                {
                    // Object moved or changed relative to the grid. 
                    // Mark the old area as dirty because the object is no longer there.
                    MarkAreaDirty(oldState.anchorBounds);
                    _registry[id] = newState;
                    changed = true;
                }
            }
            else
            {
                // New object discovered.
                _registry.Add(id, newState);
                changed = true;
            }

            if (changed)
            {
                // Mark the new area as dirty to include the object in the next bake.
                MarkAreaDirty(newState.anchorBounds);

                if (triggerImmediateBake)
                    ApplyChanges();
            }

            return true;
        }

        /// <summary>
        /// Removes an object from the registry and marks its area as dirty.
        /// </summary>
        /// <param name="id">The Unity InstanceID of the object.</param>
        /// <param name="triggerImmediateBake">If true, processes dirty chunks immediately.</param>
        /// <returns>True if the object was found and successfully removed.</returns>
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

        /// <summary>
        /// Iterates through all dirty chunks and triggers their bake process.
        /// Clears the dirty queue upon completion.
        /// </summary>
        public void ApplyChanges()
        {
            if (_dirtyChunks.Count == 0) return;

            foreach (Vector2Int coord in _dirtyChunks)
            {
                // In an extended version, resolution/LOD data would be 
                // determined here by the Manager before calling the bake.
                BakeChunk(coord);
            }

            _dirtyChunks.Clear();
        }

        /// <summary>
        /// Identifies all grid coordinates overlapping the provided anchor-relative bounds and marks them as dirty.
        /// </summary>
        /// <param name="relativeBounds">Bounds of the object in anchor-space.</param>
        private void MarkAreaDirty(Bounds relativeBounds)
        {
            int minX = Mathf.FloorToInt(relativeBounds.min.x / ChunkSize);
            int maxX = Mathf.FloorToInt(relativeBounds.max.x / ChunkSize);
            int minZ = Mathf.FloorToInt(relativeBounds.min.z / ChunkSize);
            int maxZ = Mathf.FloorToInt(relativeBounds.max.z / ChunkSize);

            for (int x = minX; x <= maxX; x++)
            {
                for (int z = minZ; z <= maxZ; z++)
                {
                    _dirtyChunks.Add(new Vector2Int(x, z));
                }
            }
        }

        /// <summary>
        /// Executes the bake logic for a specific chunk coordinate.
        /// </summary>
        /// <param name="coord">The (x, z) grid coordinate of the chunk.</param>
        private void BakeChunk(Vector2Int coord)
        {
            // 1. Calculate the spatial volume of the chunk in anchor-space.
            Vector3 min = new Vector3(coord.x * ChunkSize, -1000, coord.y * ChunkSize);
            Vector3 max = new Vector3((coord.x + 1) * ChunkSize, 1000, (coord.y + 1) * ChunkSize);
            Bounds chunkBounds = new Bounds();
            chunkBounds.SetMinMax(min, max);

            // 2. Filter registry for objects that physically overlap this chunk.
            // For production: Consider a Spatial Hash if the registry grows to thousands of objects.
            List<SpatialObjectState> relevantStates = new List<SpatialObjectState>();
            foreach (var state in _registry.Values)
            {
                if (state.anchorBounds.Intersects(chunkBounds))
                {
                    relevantStates.Add(state);
                }
            }

            // 3. Render the states into the chunk's RenderTexture.
            // HeightmapBaker.Render(coord, relevantStates);
        }

        /// <summary>
        /// Extracts world data from a GameObject and uses the state factory to create a relative state.
        /// </summary>
        private bool TryCreateRelativeState(GameObject obj, out SpatialObjectState state)
        {
            state = default;

            if (!TryGetWorldBounds(obj, out Bounds worldBounds)) return false;

            MeshFilter filter = obj.GetComponent<MeshFilter>();
            Mesh mesh = (filter != null) ? filter.sharedMesh : null;

            state = SpatialObjectState.Create(
                worldBounds,
                obj.transform.localToWorldMatrix,
                CurrentAnchor,
                mesh
            );

            return true;
        }

        /// <summary>
        /// Extracts the best available world-space bounds from a GameObject (Renderer, Collider, or Position).
        /// </summary>
        /// <param name="obj">The GameObject to inspect.</param>
        /// <param name="bounds">The resulting world-space bounds.</param>
        /// <returns>True if a valid bounding volume was found.</returns>
        private bool TryGetWorldBounds(GameObject obj, out Bounds bounds)
        {
            if (obj.TryGetComponent<Renderer>(out var renderer))
            {
                bounds = renderer.bounds;
                return true;
            }
            if (obj.TryGetComponent<Collider>(out var collider))
            {
                bounds = collider.bounds;
                return true;
            }
            // Fallback for objects without physical representation.
            bounds = new Bounds(obj.transform.position, Vector3.one);
            return false;
        }

        /// <summary>
        /// Provides an iterator over all currently registered Unity InstanceIDs.
        /// </summary>
        /// <returns>A collection of IDs used for synchronization or cleanup.</returns>
        public IEnumerable<int> GetAllIds() => _registry.Keys;
    }
}