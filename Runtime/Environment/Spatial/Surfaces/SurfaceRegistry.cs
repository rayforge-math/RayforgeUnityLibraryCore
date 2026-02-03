using System.Collections.Generic;
using UnityEngine;

namespace Rayforge.Core.Environment.Spatial.Surfaces
{
    /// <summary>
    /// Manages the registration and state tracking of spatial objects.
    /// Supports deferred baking to optimize performance during batch updates.
    /// </summary>
    public class SurfaceRegistry
    {
        private readonly Dictionary<int, SpatialObjectState> _registry = new Dictionary<int, SpatialObjectState>();
        private readonly HashSet<Vector2Int> _dirtyChunks = new HashSet<Vector2Int>();

        /// <summary>
        /// Attempts to register or update a GameObject.
        /// </summary>
        /// <param name="obj">The GameObject to track.</param>
        /// <param name="triggerImmediateBake">If true, affected chunks are processed immediately.</param>
        /// <returns>True if the object was successfully processed.</returns>
        public bool TryRegisterSurface(GameObject obj, bool triggerImmediateBake = false)
        {
            if (obj == null) return false;

            int id = obj.GetInstanceID();
            if (!TryCreateState(obj, out SpatialObjectState newState))
            {
                return false;
            }

            bool changed = false;

            if (_registry.TryGetValue(id, out SpatialObjectState oldState))
            {
                if (oldState != newState)
                {
                    // Mark old area as dirty (something moved away)
                    MarkAreaDirty(oldState.worldBounds);
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
                // Mark new area as dirty
                MarkAreaDirty(newState.worldBounds);

                if (triggerImmediateBake)
                {
                    ApplyChanges();
                }
            }

            return true;
        }

        /// <summary>
        /// Removes an object from the registry.
        /// </summary>
        /// <param name="id">Unity InstanceID.</param>
        /// <param name="triggerImmediateBake">If true, triggers a bake for the cleared area immediately.</param>
        /// <returns>True if the object was found and removed.</returns>
        public bool UnregisterSurface(int id, bool triggerImmediateBake = false)
        {
            if (_registry.TryGetValue(id, out SpatialObjectState state))
            {
                MarkAreaDirty(state.worldBounds);
                _registry.Remove(id);

                if (triggerImmediateBake)
                {
                    ApplyChanges();
                }
                return true;
            }
            return false;
        }

        /// <summary>
        /// Triggers the baking process for all chunks that have been marked as dirty.
        /// </summary>
        public void ApplyChanges()
        {
            if (_dirtyChunks.Count == 0) return;

            foreach (Vector2Int coord in _dirtyChunks)
            {
                // Here: Tell the ChunkRegistry or the specific Chunk at 'coord' 
                // to re-render its heightmap using the states overlapping its area.
                BakeChunk(coord);
            }

            _dirtyChunks.Clear();
            Debug.Log("[SurfaceRegistry] Batch bake completed.");
        }

        /// <summary>
        /// Internal helper to calculate which chunk coordinates are affected by a bounds volume.
        /// </summary>
        private void MarkAreaDirty(Bounds bounds)
        {
            // Placeholder: Convert world bounds to chunk coordinates
            // Example:
            // Vector2Int min = SpatialGridUtility.WorldToChunk(bounds.min);
            // Vector2Int max = SpatialGridUtility.WorldToChunk(bounds.max);
            // for (int x = min.x; x <= max.x; x++) 
            //    for (int y = min.y; y <= max.y; y++) 
            //        _dirtyChunks.Add(new Vector2Int(x, y));
        }

        private void BakeChunk(Vector2Int coord)
        {
            // Implementation follows: This will collect all WorldObjectStates 
            // for this chunk and render them into a RenderTexture.
        }

        public IEnumerable<int> GetAllIds() => _registry.Keys;

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

            bounds = new Bounds(obj.transform.position, Vector3.one);
            return false;
        }

        private bool TryCreateState(GameObject obj, out SpatialObjectState state)
        {
            state = default;
            if (!TryGetWorldBounds(obj, out Bounds worldBounds)) return false;

            MeshFilter filter = obj.GetComponent<MeshFilter>();
            Mesh mesh = (filter != null) ? filter.sharedMesh : null;

            state = new SpatialObjectState
            {
                worldBounds = worldBounds,
                localToWorld = obj.transform.localToWorldMatrix,
                mesh = mesh,
                subMeshIndex = 0,
                geometryHash = (mesh != null) ? mesh.GetInstanceID() : 0
            };

            return true;
        }
    }
}
