using Rayforge.Core.Collections.Abstractions;
using Rayforge.Core.Collections.Helpers;
using Rayforge.Core.Execution.Abstractions;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Rayforge.Core.Environment.Spatial
{
    public struct EntryCreateData<TKey>
    {
        public TKey key;
        public GameObject gameObject;
    }

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
        #region Private Members

        /// <summary> Internal storage for spatial objects. Encapsulated to ensure dirty-flag integrity. </summary>
        private readonly Dictionary<TKey, TValue> m_Storage = new Dictionary<TKey, TValue>();

        private Transform m_Container;
        private bool m_ContainerLinkedToAnchor = false;
        protected bool m_GlobalDirty = false;
        private bool m_IsInitialized = false;
        private string m_RegistryName;
        private bool m_Disposed = false;

        #endregion

        #region Public Properties

        /// <summary> Parent transform for instantiated GameObjects. </summary>
        public Transform Container => m_Container;

        /// <summary> Tracks if the container was created by this registry to allow auto-cleanup. </summary>
        public bool ContainerLinkedToAnchor => m_ContainerLinkedToAnchor;

        /// <summary> Flag indicating if the collection composition (addition/removal) has changed. </summary>
        public bool GlobalDirty => m_GlobalDirty;

        /// <summary>
        /// Returns true if Initialize has been called and the container exists.
        /// </summary>
        public bool IsInitialized => m_IsInitialized && m_Container != null && m_Container.gameObject != null;

        /// <summary> 
        /// The number of entries currently held in the registry.
        /// </summary>
        public int Count => m_Storage.Count;

        /// <summary> 
        /// The unique identification string of this registry instance.
        /// Useful for logging and identifying the container in the hierarchy.
        /// </summary>
        public virtual string RegistryName
        {
            get => m_RegistryName;
            set
            {
                if (string.IsNullOrWhiteSpace(value))
                {
                    throw new ArgumentException("RegistryName cannot be null or whitespace.", nameof(value));
                }

                if (m_Container == null)
                {
                    throw new InvalidOperationException("Cannot set RegistryName: m_Container is not assigned or has been destroyed.");
                }

                int id = m_Container.gameObject.GetInstanceID();
                m_RegistryName = $"{value}_{id}";
                m_Container.name = m_RegistryName;
            }
        }

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
            if (string.IsNullOrWhiteSpace(defaultName))
                throw new ArgumentException("Registry name cannot be null or empty.", nameof(defaultName));

            if (IsInitialized) Reset();

            GameObject go = new GameObject(defaultName);
            m_Container = go.transform;

            if (parent != null)
            {
                m_Container.SetParent(parent, false);
                m_Container.localPosition = Vector3.zero;
                m_Container.localRotation = Quaternion.identity;
                m_ContainerLinkedToAnchor = true;
            }
            else
            {
                m_ContainerLinkedToAnchor = false;
            }

            RegistryName = defaultName;

            m_IsInitialized = true;
        }

        #endregion

        #region Public Access

        /// <summary>
        /// Attempts to retrieve an entry by its spatial key.
        /// Returns true if the entry exists, otherwise false.
        /// </summary>
        public bool TryGetEntry(TKey key, out TValue value)
        {
            return m_Storage.TryGetValue(key, out value);
        }

        /// <summary>
        /// Determines whether the registry contains an entry with the specified key.
        /// </summary>
        /// <param name="key">The key to locate in the registry.</param>
        /// <returns>True if the registry contains an entry with the key; otherwise, false.</returns>
        public bool Contains(TKey key)
        {
            return m_Storage.ContainsKey(key);
        }

        /// <summary>
        /// Safely destroys the GameObject associated with the key and removes it from storage.
        /// Also triggers auto-cleanup of the container if applicable.
        /// </summary>
        public virtual void RemoveAndDestroy(TKey key)
        {
            if (m_Storage.Remove(key, out TValue value))
            {
                if (value != null && value.gameObject != null)
                {
                    DestroyGameObject(value.gameObject);
                }
                m_GlobalDirty = true;
            }
        }

        /// <summary>
        /// Clears all entries, destroys their associated GameObjects.
        /// </summary>
        public void Clear()
        {
            foreach (var value in m_Storage.Values)
            {
                if (value != null && value.gameObject != null)
                {
                    DestroyGameObject(value.gameObject);
                }
            }

            m_Storage.Clear();
        }

        #endregion

        #region Entry Creation

        /// <summary>
        /// The master factory method. Retrieves an existing entry or creates, parents, and registers a new one.
        /// Initialization logic is decoupled via the <see cref="IFunctionHandler{TData, TResult}"/> pattern to avoid heap allocations.
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
            where THandler : struct, IFunctionHandler<EntryCreateData<TKey>, TValue>
        {
            if (!IsInitialized)
                throw new InvalidOperationException("Registry is not initialized. Call Initialize() first.");

            if (m_Storage.TryGetValue(key, out result) && result != null)
            {
                bool isDestroyed = false;
                try
                {
                    isDestroyed = (result.gameObject == null);
                }
                catch (MissingReferenceException)
                {
                    isDestroyed = true;
                }

                if (!isDestroyed)
                {
                    return false;
                }
            }

            m_Storage.Remove(key);

            GameObject go = new GameObject(name);

            if (m_Container != null) go.transform.SetParent(m_Container);
            go.transform.position = position;

            var createData = new EntryCreateData<TKey> { key = key, gameObject = go };

            result = onCreate.Execute(createData);

            if (result == null)
            {
                DestroyGameObject(go);
                throw new NullReferenceException($"Handler failed to create a valid instance for key {key}.");
            }

            m_Storage[key] = result;
            m_GlobalDirty = true;

            return true;
        }

        #endregion

        #region Cleanup

        /// <summary>
        /// English: Disposes the registry and all its managed chunks.
        /// Triggers the abstract OnDispose logic in each chunk implementation.
        /// </summary>
        public virtual void Dispose()
        {
            if (m_Disposed) return;
            Reset();
            m_Disposed = true;
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Clears all entries, destroys their associated GameObjects, and removes the auto-generated container.
        /// </summary>
        public void Reset()
        {
            Clear();

            if (m_Container != null)
            {
                if (Application.isPlaying) UnityEngine.Object.Destroy(m_Container.gameObject);
                else UnityEngine.Object.DestroyImmediate(m_Container.gameObject);
                m_Container = null;
            }

            m_ContainerLinkedToAnchor = false;
            m_IsInitialized = false;
        }

        #endregion

        #region State Management

        /// <summary>
        /// Determines if any entry is dirty or if the collection itself has changed.
        /// </summary>
        public bool NeedsUpdate()
        {
            if (m_GlobalDirty) return true;
            foreach (var entry in m_Storage.Values)
                if (entry != null && entry.IsDirty) return true;
            return false;
        }

        /// <summary>
        /// Resets all internal and entry-level dirty flags after a synchronization cycle.
        /// </summary>
        public void ResetDirtyFlags()
        {
            m_GlobalDirty = false;
            foreach (var entry in m_Storage.Values)
                if (entry != null) entry.ClearDirty();
        }

        #endregion

        #region Iteration

        /// <summary>
        /// Gets an iterator over all keys currently stored in the registry.
        /// </summary>
        public IIterator<TKey> AllKeys => m_Storage.Keys.GetEnumerator().ToIterator();

        /// <summary>
        /// Gets an iterator over all entry values currently stored in the registry.
        /// </summary>
        public IIterator<TValue> AllEntries => m_Storage.Values.GetEnumerator().ToIterator();

        /// <summary>
        /// Executes a given action on every key in the registry.
        /// </summary>
        /// <typeparam name="TAction">The type of the execution handler.</typeparam>
        /// <param name="action">The action handler to execute for each key.</param>
        public void ForEachKey<TAction>(TAction action)
            where TAction : struct, IExecutionHandler<TKey>
        {
            var iter = m_Storage.Keys.GetEnumerator().ToIterator();
            while (iter.MoveNext())
            {
                action.Execute(iter.Current);
            }
        }

        /// <summary>
        /// Executes a given action on every entry value in the registry.
        /// </summary>
        /// <typeparam name="TAction">The type of the execution handler.</typeparam>
        /// <param name="action">The action handler to execute for each entry.</param>
        public void ForEachEntry<TAction>(TAction action)
            where TAction : struct, IExecutionHandler<TValue>
        {
            var iter = m_Storage.Values.GetEnumerator().ToIterator();
            while (iter.MoveNext())
            {
                action.Execute(iter.Current);
            }
        }

        #endregion

        #region Private Helpers

        private void DestroyGameObject(GameObject go)
        {
            if (go == null) return;
            if (Application.isPlaying) UnityEngine.Object.Destroy(go);
            else UnityEngine.Object.DestroyImmediate(go);
        }

        #endregion
    }
}
