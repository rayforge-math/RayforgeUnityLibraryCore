using Rayforge.Core.Environment.Abstractions;
using Rayforge.Core.Diagnostics;
using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;
using System.Runtime.CompilerServices;

namespace Rayforge.Core.Environment.Spatial
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

        /// <summary>
        /// Logs a message to the custom DebugOutput if logging is enabled.
        /// This call is completely stripped from non-editor builds.
        /// </summary>
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
        /// Provides an enumerable of all MeshRenderer within a specific spatial cell.
        /// </summary>
        public IEnumerable<MeshRenderer> GetRenderersInCell(Vector3Int key)
        {
            if (!_spatialBuckets.TryGetValue(key, out var ids))
                yield break;

            foreach (int id in ids)
            {
                if (_registry.TryGetValue(id, out var state) && state.renderer != null)
                {
                    yield return state.renderer;
                }
            }
        }

        /// <summary>
        /// Provides an enumerable of all Terrains within a specific spatial cell.
        /// </summary>
        public IEnumerable<Terrain> GetTerrainsInCell(Vector3Int key)
        {
            if (!_spatialBuckets.TryGetValue(key, out var ids))
                yield break;

            foreach (int id in ids)
            {
                if (_registry.TryGetValue(id, out var state) && state.terrain != null)
                {
                    yield return state.terrain;
                }
            }
        }

        /// <summary>
        /// Checks if a specific cell contains any registered objects.
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
            if (!IsInitialized) return false;

            if (obj.TryGetComponent<Terrain>(out var terrain))
            {
                if (terrain.terrainData == null) return false;
                state = SpatialObjectState.Create(_gridProvider.Anchor, terrain);
                return true;
            }

            if (obj.TryGetComponent<MeshRenderer>(out var renderer))
            {
                if (!obj.TryGetComponent<MeshFilter>(out var filter) || filter.sharedMesh == null)
                    return false;

                state = SpatialObjectState.Create(_gridProvider.Anchor, renderer);
                return true;
            }

            return false;
        }

        #endregion
    }
}