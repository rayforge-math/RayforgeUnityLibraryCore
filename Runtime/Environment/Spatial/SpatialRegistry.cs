using Rayforge.Core.Environment.Abstractions;
using System.Collections.Generic;
using UnityEngine;

namespace Rayforge.Core.Environment.Spatial
{
    /// <summary>
    /// A high-performance base class for managing the lifecycle and storage of spatial objects.
    /// Handles dictionary-based storage, GameObject destruction, and dirty state tracking.
    /// </summary>
    /// <typeparam name="TKey">The type used for spatial indexing.</typeparam>
    /// <typeparam name="TValue">The type of spatial object.</typeparam>
    public abstract class SpatialRegistry<TKey, TValue>
        where TValue : ISpatialEntry
    {
        #region Data Structures
        /// <summary> Internal storage for spatial objects. </summary>
        protected readonly Dictionary<TKey, TValue> _storage = new Dictionary<TKey, TValue>();

        /// <summary> Parent transform for instantiated GameObjects. </summary>
        protected readonly Transform _container;

        /// <summary> Flag indicating if the collection composition has changed. </summary>
        protected bool _globalDirty = false;

        /// <summary> Provides access to all currently registered entries. </summary>
        public Dictionary<TKey, TValue>.ValueCollection AllEntries => _storage.Values;
        #endregion

        protected SpatialRegistry(Transform container = null)
        {
            _container = container;
        }

        #region Lifecycle Management
        /// <summary>
        /// Retrieves an entry by its key or null if none exists.
        /// </summary>
        public TValue GetEntry(TKey key) => _storage.TryGetValue(key, out TValue value) ? value : default;

        /// <summary>
        /// Safely destroys an object and removes it from the storage.
        /// </summary>
        public virtual void RemoveAndDestroy(TKey key)
        {
            if (_storage.Remove(key, out TValue value))
            {
                if (value != null)
                {
                    if (Application.isPlaying) UnityEngine.Object.Destroy(value.gameObject);
                    else UnityEngine.Object.DestroyImmediate(value.gameObject);
                }
                _globalDirty = true;
            }
        }

        /// <summary>
        /// Clears all entries and destroys their associated GameObjects.
        /// </summary>
        public virtual void Clear()
        {
            List<TKey> keys = new List<TKey>(_storage.Keys);
            foreach (var key in keys) RemoveAndDestroy(key);
            _storage.Clear();
            _globalDirty = true;
        }
        #endregion

        #region State Management
        /// <summary>
        /// Determines if any data needs a GPU update or if the collection changed.
        /// </summary>
        public bool NeedsUpdate()
        {
            if (_globalDirty) return true;
            foreach (var entry in _storage.Values)
                if (entry.IsDirty()) return true;
            return false;
        }

        /// <summary>
        /// Resets all internal dirty flags after a sync cycle.
        /// </summary>
        public void ResetDirtyFlags()
        {
            _globalDirty = false;
            foreach (var entry in _storage.Values)
                entry.ClearDirty();
        }
        #endregion
    }
}
