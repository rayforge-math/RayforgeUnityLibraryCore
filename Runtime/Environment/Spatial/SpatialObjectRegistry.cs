using Rayforge.Core.Environment.Abstractions;
using Rayforge.Core.Environment.Spatial.Surfaces;
using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;

namespace Rayforge.Core.Environment.Spatial.Surface
{
    /// <summary>
    /// Manages the registration and spatial partitioning of objects.
    /// Acts as a pure spatial hash, mapping object states to grid cells.
    /// </summary>
    public class SpatialObjectRegistry
    {
        #region Fields
        /// <summary> Primary storage: InstanceID -> State. </summary>
        private readonly Dictionary<int, SpatialObjectState> _registry = new Dictionary<int, SpatialObjectState>();

        /// <summary> 
        /// Spatial Index: CellKey (2D) -> Set of InstanceIDs. 
        /// </summary>
        private readonly Dictionary<Vector3Int, HashSet<int>> _spatialBuckets = new Dictionary<Vector3Int, HashSet<int>>();

        private readonly HashSet<Vector3Int> _dirtyBuckets = new HashSet<Vector3Int>();

        /// <summary> The provider used to calculate grid keys from world bounds. </summary>
        private ISpatialGridProvider _gridProvider;

        public bool showDebugLogs = false;

        /// <summary> Helper to check if the registry is ready for spatial operations. </summary>
        public bool IsInitialized => _gridProvider != null;

        public Dictionary<int, SpatialObjectState>.KeyCollection GetAllIds() => _registry.Keys;
        #endregion

        #region Debug Helper
        [Conditional("UNITY_EDITOR")]
        private void LogDebug(string message)
        {
            if (showDebugLogs)
            {
                UnityEngine.Debug.Log($"<color=#4FC3F7>[ChunkObjectRegistry]</color> {message}");
            }
        }
        #endregion

        #region Lifecycle

        /// <summary>
        /// Connects the registry to a grid provider and rebuilds buckets.
        /// </summary>
        public void Initialize(ISpatialGridProvider gridProvider)
        {
            LogDebug("Initializing with Grid Provider. Remapping all objects.");
            _gridProvider = gridProvider;

            _spatialBuckets.Clear();
            _dirtyBuckets.Clear();

            if (_gridProvider == null) return;

            foreach (var entry in _registry)
            {
                UpdateBucketsForObject(entry.Key, entry.Value.anchorBounds, true);
            }
        }

        #endregion

        #region Registration Logic

        public bool TryRegister(GameObject obj)
        {
            if (obj == null || !IsInitialized) return false;

            int id = obj.GetInstanceID();
            if (!TryCreateRelativeState(obj, out SpatialObjectState newState))
                return false;

            if (_registry.TryGetValue(id, out SpatialObjectState oldState))
            {
                if (oldState.Equals(newState)) return true;

                LogDebug($"Updating object: {obj.name}. Spatial state changed.");

                UpdateBucketsForObject(id, oldState.anchorBounds, false);
                _registry[id] = newState;
                UpdateBucketsForObject(id, newState.anchorBounds, true);
            }
            else
            {
                LogDebug($"Registering new object: {obj.name}");
                _registry.Add(id, newState);
                UpdateBucketsForObject(id, newState.anchorBounds, true);
            }

            return true;
        }

        public bool Unregister(int id)
        {
            if (_registry.TryGetValue(id, out SpatialObjectState state))
            {
                LogDebug($"Removing object: {id}");
                UpdateBucketsForObject(id, state.anchorBounds, false);
                _registry.Remove(id);
                return true;
            }
            return false;
        }

        #endregion

        #region Dirty State Management

        /// <summary>
        /// Provides an enumerator over dirty buckets. 
        /// IMPORTANT: Call ClearDirtyBuckets() after processing to reset the state.
        /// </summary>
        public IEnumerable<Vector3Int> GetDirtyBuckets()
        {
            foreach (var key in _dirtyBuckets)
            {
                yield return key;
            }
        }

