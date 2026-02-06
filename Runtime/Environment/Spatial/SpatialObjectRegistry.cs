using Rayforge.Core.Environment.Abstractions;
using Rayforge.Core.Diagnostics;
using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;
using System.Runtime.CompilerServices;

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
        private void LogDebug(string message, [CallerLineNumber] int line = 0)
        {
            DebugOutput.Log(message, showDebugLogs, lineNumber: line);
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

        /// <summary>
        /// Completely wipes all registered objects and spatial data.
        /// Use this before a full scene rescan to ensure no "ghost" objects remain.
        /// </summary>
        public void Clear()
        {
            LogDebug("Registry: Performing full clear of all objects and buckets.");

            _registry.Clear();
            _spatialBuckets.Clear();
            _dirtyBuckets.Clear();
        }

        #endregion

        #region Registration Logic

        /// <summary>
        /// Registers or updates a GameObject in the spatial registry.
        /// </summary>
        /// <returns>
        /// True if the registry was actually modified (new object or spatial change). 
        /// False if the object is already up-to-date or registration failed.
        /// </returns>
        public bool TryRegister(GameObject obj)
        {
            if (obj == null || !IsInitialized) return false;

            int id = obj.GetInstanceID();
            if (!TryCreateRelativeState(obj, out SpatialObjectState newState))
                return false;

            if (_registry.TryGetValue(id, out SpatialObjectState oldState))
            {
                if (oldState.Equals(newState)) return false;

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

        /// <summary>
        /// Removes an object from the registry and its spatial buckets.
        /// </summary>
        /// <returns>True if the object was found and removed, false otherwise.</returns>
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

        /// <summary>
        /// Checks if a specific cell contains any registered objects.
        /// Useful for the Manager to decide if a chunk shell is needed.
        /// </summary>
        public bool HasDataInBucket(Vector3Int key)
        {
            return _spatialBuckets.ContainsKey(key);
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

            System.Text.StringBuilder sb = showDebugLogs ? new System.Text.StringBuilder() : null;
            int affectedCount = 0;

            foreach (Vector3Int key in _gridProvider.GetKeysInRelativeBounds(bounds))
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

                if (showDebugLogs)
                {
                    if (affectedCount > 0) sb.Append(", ");
                    sb.Append(key.ToString());
                    affectedCount++;
                }
            }

            if (showDebugLogs)
            {
                string op = add ? "Mapped to" : "Removed from";
                LogDebug($"{op} {affectedCount} buckets: [{sb}] (ID: {id})");
            }
        }

        #endregion

        #region Helpers

        private bool TryCreateRelativeState(GameObject obj, out SpatialObjectState state)
        {
            state = default;
            if (!IsInitialized)
            {
                LogDebug($"Registry: Failed to create state for {obj.name}. Registry not initialized.");
                return false;
            }

            TryGetMesh(obj, out Mesh mesh);

            bool isTerrain = obj.TryGetComponent<Terrain>(out var terrain);
            if (isTerrain)
            {
                bool hasCollider = obj.TryGetComponent<TerrainCollider>(out var tc) && tc.enabled;
                if (terrain.terrainData == null || !hasCollider)
                {
                    LogDebug($"Registry: {obj.name} has Terrain component but missing data or enabled collider.");
                    isTerrain = false;
                    terrain = null;
                }
            }

            if (mesh == null && !isTerrain)
            {
                LogDebug($"Registry: {obj.name} ignored. No valid Mesh or Terrain found.");
                return false;
            }

            if (!TryGetSpatialBounds(obj, out Bounds worldBounds))
            {
                LogDebug($"Registry: {obj.name} ignored. Could not calculate world bounds.");
                return false;
            }

            state = SpatialObjectState.Create(
                worldBounds,
                obj.transform.localToWorldMatrix,
                _gridProvider.Anchor,
                mesh,
                terrain
            );

            if (showDebugLogs)
            {
                Vector3 localPos = worldBounds.center - _gridProvider.Anchor;
                LogDebug($"Registry: Created state for '{obj.name}'\n" +
                         $"  World Center: {worldBounds.center}\n" +
                         $"  Relative to Anchor: {localPos}\n" +
                         $"  Bounds Size: {worldBounds.size}");
            }

            return true;
        }

        private bool TryGetMesh(GameObject obj, out Mesh mesh)
        {
            mesh = null;

            if (obj.TryGetComponent<MeshFilter>(out var filter) && filter.sharedMesh != null)
            {
                mesh = filter.sharedMesh;
                return true;
            }

            if (obj.TryGetComponent<SkinnedMeshRenderer>(out var skinned) && skinned.sharedMesh != null)
            {
                mesh = skinned.sharedMesh;
                return true;
            }

            if (obj.TryGetComponent<MeshCollider>(out var meshCol) && meshCol.sharedMesh != null)
            {
                mesh = meshCol.sharedMesh;
                return true;
            }

            return false;
        }

        private bool TryGetSpatialBounds(GameObject obj, out Bounds bounds)
        {
            if (obj.TryGetComponent<Renderer>(out var r))
            {
                bounds = r.bounds;
                return true;
            }

            if (obj.TryGetComponent<Terrain>(out var terrain) && terrain.terrainData != null)
            {
                Vector3 size = terrain.terrainData.size;
                bounds = new Bounds(obj.transform.position + size * 0.5f, size);
                return true;
            }

            bounds = default;
            return false;
        }

        #endregion
    }
}