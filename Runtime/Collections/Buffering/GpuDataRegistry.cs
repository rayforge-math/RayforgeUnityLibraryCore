using Rayforge.Core.Collections.Abstractions;
using Rayforge.Core.Execution.Abstractions;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine;

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

        /// <inheritdoc />
        public int Capacity => m_Capacity;

        /// <inheritdoc />
        public int BatchSize => m_BatchSize;

        /// <inheritdoc />
        public int Count => m_Mapper.Count;

        /// <inheritdoc />
        public int HighestIndex => m_Mapper.HighestActiveIndex;

        #endregion

        #region Init

        /// <summary>
        /// Default constructor is disabled to ensure the stores are properly initialized with valid capacity and batch size.
        /// </summary>
        [Obsolete("Use the parameterized constructor instead.", true)]
        private GpuDataRegistry() { }

        /// <summary>
        /// Initializes a new registry with a fixed capacity and batch size for all its stores.
        /// </summary>
        public GpuDataRegistry(int capacity, int batchSize)
        {
            Reconfigure(capacity, batchSize);
        }

        /// <inheritdoc />
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

        #region Store Management

        /// <inheritdoc />
        public bool IsDirty<T>() where T : unmanaged
            => GetStore<T>()?.AnyDirty ?? false;

        /// <inheritdoc />
        public bool AnyDirty
        {
            get
            {
                foreach(var store in m_Stores.Values)
                {
                    if (store.AnyDirty)
                        return true;
                }
                return false;
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

        /// <inheritdoc />
        public void Clear()
        {
            m_Mapper.Reset();

            foreach (var store in m_Stores.Values)
            {
                store.Clear();
            }
        }

        /// <inheritdoc />
        public void ClearDirtyState()
        {
            foreach (var store in m_Stores.Values)
            {
                store.ClearDirty();
            }
        }

        /// <inheritdoc />
        public void ClearDirty<T>() where T : unmanaged
        {
            if (m_Stores.TryGetValue(typeof(T), out var storeObj))
            {
                ((MetadataStore<T>)storeObj).ClearDirty();
            }
            else
            {
                throw new InvalidOperationException($"No store registered for type {typeof(T).Name}.");
            }
        }

        #endregion

        #region Public Access

        /// <inheritdoc />
        public IReadOnlyRawBuffer<T> GetReadOnlyBuffer<T>() where T : unmanaged
        {
            return GetStore<T>();
        }

        /// <inheritdoc />
        public IRawBuffer<T> GetRawBuffer<T>() where T : unmanaged
        {
            var store = GetStore<T>();

            if (store == null)
            {
                throw new InvalidOperationException(
                    $"No store registered for type {typeof(T).Name}. " +
                    "Ensure the store is added via AddStore<T>() before accessing.");
            }

            return store;
        }

        /// <inheritdoc />
        public void Upload<T>(ComputeBuffer target) where T : unmanaged
        {
            var store = GetStore<T>() ?? throw new InvalidOperationException($"No store registered for type {typeof(T).Name}.");
            Upload<T>(target, 0, 0, store.Capacity);
        }

        /// <inheritdoc />
        public void Upload<T>(ComputeBuffer target, int srcOffset, int destOffset, int count) where T : unmanaged
        {
            var store = GetStore<T>();

            if (store == null)
                throw new InvalidOperationException($"No store registered for type {typeof(T).Name}.");
    
            if (target == null)
                throw new ArgumentNullException(nameof(target), "Provided ComputeBuffer is null.");

            // Sanity Checks
            if (srcOffset < 0 || srcOffset + count > store.Capacity)
                throw new ArgumentOutOfRangeException(nameof(srcOffset), "Source offset or count out of store bounds.");
        
            if (destOffset < 0 || destOffset + count > target.count)
                throw new ArgumentOutOfRangeException(nameof(destOffset), "Destination offset or count out of ComputeBuffer bounds.");

            if (target.stride != store.Stride)
                throw new ArgumentException($"Stride mismatch: Buffer ({target.stride}) != Store ({store.Stride}).");

            target.SetData(store.TypedBuffer, srcOffset, destOffset, count);
        }

        /// <inheritdoc />
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

        /// <inheritdoc />
        public bool TryGet<T>(TKey key, out T value) where T : unmanaged
        {
            if (m_Mapper.TryGetIndex(key, out int index))
            {
                if (m_Stores.TryGetValue(typeof(T), out var storeObj))
                {
                    value = ((MetadataStore<T>)storeObj).Get(index);
                    return true;
                }
                throw new InvalidOperationException($"No store registered for type {typeof(T).Name}.");
            }

            value = default;
            return false;
        }

        /// <inheritdoc />
        public T Get<T>(TKey key) where T : unmanaged
        {
            if (TryGet(key, out T value))
            {
                return value;
            }

            throw new KeyNotFoundException($"The key {key} was not found in the registry.");
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
            {
                Clear();
                return false;
            }

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