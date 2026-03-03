using Rayforge.Core.Collections.Abstractions;
using Rayforge.Core.Collections.Iterator;
using Rayforge.Core.Collections.Helpers;
using Rayforge.Core.Environment.Abstractions;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Rayforge.Core.Environment.Spatial
{
    /// <summary>
    /// Handles typed spatial partitioning for a specific component type.
    /// Kapselt die Registrierung und das Bucket-Management für einen einzelnen Typen.
    /// </summary>
    /// <typeparam name="TKey">The spatial key type (e.g., Vector3Int).</typeparam>
    /// <typeparam name="TType">The component type to be managed (must be a Unity Component).</typeparam>
    public class SpatialObjectRegistry<TKey, TType> : ISpatialRegistry<TKey, TType>
        where TType : Component
        where TKey : struct, IEquatable<TKey>
    {
        #region Fields

        private string Tag => $"[ObjectRegistry<{typeof(TType).Name}>]";

        /// <summary> Primary storage: InstanceID -> State. </summary>
        private readonly Dictionary<int, SpatialState<TType>> _registry = new();

        /// <summary> Spatial Index: CellKey -> Set of InstanceIDs. </summary>
        private readonly Dictionary<TKey, HashSet<int>> _buckets = new();

        /// <summary> 
        /// Tracks cells that need re-baking. 
        /// Reference to either an internal or external shared HashSet.
        /// </summary>
        private HashSet<TKey> _dirtyBuckets;

        private ISpatialGridProvider<TKey> _gridProvider;

        /// <summary> Gets the total number of registered objects. </summary>
        public int RegisteredCount => _registry.Count;

        /// <summary> Gets all registered instance IDs. </summary>
        public IIterator<int> AllIds => _registry.Keys.GetEnumerator().ToIterator();

        /// <summary>
        /// Checks if the registry is fully operational.
        /// Requires a valid grid provider and an active dirty bucket collection.
        /// </summary>
        public bool IsInitialized => _gridProvider != null && _dirtyBuckets != null;

        #endregion

        #region Lifecycle

        /// <summary>
        /// Initializes the registry with a grid provider and an optional shared dirty tracker.
        /// </summary>
        /// <param name="gridProvider">The provider for coordinate mapping.</param>
        /// <param name="externalDirtyTracker">Optional shared HashSet to track modified cells across multiple registries.</param>
        public void Initialize(ISpatialGridProvider<TKey> gridProvider, HashSet<TKey> externalDirtyTracker = null)
        {
            try
            {
                Reset();

                _gridProvider = gridProvider ?? throw new ArgumentNullException(nameof(gridProvider));
                _dirtyBuckets = externalDirtyTracker ?? new HashSet<TKey>();

                FullRemap();
            }
            catch (Exception e)
            {
                throw new Exception($"{Tag} Initialization failed: {e.Message}", e);
            }
        }

        /// <summary>
        /// Clears and rebuilds all spatial buckets based on the current registry state.
        /// </summary>
        public void FullRemap()
        {
            if (!IsInitialized) return;

            try
            {
                _buckets.Clear();
                _dirtyBuckets.Clear();

                foreach (var entry in _registry)
                {
                    UpdateBuckets(entry.Key, entry.Value.anchorBounds, true);
                }
            }
            catch (Exception e)
            {
                throw new Exception($"{Tag} FullRemap failed: {e.Message}", e);
            }
        }

        /// <summary>
        /// Removes all objects and clears all spatial indices.
        /// </summary>
        public void Clear()
        {
            _registry?.Clear();
            _buckets?.Clear();
            _dirtyBuckets?.Clear();
        }

        /// <summary>
        /// Hard reset: Clears all data and detaches the grid provider and dirty tracker.
        /// </summary>
        public void Reset()
        {
            Clear();
            _gridProvider = null;
            _dirtyBuckets = null;
        }

        /// <summary>
        /// Clears the list of modified (dirty) cells.
        /// </summary>
        public void ClearDirtyCells() => _dirtyBuckets.Clear();

        #endregion

        #region Registration Logic

        /// <summary>
        /// Registers or updates an object in the spatial grid.
        /// Returns true if the registration resulted in a spatial change.
        /// </summary>
        /// <param name="id">The unique InstanceID of the object.</param>
        /// <param name="newState">The new spatial state including bounds and component reference.</param>
        public bool TryRegister(int id, SpatialState<TType> newState)
        {
            if (!IsInitialized)
                throw new InvalidOperationException($"{Tag} Not initialized!");

            if (_registry.TryGetValue(id, out var oldState))
            {
                if (oldState.Equals(newState)) return false;
                UpdateBuckets(id, oldState.anchorBounds, false);
            }

            _registry[id] = newState;
            UpdateBuckets(id, newState.anchorBounds, true);
            return true;
        }

        /// <summary>
        /// Removes an object from the registry and its corresponding spatial buckets.
        /// </summary>
        /// <param name="id">The unique InstanceID of the object to remove.</param>
        /// <returns>True if the object was found and removed.</returns>
        public bool Unregister(int id)
        {
            if (_registry.TryGetValue(id, out var state))
            {
                UpdateBuckets(id, state.anchorBounds, false);
                return _registry.Remove(id);
            }
            return false;
        }

        #endregion

        #region ISpatialRegistry & Dispatcher Implementation

        /// <summary>
        /// Unified generic entry point. Dispatches to the internal engine if T matches TType.
        /// </summary>
        /// <param name="key">The spatial cell coordinate.</param>
        /// <param name="iterator">The resulting iterator if the type matches.</param>
        /// <returns>True if the requested type T is managed by this registry.</returns>
        public bool TryGetIterator(TKey key, out IIterator<TType> iterator)
        {
            iterator = null;

            if (_buckets.TryGetValue(key, out var bucket))
            {
                var state = new SpatialIteratorState<TKey, TType>(
                    bucket.GetEnumerator(),
                    _registry
                );

                iterator = new Iterator<TType, SpatialIteratorState<TKey, TType>>(state);
                return true;
            }

            return false;
        }

        /// <summary> Checks if a specific cell contains any objects of type TType. </summary>
        public bool HasEntriesInCell(TKey key) => _buckets.TryGetValue(key, out var b) && b.Count > 0;

        /// <summary> Returns an iterator over all cells that have been modified since the last clear. </summary>
        public IIterator<TKey> GetDirtyCells()
        {
            return _dirtyBuckets.GetEnumerator().ToIterator();
        }

        #endregion

        #region Internal Bucket Management

        /// <summary>
        /// Internal helper to add or remove an object's ID from all affected spatial cells.
        /// </summary>
        private void UpdateBuckets(int id, Bounds bounds, bool add)
        {
            var keys = _gridProvider.GetKeysInRelativeBounds(bounds);

            foreach (TKey key in keys)
            {
                _dirtyBuckets.Add(key);

                if (add)
                {
                    if (!_buckets.TryGetValue(key, out var bucket))
                        _buckets[key] = bucket = new HashSet<int>();
                    bucket.Add(id);
                }
                else if (_buckets.TryGetValue(key, out var bucket))
                {
                    bucket.Remove(id);
                    if (bucket.Count == 0) _buckets.Remove(key);
                }
            }
        }

        #endregion
    }
}