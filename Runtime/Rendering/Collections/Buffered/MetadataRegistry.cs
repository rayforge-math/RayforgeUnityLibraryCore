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
    public class MetadataRegistry<TKey> where TKey : struct, IEquatable<TKey>
    {
        private readonly KeyedSlotMapper<TKey> m_Mapper;
        private readonly Dictionary<Type, IMetadataStore> m_Stores = new();

        private readonly int m_Capacity;
        private readonly int m_BatchSize;

        /// <summary>
        /// Gets the total capacity shared across all stores.
        /// </summary>
        public int Capacity => m_Capacity;

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
            m_Capacity = capacity;
            m_BatchSize = batchSize;
            m_Mapper = new KeyedSlotMapper<TKey>(capacity);
        }

        /// <summary>
        /// Resets the registry by clearing the key-to-slot mapping and resetting all registered data stores.
        /// Full logical reset. Clears the mapper and zeroes out all CPU arrays and dirty bits.
        /// </summary>
        public void Reset()
        {
            m_Mapper.Reset();

            foreach (var store in m_Stores.Values)
            {
                store.Reset();
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
        /// Registers a new data stream for a specific type. 
        /// Returns the existing store if it was already registered.
        /// </summary>
        public MetadataStore<T> AddStore<T>() where T : unmanaged
        {
            var type = typeof(T);
            if (m_Stores.TryGetValue(type, out var existing))
                return (MetadataStore<T>)existing;

            var store = new MetadataStore<T>(m_Capacity, m_BatchSize);
            m_Stores[type] = store;
            return store;
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
        /// Retrieves the store for a specific type. Useful for manual synchronization.
        /// </summary>
        public MetadataStore<T> GetStore<T>() where T : unmanaged
        {
            if (m_Stores.TryGetValue(typeof(T), out var storeObj))
                return (MetadataStore<T>)storeObj;
            return null;
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
                GetStore<TMain>()?.Set(index, default);
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