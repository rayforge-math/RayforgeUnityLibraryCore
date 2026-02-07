using System;
using System.Collections.Generic;
using UnityEngine;

namespace Rayforge.Core.Environment.Spatial
{
    /// <summary>
    /// A high-performance base class for managing the lifecycle and storage of spatial objects.
    /// Handles dictionary-based storage, automated GameObject instantiation, hierarchy management, 
    /// and dirty state tracking.
    /// </summary>
    /// <typeparam name="TKey">The type used for spatial indexing (e.g., Vector3Int).</typeparam>
    /// <typeparam name="TValue">The type of spatial object, must be a MonoBehaviour and implement ISpatialEntry.</typeparam>
    public abstract class SpatialRegistry<TKey, TValue> : IDisposable
        where TValue : ISpatialEntry, IDisposable
    {
        #region Data Structures
        /// <summary> Internal storage for spatial objects. Encapsulated to ensure dirty-flag integrity. </summary>
        private readonly Dictionary<TKey, TValue> _storage = new Dictionary<TKey, TValue>();

        /// <summary> Parent transform for instantiated GameObjects. </summary>
        private Transform _container;
        protected Transform Container => _container;

        /// <summary> Tracks if the container was created by this registry to allow auto-cleanup. </summary>
        private bool _containerLinkedToAnchor = false;
        public bool ContainerLinkedToAnchor => _containerLinkedToAnchor;

        /// <summary> Flag indicating if the collection composition (addition/removal) has changed. </summary>
        protected bool _globalDirty = false;

        /// <summary> Provides read-only access to all currently registered entries. </summary>
        public Dictionary<TKey, TValue>.ValueCollection AllEntries => _storage.Values;

        /// <summary> 
        /// The unique identification string of this registry instance.
        /// Useful for logging and identifying the container in the hierarchy.
        /// </summary>
        public virtual string RegistryName
        {
            get => _registryName;
            protected set
            {
                int id = (_container != null) ? _container.gameObject.GetInstanceID() : 0;

                _registryName = $"{value}_{id}";

                if (_container != null)
                {
                    _container.name = _registryName;
                }
            }
        }
        private string _registryName;

        #endregion

        /// <summary>
        /// Initializes the registry. 
        /// </summary>
        /// <param name="parent">Optional parent transform. If null, the registry gets positioned in root.</param>
        /// <param name="defaultName">The name for the auto-generated container.</param>
        protected SpatialRegistry(Transform parent = null, string defaultName = "SpatialRegistry_Container")
        {
            GameObject go = new GameObject(defaultName);

            _container = go.transform;
            RegistryName = defaultName;

            if (parent != null)
            {
                _container.SetParent(parent, false);
                _container.localPosition = Vector3.zero;
                _container.localRotation = Quaternion.identity;

                _containerLinkedToAnchor = true;
            }
            else
            {
                _containerLinkedToAnchor = false;
            }
        }

        #region Lifecycle Management
        /// <summary>
        /// The master factory method. Retrieves an existing entry or creates, parents, and registers a new one.
        /// </summary>
        /// <param name="key">The spatial key for indexing.</param>
        /// <param name="name">The name for the new GameObject.</param>
        /// <param name="position">The initial world position.</param>
        /// <param name="factory">Factory method for initialization.</param>
        /// <returns>The existing or newly created entry.</returns>
        protected bool GetOrCreate(TKey key, string name, Vector3 position, Func<GameObject, TKey, TValue> factory, out TValue result)
        {
            if (_storage.TryGetValue(key, out result))
            {
                if (result != null && result.gameObject != null)
                {
                    return false;
                }
                _storage.Remove(key);
            }

            GameObject go = new GameObject(name);
            if (_container != null) go.transform.SetParent(_container);
            go.transform.position = position;

            result = factory.Invoke(go, key);

            _storage[key] = result;
            _globalDirty = true;

            return true;
        }

        /// <summary>
        /// Attempts to retrieve an entry by its spatial key.
        /// Returns true if the entry exists, otherwise false.
        /// </summary>
        public bool TryGetEntry(TKey key, out TValue value)
        {
            return _storage.TryGetValue(key, out value);
        }

        /// <summary>
        /// Safely destroys the GameObject associated with the key and removes it from storage.
        /// Also triggers auto-cleanup of the container if applicable.
        /// </summary>
        public virtual void RemoveAndDestroy(TKey key)
        {
            if (_storage.Remove(key, out TValue value))
            {
                if (value != null && value.gameObject != null)
                {
                    DestroyEntry(value);
                }

                _globalDirty = true;
            }
        }

        /// <summary>
        /// Clears all entries, destroys their associated GameObjects.
        /// </summary>
        public void ClearChunks()
        {
            foreach (var value in _storage.Values)
            {
                if (value != null && value.gameObject != null)
                {
                    DestroyEntry(value);
                }
            }

            _storage.Clear();
        }

        /// <summary>
        /// English: Disposes the registry and all its managed chunks.
        /// Triggers the abstract OnDispose logic in each chunk implementation.
        /// </summary>
        public virtual void Dispose() => Clear();

        /// <summary>
        /// Clears all entries, destroys their associated GameObjects, and removes the auto-generated container.
        /// </summary>
        public void Clear()
        {
            ClearChunks();

            if (_container != null)
            {
                if (Application.isPlaying) UnityEngine.Object.Destroy(_container.gameObject);
                else UnityEngine.Object.DestroyImmediate(_container.gameObject);
                _container = null;
            }
        }

        private void DestroyEntry(TValue entry)
        {
            if (entry == null) return;
            entry.Dispose();
        }

        #endregion

        #region State Management
        /// <summary>
        /// Determines if any entry is dirty or if the collection itself has changed.
        /// </summary>
        public bool NeedsUpdate()
        {
            if (_globalDirty) return true;
            foreach (var entry in _storage.Values)
                if (entry != null && entry.IsDirty) return true;
            return false;
        }

        /// <summary>
        /// Resets all internal and entry-level dirty flags after a synchronization cycle.
        /// </summary>
        public void ResetDirtyFlags()
        {
            _globalDirty = false;
            foreach (var entry in _storage.Values)
                if (entry != null) entry.ClearDirty();
        }
        #endregion
    }
}
