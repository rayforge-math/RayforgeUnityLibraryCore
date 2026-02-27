using Rayforge.Core.Rendering.Abstractions;
using System;
using System.Collections.Generic;

namespace Rayforge.Core.Rendering.Collections.Buffered
{
    /// <summary>
    /// A centralized hub for managing GPU-bound metadata. 
    /// Bridges logical keys to multiple typed data stores (Spatial, Visual, etc.).
    /// </summary>
    /// <typeparam name="TKey">The unique identifier type (e.g., Vector3Int for Chunks).</typeparam>
    public class MetadataRegistry<TKey> : IMetadataRegistry
        where TKey : struct, IEquatable<TKey>
    {
        private const string Tag = "[MetadataRegistry]";

        private readonly KeyedSlotMapper<TKey> m_Mapper = new();
        private readonly Dictionary<Type, IMetadataStoreController> m_Stores = new();

        private int m_Capacity;
        private int m_BatchSize;

        /// <summary>
        /// Gets the total capacity shared across all stores.
        /// </summary>
        public int Capacity => m_Capacity;

        /// <summary>
        /// Gets the active batch size.
        /// </summary>
        public int BatchSize => m_BatchSize;

        /// <summary>
        /// Gets the number of currently active keys in the registry.
        /// </summary>
        public int Count => m_Mapper.Count;

        /// <summary>
        /// Gets the highest allocated index. Useful for limiting the range of GPU compute dispatches.
        /// </summary>
        public int HighestIndex => m_Mapper.HighestActiveIndex;

        /// <summary>
        /// Initializes a new registry with a fixed capacity and batch size for all its stores.
        /// </summary>
        public MetadataRegistry(int capacity, int batchSize)
        {
            Reconfigure(capacity, batchSize);
        }

        /// <summary>
        /// (Re)Initializes the registry with a fixed capacity and batch size.
        /// If parameters are identical, it only clears data to ensure a fresh state.
        /// If parameters differ, it performs a structural re-allocation.
        /// </summary>
        /// <param name="capacity">The target slot capacity.</param>
        /// <param name="batchSize">The target sync granularity.</param>
        /// <returns>True if a structural resize/re-allocation happened; false if only a Clear was performed.</returns>
        public bool Reconfigure(int capacity, int batchSize)
        {
            if (capacity <= 0)
                throw new ArgumentOutOfRangeException(nameof(capacity), $"{Tag} Capacity must be greater than zero.");

            if (batchSize <= 0)
                throw new ArgumentOutOfRangeException(nameof(batchSize), $"{Tag} BatchSize must be at least 1.");

            if (m_Capacity == capacity && m_BatchSize == batchSize)
            {
                Clear();
                return false;
            }

            m_Capacity = capacity;
            m_BatchSize = batchSize;

            m_Mapper.Initialize(capacity);
            foreach (var store in m_Stores.Values)
            {
                store.Resize(capacity);
                store.UpdateBatchSize(batchSize);
            }

            return true;
        }

        /// <summary>
        /// Resizes all registered stores and the internal mapper to a new capacity.
        /// </summary>
        /// <param name="newCapacity">The new maximum number of slots.</param>
        /// <returns>True if the capacity changed and data was reset; false if already at target capacity.</returns>
        public bool Resize(int newCapacity)
        {
            if (newCapacity <= 0)
                throw new ArgumentOutOfRangeException(nameof(newCapacity), $"{Tag} Capacity must be greater than zero.");

            if (m_Capacity == newCapacity)
                return false;

            m_Capacity = newCapacity;
            m_Mapper.Initialize(newCapacity);
            foreach (var store in m_Stores.Values)
            {
                store.Resize(newCapacity);
            }

            return true;
        }

        /// <summary>
        /// Updates the dirty-tracking granularity for all registered stores.
        /// This is a non-destructive operation. Metadata values are preserved.
        /// </summary>
        /// <param name="newBatchSize">The new size for tracking segments.</param>
        /// <returns>True if the batch size was changed and migrated; false if already at target size.</returns>
        public bool UpdateBatchSize(int newBatchSize)
        {
            if (newBatchSize <= 0)
                throw new ArgumentOutOfRangeException(nameof(newBatchSize), $"{Tag} BatchSize must be at least 1.");

            if (m_BatchSize == newBatchSize)
                return false;

            m_BatchSize = newBatchSize;

            foreach (var store in m_Stores.Values)
            {
                store.UpdateBatchSize(newBatchSize);
            }

            return true;
        }

        /// <summary>
        /// Resets the registry by clearing the key-to-slot mapping and resetting all registered data stores.
        /// </summary>
        public void Clear()
        {
            m_Mapper.Reset();

            foreach (var store in m_Stores.Values)
            {
                store.Clear();
            }
        }

        /// <summary>
        /// Synchronizes all modified data across all stores to the GPU.
        /// Centralized sync point for the entire metadata system.
        /// </summary>
        /// <param name="uploadAction">Callback for (Array source, int start, int count, Type storeType).</param>
        public void SyncAllStores(Action<Array, int, int, Type> uploadAction)
        {
            foreach (var entry in m_Stores)
            {
                var store = entry.Value;
                if (store.AnyDirty)
                {
                    store.ProcessDirtyBatches((data, start, count) =>
                        uploadAction(data, start, count, entry.Key));
                }
            }
        }

        /// <summary>
        /// Resets the dirty tracking state for all registered stores.
        /// Call this after a successful SyncAllStores to acknowledge processed data.
        /// </summary>
        public void ClearDirtyState()
        {
            foreach (var store in m_Stores.Values)
            {
                store.ClearDirty();
            }
        }

        /// <summary>
        /// Registers a new data stream for a specific type. 
        /// Returns the existing store if it was already registered.
        /// </summary>
        protected MetadataStore<T> AddStore<T>() where T : unmanaged
        {
            var type = typeof(T);
            if (m_Stores.TryGetValue(type, out var existing))
                return (MetadataStore<T>)existing;

            var store = new MetadataStore<T>(m_Capacity, m_BatchSize);
            m_Stores[type] = store;
            return store;
        }

        /// <summary>
        /// Retrieves an existing store as a read-only interface.
        /// Use this for external systems like renderers that should not 
        /// be able to call Resize or UpdateBatchSize.
        /// </summary>
        protected MetadataStore<T> GetStoreInternal<T>() where T : unmanaged
        {
            if (m_Stores.TryGetValue(typeof(T), out var storeObj))
                return (MetadataStore<T>)storeObj;
            return null;
        }

        /// <summary>
        /// Retrieves an existing store as a read-only interface.
        /// Use this for external systems like renderers that should not 
        /// be able to call Resize or UpdateBatchSize.
        /// </summary>
        protected IMetadataStore GetStore<T>() where T : unmanaged
        {
            return GetStoreInternal<T>();
        }

        /// <summary>
        /// Updates a value for a specific key and type. 
        /// Automatically allocates a slot if the key is new.
        /// </summary>
        public void Set<T>(TKey key, T value) where T : unmanaged
        {
            int index = m_Mapper.GetOrAllocate(key);

            if (m_Stores.TryGetValue(typeof(T), out var storeObj))
            {
                ((MetadataStore<T>)storeObj).Set(index, value);
            }
            else
            {
                throw new InvalidOperationException($"No store registered for type {typeof(T).Name}. Call AddStore first.");
            }
        }

        /// <summary>
        /// Releases the key and ensures that the data in a specific "Main-Store" is reset.
        /// This prevents 'ghosting' (GPU rendering old data) by invalidating the culling data.
        /// </summary>
        /// <typeparam name="TMain">The primary store type to clear (usually the one used for Visibility/Culling).</typeparam>
        public void ReleaseAndInvalidate<TMain>(TKey key) where TMain : unmanaged
        {
            if (m_Mapper.TryGetIndex(key, out int index))
            {
                GetStoreInternal<TMain>()?.Set(index, default);
                Release(key);
            }
        }

        /// <summary>
        /// Releases the index associated with a key. 
        /// Note: This does not clear the stores. Update data to a 'null' state via Set() before releasing if needed.
        /// </summary>
        public void Release(TKey key)
        {
            m_Mapper.Release(key);
        }

        /// <summary>
        /// Tries to get the current index for a specific key.
        /// </summary>
        public bool TryGetIndex(TKey key, out int index)
        {
            return m_Mapper.TryGetIndex(key, out index);
        }

        /// <summary>
        /// Direct access to the internal index allocation. 
        /// Useful for high-performance manual updates across multiple cached stores.
        /// </summary>
        public int GetOrAllocateIndex(TKey key) => m_Mapper.GetOrAllocate(key);
    }
}