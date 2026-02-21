using Rayforge.Core.Collections.Abstractions;
using Rayforge.Core.Collections.Iterator;
using Rayforge.Core.Diagnostics;
using Rayforge.Core.Environment.Abstractions;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using UnityEngine;

namespace Rayforge.Core.Environment.Spatial
{
    /// <summary>
    /// Manages registration and spatial partitioning.
    /// Uses a two-step lookup: Cell -> IDs -> Components for maximum data integrity.
    /// </summary>
    public class SpatialObjectRegistry :
        ISpatialCollection,
        IIterable<MeshRenderer, SpatialIteratorState<Vector3Int, HashSet<int>.Enumerator>>,
        IIterable<Terrain, SpatialIteratorState<Vector3Int, HashSet<int>.Enumerator>>
    {
        #region Fields

        /// <summary> Primary storage: InstanceID -> State for MeshRenderers. </summary>
        private readonly Dictionary<int, SpatialState<MeshRenderer>> _meshRegistry = new();

        /// <summary> Primary storage: InstanceID -> State for Terrains. </summary>
        private readonly Dictionary<int, SpatialState<Terrain>> _terrainRegistry = new();

        /// <summary> Spatial Index: CellKey -> Set of Mesh InstanceIDs. </summary>
        private readonly Dictionary<Vector3Int, HashSet<int>> _meshBuckets = new();

        /// <summary> Spatial Index: CellKey -> Set of Terrain InstanceIDs. </summary>
        private readonly Dictionary<Vector3Int, HashSet<int>> _terrainBuckets = new();

        private readonly HashSet<Vector3Int> _dirtyBuckets = new();
        private ISpatialGridProvider<Vector3Int> _gridProvider;

        public bool showDebugLogs = false;
        public bool IsInitialized => _gridProvider != null;

        #endregion

        #region Lifecycle

        public void Initialize(ISpatialGridProvider<Vector3Int> gridProvider)
        {
            LogDebug("Initializing Registry. Remapping ID-based buckets.");
            _gridProvider = gridProvider;

            _meshBuckets.Clear();
            _terrainBuckets.Clear();
            _dirtyBuckets.Clear();

            if (_gridProvider == null) return;

            foreach (var entry in _meshRegistry)
                UpdateBuckets(_meshBuckets, entry.Key, entry.Value.anchorBounds, true);

            foreach (var entry in _terrainRegistry)
                UpdateBuckets(_terrainBuckets, entry.Key, entry.Value.anchorBounds, true);
        }

        public void Clear()
        {
            _meshRegistry.Clear();
            _terrainRegistry.Clear();
            _meshBuckets.Clear();
            _terrainBuckets.Clear();
            _dirtyBuckets.Clear();
        }

        #endregion

        #region Registration Logic

        public bool TryRegister(GameObject obj)
        {
            if (obj == null || !IsInitialized) return false;

            int id = obj.GetInstanceID();
            bool changed = false;

            if (obj.TryGetComponent<MeshRenderer>(out var renderer))
            {
                if (obj.TryGetComponent<MeshFilter>(out var filter) && filter.sharedMesh != null)
                {
                    var newState = SpatialState<MeshRenderer>.Create(_gridProvider.Anchor, renderer);
                    if (UpdateTypedRegistry(_meshRegistry, _meshBuckets, id, newState))
                        changed = true;
                }
            }

            if (obj.TryGetComponent<Terrain>(out var terrain) && terrain.terrainData != null)
            {
                var newState = SpatialState<Terrain>.Create(_gridProvider.Anchor, terrain);
                if (UpdateTypedRegistry(_terrainRegistry, _terrainBuckets, id, newState))
                    changed = true;
            }

            return changed;
        }

        public bool Unregister(int id)
        {
            bool removed = false;

            if (_meshRegistry.TryGetValue(id, out var mState))
            {
                UpdateBuckets(_meshBuckets, id, mState.anchorBounds, false);
                _meshRegistry.Remove(id);
                removed = true;
            }

            if (_terrainRegistry.TryGetValue(id, out var tState))
            {
                UpdateBuckets(_terrainBuckets, id, tState.anchorBounds, false);
                _terrainRegistry.Remove(id);
                removed = true;
            }

            return removed;
        }

        private bool UpdateTypedRegistry<T>(
            Dictionary<int, SpatialState<T>> registry,
            Dictionary<Vector3Int, HashSet<int>> bucketDict,
            int id,
            SpatialState<T> newState) where T : Component
        {
            if (registry.TryGetValue(id, out var oldState))
            {
                if (oldState.Equals(newState)) return false;
                UpdateBuckets(bucketDict, id, oldState.anchorBounds, false);
            }

            registry[id] = newState;
            UpdateBuckets(bucketDict, id, newState.anchorBounds, true);
            return true;
        }

        #endregion

        #region ISpatialCollection & Dispatcher Implementation

        /// <summary>
        /// English: Unified generic entry point. Dispatches to the correct internal engine based on T.
        /// This replaces GetComponentsInCell for a cleaner, type-safe API.
        /// </summary>
        public bool TryGetIterator<T>(Vector3Int key, out IIterator<T> iterator) where T : Component
        {
            iterator = null;

            if (typeof(T) == typeof(MeshRenderer))
            {
                if (TryGetMeshIterator(key, out var concrete))
                {
                    iterator = concrete as IIterator<T>;
                    return true;
                }
            }
            else if (typeof(T) == typeof(Terrain))
            {
                if (TryGetTerrainIterator(key, out var concrete))
                {
                    iterator = concrete as IIterator<T>;
                    return true;
                }
            }

            return false;
        }

        public ICollection<int> GetAllIds()
        {
            var ids = new List<int>(_meshRegistry.Count + _terrainRegistry.Count);
            ids.AddRange(_meshRegistry.Keys);
            ids.AddRange(_terrainRegistry.Keys);
            return ids;
        }

        public bool HasEntriesInCell(Vector3Int key)
        {
            return (_meshBuckets.TryGetValue(key, out var m) && m.Count > 0) ||
                   (_terrainBuckets.TryGetValue(key, out var t) && t.Count > 0);
        }

        public IIterator<Vector3Int> GetDirtyCells()
        {
            var enumerator = _dirtyBuckets.GetEnumerator();
            return new Iterator<Vector3Int, HashSet<Vector3Int>.Enumerator>(enumerator,
                (ref HashSet<Vector3Int>.Enumerator s, out Vector3Int res) =>
                {
                    bool next = s.MoveNext();
                    res = next ? s.Current : default;
                    return next;
                });
        }

        #endregion

        #region Specialized Internal Engines (Struct Path)

        public bool TryGetMeshIterator(Vector3Int key, out Iterator<MeshRenderer, SpatialIteratorState<Vector3Int, HashSet<int>.Enumerator>> iter)
        {
            if (_meshBuckets.TryGetValue(key, out var bucket))
            {
                var state = new SpatialIteratorState<Vector3Int, HashSet<int>.Enumerator>(key, bucket.GetEnumerator());
                iter = new Iterator<MeshRenderer, SpatialIteratorState<Vector3Int, HashSet<int>.Enumerator>>(state, TryGetNext);
                return true;
            }
            iter = default; return false;
        }

        public bool TryGetTerrainIterator(Vector3Int key, out Iterator<Terrain, SpatialIteratorState<Vector3Int, HashSet<int>.Enumerator>> iter)
        {
            if (_terrainBuckets.TryGetValue(key, out var bucket))
            {
                var state = new SpatialIteratorState<Vector3Int, HashSet<int>.Enumerator>(key, bucket.GetEnumerator());
                iter = new Iterator<Terrain, SpatialIteratorState<Vector3Int, HashSet<int>.Enumerator>>(state, TryGetNext);
                return true;
            }
            iter = default; return false;
        }

        #endregion

        #region IIterable Implementation (Spatial Iterators)

        public bool TryGetNext(ref SpatialIteratorState<Vector3Int, HashSet<int>.Enumerator> state, out MeshRenderer result)
        {
            result = null;
            while (state.Internal.MoveNext())
            {
                if (_meshRegistry.TryGetValue(state.Internal.Current, out var spatialState))
                {
                    result = spatialState.component;
                    return true;
                }
            }
            return false;
        }

        public bool TryGetNext(ref SpatialIteratorState<Vector3Int, HashSet<int>.Enumerator> state, out Terrain result)
        {
            result = null;
            while (state.Internal.MoveNext())
            {
                if (_terrainRegistry.TryGetValue(state.Internal.Current, out var spatialState))
                {
                    result = spatialState.component;
                    return true;
                }
            }
            return false;
        }

        #endregion

        #region Bucket Management

        private void UpdateBuckets(Dictionary<Vector3Int, HashSet<int>> bucketDict, int id, Bounds bounds, bool add)
        {
            if (!IsInitialized) return;

            foreach (Vector3Int key in _gridProvider.GetKeysInRelativeBounds(bounds))
            {
                _dirtyBuckets.Add(key);

                if (add)
                {
                    if (!bucketDict.TryGetValue(key, out var bucket))
                        bucketDict[key] = bucket = new HashSet<int>();
                    bucket.Add(id);
                }
                else if (bucketDict.TryGetValue(key, out var bucket))
                {
                    bucket.Remove(id);
                    if (bucket.Count == 0) bucketDict.Remove(key);
                }
            }
        }

        #endregion

        #region Helpers

        public void ClearDirtyCells() => _dirtyBuckets.Clear();

        [Conditional("UNITY_EDITOR")]
        private void LogDebug(string message, [CallerLineNumber] int line = 0)
            => DebugOutput.Log(message, showDebugLogs, lineNumber: line);

        #endregion
    }
}