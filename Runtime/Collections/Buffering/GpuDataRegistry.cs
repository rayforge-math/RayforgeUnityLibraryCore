using Mono.Cecil;
using Rayforge.Core.Collections.Abstractions;
using Rayforge.Core.Execution.Abstractions;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace Rayforge.Core.Collections.Buffering
{
    /// <summary>
    /// A centralized hub for managing GPU-bound metadata. 
    /// Bridges logical keys to multiple typed data stores (Spatial, Visual, etc.).
    /// </summary>
    /// <typeparam name="TKey">The unique identifier type (e.g., Vector3Int for Chunks).</typeparam>
    public class GpuDataRegistry<TKey> : IGpuDataProvider<TKey>
        where TKey : struct, IEquatable<TKey>
    {
        #region Properties

        private readonly KeyedSlotMapper<TKey> m_Mapper = new();
        private readonly Dictionary<Type, IMetadataController> m_Stores = new();

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

        #endregion

        #region Store Diagnostics & Raw Access

        /// <summary>
        /// Returns the size in bytes of the unmanaged type T.
        /// Note: This does not depend on a registered store, so it always returns the size.
        /// </summary>
        public int GetStride<T>() where T : unmanaged
            => Unsafe.SizeOf<T>();

        /// <summary>
        /// Returns true if the store for type T is dirty.
        /// If no store is registered, it is considered not dirty (false).
        /// </summary>
        public bool IsDirty<T>() where T : unmanaged
            => GetStore<T>()?.AnyDirty ?? false;

        /// <summary>
        /// Gets the underlying raw data array.
        /// Throws InvalidOperationException if the store is not registered.
        /// </summary>
        public Array GetUntypedBuffer<T>() where T : unmanaged
        {
            var store = GetStore<T>() ?? throw new InvalidOperationException($"No store registered for type {typeof(T).Name}.");
            return store.UntypedBuffer;
        }

        /// <summary>
        /// Gets the typed array for CPU-side interop and manual buffer manipulation.
        /// Throws InvalidOperationException if the store is not registered.
        /// </summary>
        public T[] GetTypedBuffer<T>() where T : unmanaged
        {
            var store = GetStore<T>() ?? throw new InvalidOperationException($"No store registered for type {typeof(T).Name}.");
            return store.TypedBuffer;
        }

        /// <summary>
        /// Gets the data as a ReadOnlySpan.
        /// Returns ReadOnlySpan.Empty if no store is registered.
        /// </summary>
        public ReadOnlySpan<T> AsSpan<T>() where T : unmanaged
        {
            var store = GetStore<T>();
            return store != null ? store.AsSpan() : ReadOnlySpan<T>.Empty;
        }

        #endregion

        #region Public Store Management

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
        /// Retrieves an existing store as a read-only interface.
        /// Use this for external systems like renderers that should not 
        /// be able to call Resize or UpdateBatchSize.
        /// </summary>
        public MetadataStore<T> GetStore<T>() where T : unmanaged
        {
            if (m_Stores.TryGetValue(typeof(T), out var storeObj))
                return (MetadataStore<T>)storeObj;
            return null;
        }

        #endregion

        #region Init

        /// <summary>
        /// Initializes a new registry with a fixed capacity and batch size for all its stores.
        /// </summary>
        public GpuDataRegistry(int capacity, int batchSize)
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
                throw new ArgumentOutOfRangeException(nameof(capacity), $"Capacity must be greater than zero.");

            if (batchSize <= 0)
                throw new ArgumentOutOfRangeException(nameof(batchSize), $"BatchSize must be at least 1.");

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

        #endregion

        #region Public Management API

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
        /// Sets the value for a specific key and type. 
        /// Automatically allocates a slot if the key is new.
        /// </summary>
        /// <typeparam name="T">The unmanaged type of the metadata.</typeparam>
        /// <param name="key">The key identifying the data slot.</param>
        /// <param name="value">The value to store.</param>
        /// <returns>The absolute index where the value was stored.</returns>
        /// <exception cref="InvalidOperationException">Thrown if no store for type T is registered.</exception>
        public int Set<T>(TKey key, T value) where T : unmanaged
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

            return index;
        }

        /// <summary>
        /// Retrieves the value for a specific key.
        /// </summary>
        /// <typeparam name="T">The unmanaged type of the metadata.</typeparam>
        /// <param name="key">The key identifying the data slot.</param>
        /// <returns>The stored value, or default(T) if the key is not found.</returns>
        /// <exception cref="InvalidOperationException">Thrown if no store for type T is registered.</exception>
        public T Get<T>(TKey key) where T : unmanaged
        {
            if (m_Mapper.TryGetIndex(key, out int index))
            {
                if (m_Stores.TryGetValue(typeof(T), out var storeObj))
                {
                    return ((MetadataStore<T>)storeObj).Get(index);
                }
                throw new InvalidOperationException($"No store registered for type {typeof(T).Name}.");
            }
            return default;
        }

        /// <summary>
        /// Releases the key and ensures that the data in a specific "Main-Store" is reset.
        /// This prevents 'ghosting' (GPU rendering old data) by invalidating the culling data.
        /// </summary>
        /// <typeparam name="TMain">The primary store type to clear (usually the one used for Visibility/Culling).</typeparam>
        /// <returns>The index that was released, or -1 if the key was not found.</returns>
        public int ReleaseAndInvalidate<TMain>(TKey key) where TMain : unmanaged
        {
            if (m_Mapper.TryGetIndex(key, out int index))
            {
                GetStore<TMain>()?.Set(index, default);
                m_Mapper.Release(key);
                return index;
            }
            return -1;
        }

        /// <summary>
        /// Releases the index associated with a key. 
        /// Note: This does not clear the stores. Update data to a 'null' state via Set() before releasing if needed.
        /// </summary>
        /// <returns>The index that was released, or -1 if the key was not found.</returns>
        public int Release(TKey key)
        {
            if (m_Mapper.TryGetIndex(key, out int index))
            {
                m_Mapper.Release(key);
                return index;
            }
            return -1;
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

        /// <summary>
        /// Resizes all registered stores and the internal mapper to a new capacity.
        /// </summary>
        /// <param name="newCapacity">The new maximum number of slots.</param>
        /// <returns>True if the capacity changed and data was reset; false if already at target capacity.</returns>
        public bool Resize(int newCapacity)
        {
            if (newCapacity <= 0)
                throw new ArgumentOutOfRangeException(nameof(newCapacity), $"Capacity must be greater than zero.");

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
                throw new ArgumentOutOfRangeException(nameof(newBatchSize), $"BatchSize must be at least 1.");

            if (m_BatchSize == newBatchSize)
                return false;

            m_BatchSize = newBatchSize;

            foreach (var store in m_Stores.Values)
            {
                store.UpdateBatchSize(newBatchSize);
            }

            return true;
        }

        #endregion

        #region Iteration

        /// <inheritdoc />
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void ForEachDirtySegment<T, TAction>(ref TAction action, bool mergeContiguous = true)
            where T : unmanaged
            where TAction : struct, IExecutionHandler<BufferSegmentMeta<T>>
        {
            var store = GetStore<T>();
            if (store != null && store.AnyDirty)
            {
                // Wir delegieren den ref call direkt an den Store, 
                // damit der JIT-Compiler die Schleife inlinen kann.
                store.ForEachDirtySegment(ref action, mergeContiguous);
            }
        }

        /// <inheritdoc />
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void ForEachDirtyIndex<T, TAction>(ref TAction action)
            where T : unmanaged
            where TAction : struct, IExecutionHandler<int>
        {
            var store = GetStore<T>();
            if (store != null && store.AnyDirty)
            {
                store.ForEachDirtyIndex(ref action);
            }
        }

        /// <inheritdoc />
        /// <remarks>
        /// CAUTION: This implementation boxes the internal iterator. 
        /// Use <see cref="ForEachDirtySegment{T, TAction}"/> for performance-critical paths.
        /// </remarks>
        public IIterator<BufferSegmentMeta<T>> GetDirtySegmentIterator<T>(bool mergeContiguous = true)
            where T : unmanaged
        {
            var store = GetStore<T>();
            return store?.GetDirtySegmentIterator(mergeContiguous) ?? default;
        }

        /// <inheritdoc />
        /// <remarks>
        /// CAUTION: This implementation boxes the internal iterator.
        /// </remarks>
        public IIterator<int> GetDirtySegmentIndices<T>()
            where T : unmanaged
        {
            var store = GetStore<T>();
            return store?.GetDirtySegmentIndices() ?? default;
        }

        #endregion
    }
}