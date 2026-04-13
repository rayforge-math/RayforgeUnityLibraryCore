using Rayforge.Core.Collections.Abstractions;
using Rayforge.Core.Collections.Helpers;
using Rayforge.Core.Execution.Abstractions;
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
        protected virtual string Tag => $"[{GetType().Name}]";

        #region Chunk Create Struct

        public struct ChunkCreateData
        {
            public TKey key;
            public GameObject gameObject;
        }

        #endregion

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
        public IIterator<TValue> AllEntries => _storage.Values.GetEnumerator().ToIterator();

        /// <summary> 
        /// The number of entries currently held in the registry.
        /// </summary>
        public int Count => _storage.Count;

        /// <summary>
        /// Returns true if Initialize has been called and the container exists.
        /// </summary>
        public bool IsInitialized => _isInitialized && _container != null && _container.gameObject != null;
        private bool _isInitialized = false;

        /// <summary> 
        /// The unique identification string of this registry instance.
        /// Useful for logging and identifying the container in the hierarchy.
        /// </summary>
        public virtual string RegistryName
        {
            get => _registryName;
            protected set
            {
                try
                {
                    int id = (_container != null) ? _container.gameObject.GetInstanceID() : 0;
                    _registryName = $"{value}_{id}";

                    if (_container != null)
                    {
                        _container.name = _registryName;
                    }
                }
                catch (Exception e)
                {
                    throw new Exception($"{Tag} Failed to set RegistryName: {e.Message}", e);
                }
            }
        }
        private string _registryName;

        #endregion

        #region Initialization & Setup

        /// <summary>
        /// Empty default constructor to prevent premature side effects.
        /// </summary>
        protected SpatialRegistry() { }

        /// <summary>
        /// Initializes the registry, creates the container GameObject and sets up the hierarchy.
        /// English comment: Call this to activate the registry. If already initialized, it will perform a Reset.
        /// </summary>
        /// <param name="parent">Optional parent transform.</param>
        /// <param name="defaultName">The name for the auto-generated container.</param>
        public virtual void Initialize(Transform parent = null, string defaultName = "SpatialRegistry_Container")
        {
            try
            {
                if (IsInitialized) Clear();

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

                _isInitialized = true;
            }
            catch (Exception e)
            {
                throw new Exception($"{Tag} Failed to create registry hierarchy: {e.Message}", e);
            }
        }

        #endregion

        #region Lifecycle Management

        /// <summary>
        /// The master factory method. Retrieves an existing entry or creates, parents, and registers a new one.
        /// Initialization logic is decoupled via the <see cref="IExecutionHandler{T}"/> pattern to avoid heap allocations.
        /// </summary>
        /// <typeparam name="THandler">The struct handler type that implements the initialization logic.</typeparam>
        /// <param name="key">The spatial key for indexing.</param>
        /// <param name="name">The name for the new GameObject.</param>
        /// <param name="position">The initial world position.</param>
        /// <param name="onCreate">A reference to the struct handler that creates the new instance.</param>
        /// <param name="result">When this method returns, contains the existing or newly created entry.</param>
        /// <returns><b>true</b> if a brand new entry was created; <b>false</b> if an existing one was retrieved.</returns>
        /// <exception cref="InvalidOperationException">Thrown if the registry is not initialized.</exception>
        /// <exception cref="NullReferenceException">Thrown if the component could not be added or the handler fails.</exception>
        protected bool GetOrCreate<THandler>(TKey key, string name, Vector3 position, ref THandler onCreate, out TValue result)
            where THandler : struct, IFunctionHandler<ChunkCreateData, TValue>
        {
            if (!IsInitialized)
            {
                throw new InvalidOperationException($"{Tag} Registry is not initialized. Call Initialize() first.");
            }

            if (_storage.TryGetValue(key, out result))
            {
                if (result != null && result.gameObject != null)
                {
                    return false;
                }

                _storage.Remove(key);
            }

            GameObject go = new GameObject(name);
            try
            {
                if (_container != null) go.transform.SetParent(_container);
                go.transform.position = position;

                var createData = new ChunkCreateData
                {
                    key = key,
                    gameObject = go
                };
                result = onCreate.Execute(createData);

                if (result == null)
                    throw new NullReferenceException($"{Tag} AddComponent failed for {name}.");

                _storage[key] = result;
                _globalDirty = true;

                return true;
            }
            catch (Exception e)
            {
                if (go != null)
                {
                    if (Application.isPlaying) UnityEngine.Object.Destroy(go);
                    else UnityEngine.Object.DestroyImmediate(go);
                }

                throw new Exception($"{Tag} GetOrCreate failed for key {key}: {e.Message}", e);
            }
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
        public void Clear()
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
        public virtual void Dispose() => Reset();

        /// <summary>
        /// Clears all entries, destroys their associated GameObjects, and removes the auto-generated container.
        /// </summary>
        public void Reset()
        {
            Clear();

            if (_container != null)
            {
                if (Application.isPlaying) UnityEngine.Object.Destroy(_container.gameObject);
                else UnityEngine.Object.DestroyImmediate(_container.gameObject);
                _container = null;
            }

            _containerLinkedToAnchor = false;
            _isInitialized = false;
        }

        /// <summary>
        /// Triggers destruction of a spatial object.
        /// </summary>
        private void DestroyEntry(TValue entry)
        {
            if (entry == null) return;

            try
            {
                entry.Dispose();
            }
            catch (Exception e)
            {
                Debug.LogError($"{Tag} Destruction of entry failed for {entry.GetType().Name}: {e.Message}");
            }
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
