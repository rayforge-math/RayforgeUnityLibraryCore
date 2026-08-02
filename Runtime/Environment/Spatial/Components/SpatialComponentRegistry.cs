using Rayforge.Core.Collections.Abstractions;
using Rayforge.Core.Collections.Helpers;
using Rayforge.Core.Collections.Iterator;
using Rayforge.Core.Environment.Abstractions;
using Rayforge.Core.Execution.Abstractions;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Rayforge.Core.Environment.Spatial.Components
{
    /// <summary>
    /// Handles typed spatial partitioning for a specific component type.
    /// </summary>
    /// <typeparam name="TKey">The spatial key type (e.g., Vector3Int).</typeparam>
    /// <typeparam name="TType">The component type to be managed (must be a Unity Component).</typeparam>
    public class SpatialComponentRegistry<TKey, TType> : ISpatialRegistry<TKey, TType>, ISpatialCollection<TKey>
        where TType : Component
        where TKey : struct, IEquatable<TKey>
    {
        #region Fields

        /// <summary> Primary storage: InstanceID -> State. </summary>
        private readonly Dictionary<int, ComponentState<TType>> _registry = new();

        /// <summary> Spatial Index: CellKey -> Set of InstanceIDs. </summary>
        private readonly Dictionary<TKey, HashSet<int>> _buckets = new();

        /// <summary> Internal tracker for modified cells. </summary>
        private readonly HashSet<TKey> _dirtyBuckets = new();

        private ISpatialGridQuery<TKey> _gridProvider;

        /// <inheritdoc />
        public int StateCount => _registry.Count;

        /// <inheritdoc />
        public int CellCount => _buckets.Count;

        /// <inheritdoc />
        public int DirtyCellCount => _dirtyBuckets.Count;

        /// <inheritdoc />
        public int GetCellStateCount(TKey key) => _buckets.TryGetValue(key, out var bucket) ? bucket.Count : 0;

        /// <inheritdoc />
        public bool IsInitialized => _gridProvider != null;

        #endregion

        #region Lifecycle

        /// <inheritdoc />
        public void Initialize(ISpatialGridQuery<TKey> gridProvider)
        {
            _gridProvider = gridProvider ?? throw new ArgumentNullException(nameof(gridProvider));

            Reset();
        }

        /// <inheritdoc />
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
                throw new Exception($"FullRemap failed: {e.Message}", e);
            }
        }

        /// <inheritdoc />
        public void Clear()
        {
            _registry?.Clear();
            _buckets?.Clear();
            _dirtyBuckets?.Clear();
        }

        /// <inheritdoc />
        public void Reset()
        {
            Clear();
            _gridProvider = null;
        }

        /// <inheritdoc />
        public void ClearDirtyCells() => _dirtyBuckets.Clear();

        #endregion

        #region Registration & Lookup Logic

        /// <inheritdoc />
        public bool TryRegister(int id, ComponentState<TType> newState)
        {
            if (!IsInitialized)
                throw new InvalidOperationException($"Registry not initialized!");

            if (_registry.TryGetValue(id, out var oldState))
            {
                if (oldState.Equals(newState)) return false;
                UpdateBuckets(id, oldState.anchorBounds, false);
            }

            _registry[id] = newState;
            UpdateBuckets(id, newState.anchorBounds, true);
            return true;
        }

        /// <inheritdoc />
        public bool Unregister(int id)
        {
            if (_registry.TryGetValue(id, out var state))
            {
                UpdateBuckets(id, state.anchorBounds, false);
                return _registry.Remove(id);
            }
            return false;
        }

        /// <inheritdoc />
        public bool Contains(int id) => _registry.ContainsKey(id);

        /// <inheritdoc />
        public bool TryGetState(int id, out ComponentState<TType> state) => _registry.TryGetValue(id, out state);

        #endregion

        #region ISpatialCollection<TKey> Implementation

        /// <inheritdoc />
        public void ForEachCell<TAction>(ref TAction action)
            where TAction : struct, IExecutionHandler<TKey>
        {
            foreach (var key in _buckets.Keys)
            {
                action.Execute(key);
            }
        }

        /// <inheritdoc />
        public IIterator<TKey> GetCellIterator()
        {
            return _buckets.Keys.GetEnumerator().ToIterator();
        }

        #endregion

        #region ISpatialRegistry<TKey, TType> Implementation

        /// <inheritdoc />
        public bool IsCellActive(TKey key) => _buckets.TryGetValue(key, out var b) && b.Count > 0;

        /// <inheritdoc />
        public bool TryForEachInCell<TAction>(TKey key, ref TAction action)
            where TAction : struct, IExecutionHandler<TType>
        {
            if (_buckets.TryGetValue(key, out var bucket))
            {
                foreach (int id in bucket)
                {
                    if (_registry.TryGetValue(id, out var state))
                    {
                        action.Execute(state.component);
                    }
                }
                return true;
            }
            return false;
        }

        /// <inheritdoc />
        public void ForEachDirtyCell<TAction>(ref TAction action)
            where TAction : struct, IExecutionHandler<TKey>
        {
            foreach (var key in _dirtyBuckets)
            {
                action.Execute(key);
            }
        }

        /// <inheritdoc />
        public bool TryGetEntryIterator(TKey key, out IIterator<TType> iterator)
        {
            iterator = null;

            if (_buckets.TryGetValue(key, out var bucket))
            {
                var state = new ComponentIteratorState<int, TType, HashSet<int>.Enumerator>(
                    bucket.GetEnumerator(),
                    _registry
                );

                iterator = new Iterator<TType, ComponentIteratorState<int, TType, HashSet<int>.Enumerator>>(state);
                return true;
            }

            return false;
        }

        /// <inheritdoc />
        public IIterator<TKey> GetDirtyCellIterator()
        {
            return _dirtyBuckets.GetEnumerator().ToIterator();
        }

        /// <inheritdoc />
        public IIterator<int> AllIds => _registry.Keys.GetEnumerator().ToIterator();

        /// <inheritdoc />
        public IIterator<TKey> AllKeys => _buckets.Keys.GetEnumerator().ToIterator();

        /// <inheritdoc />
        public IIterator<ComponentState<TType>> AllStates => _registry.Values.GetEnumerator().ToIterator();

        /// <inheritdoc />
        public IIterator<int> CellIds(TKey key)
        {
            if (_buckets.TryGetValue(key, out var bucket))
            {
                return bucket.GetEnumerator().ToIterator();
            }
            return IIterator<int>.Empty();
        }

        /// <inheritdoc />
        public void ForEachId<TAction>(ref TAction action)
            where TAction : struct, IExecutionHandler<int>
        {
            foreach (var id in _registry.Keys)
            {
                action.Execute(id);
            }
        }

        /// <inheritdoc />
        public void ForEachKey<TAction>(ref TAction action)
            where TAction : struct, IExecutionHandler<TKey>
        {
            foreach (var key in _buckets.Keys)
            {
                action.Execute(key);
            }
        }

        /// <inheritdoc />
        public bool TryForEachCellId<TAction>(TKey key, ref TAction action)
            where TAction : struct, IExecutionHandler<int>
        {
            if (_buckets.TryGetValue(key, out var bucket))
            {
                foreach (int id in bucket)
                {
                    action.Execute(id);
                }
                return true;
            }
            return false;
        }

        /// <inheritdoc />
        public void ForEachState<TAction>(ref TAction action)
            where TAction : struct, IExecutionHandler<ComponentState<TType>>
        {
            foreach (var state in _registry.Values)
            {
                action.Execute(state);
            }
        }

        #endregion

        #region Internal Bucket Management

        /// <summary>
        /// Internal helper to add or remove an object's ID from all affected spatial cells.
        /// </summary>
        private void UpdateBuckets(int id, Bounds bounds, bool add)
        {
            var bucketUpdater = new BucketUpdaterHandler(this, id, add);
            _gridProvider.ForEachKeyInRelativeBounds(bounds, ref bucketUpdater);
        }

        /// <summary>
        /// High-performance execution handler for updating spatial buckets without allocations.
        /// </summary>
        private struct BucketUpdaterHandler : IExecutionHandler<TKey>
        {
            private readonly SpatialComponentRegistry<TKey, TType> _registry;
            private readonly int _id;
            private readonly bool _add;

            public BucketUpdaterHandler(SpatialComponentRegistry<TKey, TType> registry, int id, bool add)
            {
                _registry = registry;
                _id = id;
                _add = add;
            }

            public void Execute(TKey key)
            {
                _registry._dirtyBuckets.Add(key);

                if (_add)
                {
                    if (!_registry._buckets.TryGetValue(key, out var bucket))
                        _registry._buckets[key] = bucket = new HashSet<int>();
                    bucket.Add(_id);
                }
                else if (_registry._buckets.TryGetValue(key, out var bucket))
                {
                    bucket.Remove(_id);
                    if (bucket.Count == 0)
                        _registry._buckets.Remove(key);
                }
            }
        }

        #endregion
    }
}