        public void ClearDirtyBuckets()
        {
            _dirtyBuckets.Clear();
        }

        /// <summary>
        /// Manually mark a specific cell as dirty.
        /// </summary>
        public void MarkDirty(Vector3Int key) => _dirtyBuckets.Add(key);

        #endregion

        #region Spatial Queries

        /// <summary>
        /// Returns all object states that are registered within the given cell.
        /// Used by the Manager to gather bake data for a specific chunk.
        /// </summary>
        public List<SpatialObjectState> GetObjectsInCell(Vector3Int key)
        {
            List<SpatialObjectState> result = new List<SpatialObjectState>();
            if (_spatialBuckets.TryGetValue(key, out var ids))
            {
                foreach (int id in ids)
                {
                    if (_registry.TryGetValue(id, out var state))
                        result.Add(state);
                }
            }
            return result;
        }

        #endregion

        #region Bucket Management

        /// <summary>
        /// Internal helper to add or remove an object from the grid buckets.
        /// It asks the provider: "Which keys are touched by these bounds?"
        /// </summary>
        private void UpdateBucketsForObject(int id, Bounds bounds, bool add)
        {
            if (!IsInitialized) return;

            foreach (Vector3Int key in _gridProvider.GetKeysInBounds(bounds))
            {
                _dirtyBuckets.Add(key);

                if (add)
                {
                    if (!_spatialBuckets.TryGetValue(key, out var bucket))
                    {
                        bucket = new HashSet<int>();
                        _spatialBuckets[key] = bucket;
                    }
                    bucket.Add(id);
                }
                else
                {
                    if (_spatialBuckets.TryGetValue(key, out var bucket))
                    {
                        bucket.Remove(id);
                        if (bucket.Count == 0) _spatialBuckets.Remove(key);
                    }
                }
            }
        }

        #endregion

        #region Helpers

        private bool TryCreateRelativeState(GameObject obj, out SpatialObjectState state)
        {
            state = default;
            if (!IsInitialized) return false;

            bool hasMesh = TryGetMesh(obj, out Mesh mesh);
            bool isTerrain = obj.TryGetComponent<Terrain>(out var terrain);

            if (!hasMesh && !isTerrain) return false;

            if (!TryGetMeshWorldBounds(obj, out Bounds worldBounds)) return false;

            // Use the Anchor from our Interface
            state = SpatialObjectState.Create(
                worldBounds,
                obj.transform.localToWorldMatrix,
                _gridProvider.Anchor,
                mesh,
                terrain
            );

            return true;
        }

        private bool TryGetMesh(GameObject obj, out Mesh mesh)
        {
            mesh = null;

            if (obj.TryGetComponent<MeshFilter>(out var filter))
            {
                if (filter.sharedMesh != null)
                {
                    mesh = filter.sharedMesh;
                    return true;
                }
                LogDebug($"MeshFilter found on {obj.name}, but sharedMesh is null.");
            }

            if (obj.TryGetComponent<MeshCollider>(out var meshCol))
            {
                if (meshCol.sharedMesh != null)
                {
                    mesh = meshCol.sharedMesh;
                    return true;
                }
                LogDebug($"MeshCollider found on {obj.name}, but sharedMesh is null.");
            }

            return false;
        }

        private bool TryGetMeshWorldBounds(GameObject obj, out Bounds bounds)
        {
            if (obj.TryGetComponent<Renderer>(out var r))
            {
                bounds = r.bounds;
                return true;
            }

            if (obj.TryGetComponent<Terrain>(out var terrain))
            {
                Vector3 size = terrain.terrainData.size;
                Vector3 worldPos = obj.transform.position;
                bounds = new Bounds(worldPos + size * 0.5f, size);
                return true;
            }

            if (obj.TryGetComponent<Collider>(out var c))
            {
                bounds = c.bounds;
                return true;
            }

            bounds = default;
            return false;
        }

        #endregion
    }
}