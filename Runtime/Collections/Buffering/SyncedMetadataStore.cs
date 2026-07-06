using Rayforge.Core.Collections.Abstractions;
using Rayforge.Core.Collections.Iterator;
using Rayforge.Core.Execution.Abstractions;
using System;
using System.Runtime.CompilerServices;

namespace Rayforge.Core.Collections.Buffering
{
    /*
    /// <summary>
    /// An abstract base class for a registry managing exactly two distinct metadata stores.
    /// Provides high-performance, type-safe access to store-specific buffers and centralized state management.
    /// </summary>
    /// <typeparam name="TKey">The unique identifier type (e.g., Vector3Int).</typeparam>
    /// <typeparam name="TStoreA">The unmanaged data type for the first store.</typeparam>
    /// <typeparam name="TStoreB">The unmanaged data type for the second store.</typeparam>
    public abstract class SyncedMetadataStore<TKey, TStoreA, TStoreB> 
        : IIterable<SyncedSegmentMeta<TStoreA, TStoreB>>
        where TKey : struct, IEquatable<TKey>
        where TStoreA : unmanaged
        where TStoreB : unmanaged
    {
        #region Properties

        private readonly KeyedSlotMapper<TKey> m_Mapper = new();
        private readonly IMetadataController[] m_AllStores;

        /// <summary> The first metadata store instance. </summary>
        protected readonly MetadataStore<TStoreA> StoreA;
        /// <summary> The second metadata store instance. </summary>
        protected readonly MetadataStore<TStoreB> StoreB;

        private int m_Capacity;
        private int m_BatchSize;

        /// <summary> Gets the total capacity shared across all stores. </summary>
        public int Capacity => m_Capacity;

        /// <summary> Gets the current synchronization granularity. </summary>
        public int BatchSize => m_BatchSize;

        /// <summary> Gets the number of currently active keys. </summary>
        public int Count => m_Mapper.Count;

        /// <summary> Gets the highest allocated index for GPU dispatch optimization. </summary>
        public int HighestIndex => m_Mapper.HighestActiveIndex;

        #endregion

        #region Init

        /// <summary>
        /// Default constructor is disabled to ensure the stores are properly initialized with valid capacity and batch size.
        /// </summary>
        [Obsolete("Use the parameterized constructor instead.", true)]
        private SyncedMetadataStore() { }

        /// <summary> 
        /// Initializes a new registry and performs the initial allocation. 
        /// </summary>
        /// <exception cref="ArgumentOutOfRangeException">Thrown if capacity or batchSize is invalid.</exception>
        protected SyncedMetadataStore(int capacity, int batchSize)
        {
            if (capacity <= 0) throw new ArgumentOutOfRangeException(nameof(capacity), $"Capacity must be > 0.");
            if (batchSize <= 0) throw new ArgumentOutOfRangeException(nameof(batchSize), $"BatchSize must be >= 1.");

            m_Capacity = capacity;
            m_BatchSize = batchSize;

            m_Mapper.Initialize(capacity);
            StoreA = new MetadataStore<TStoreA>(capacity, batchSize);
            StoreB = new MetadataStore<TStoreB>(capacity, batchSize);

            m_AllStores = new IMetadataController[] { StoreA, StoreB };
        }

        /// <summary>
        /// (Re)Initializes the registry with a new capacity and batch size.
        /// If parameters are identical to current state, it only clears existing data.
        /// Otherwise, it performs a structural re-allocation of all stores and the mapper.
        /// </summary>
        /// <param name="capacity">The target slot capacity.</param>
        /// <param name="batchSize">The target sync granularity.</param>
        /// <returns>True if a structural resize occurred; false if only a Clear was performed.</returns>
        /// <exception cref="ArgumentOutOfRangeException">Thrown if parameters are non-positive.</exception>
        public bool Reconfigure(int capacity, int batchSize)
        {
            if (capacity <= 0)
                throw new ArgumentOutOfRangeException(nameof(capacity), $"Capacity must be greater than zero.");

            if (batchSize <= 0)
                throw new ArgumentOutOfRangeException(nameof(batchSize), $"BatchSize must be at least 1.");

            // If configuration is identical, simply reset the state
            if (m_Capacity == capacity && m_BatchSize == batchSize)
            {
                Clear();
                return false;
            }

            m_Capacity = capacity;
            m_BatchSize = batchSize;

            // Structural re-allocation
            m_Mapper.Initialize(capacity);
            foreach (var store in m_AllStores)
            {
                store.Resize(capacity);
                store.UpdateBatchSize(batchSize);
            }

            return true;
        }

        #endregion

        #region Protected Accessors

        protected int SetA(TKey key, TStoreA value)
        {
            int index = m_Mapper.GetOrAllocate(key);
            StoreA.Set(index, value);
            return index;
        }

        protected int SetB(TKey key, TStoreB value)
        {
            int index = m_Mapper.GetOrAllocate(key);
            StoreB.Set(index, value);
            return index;
        }

        #endregion

        #region Public Management API

        /// <summary>
        /// Resets the registry by clearing the key-to-slot mapping and resetting all registered data stores.
        /// </summary>
        public virtual void Clear()
        {
            m_Mapper.Reset();
            foreach (var store in m_AllStores)
                store.Clear();
        }

        /// <summary>
        /// Resets the dirty tracking state for all registered stores.
        /// Call this after a successful SyncAllStores to acknowledge processed data.
        /// </summary>
        public void ClearDirtyState()
        {
            foreach (var store in m_AllStores)
                store.ClearDirty();
        }

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
            foreach (var store in m_AllStores)
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

            foreach (var store in m_AllStores)
            {
                store.UpdateBatchSize(newBatchSize);
            }

            return true;
        }

        #endregion

        #region Public Access API

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
        /// Releases the key and optionally resets the data in the underlying stores to prevent 'ghosting'.
        /// </summary>
        /// <param name="key">The key to release.</param>
        /// <param name="resetStoreA">If true, resets the value in StoreA. Defaults to true.</param>
        /// <param name="resetStoreB">If true, resets the value in StoreB. Defaults to true.</param>
        /// <returns>The index that was released, or -1 if the key was not found.</returns>
        public int ReleaseAndInvalidate(TKey key, bool resetStoreA = true, bool resetStoreB = true)
        {
            if (m_Mapper.TryGetIndex(key, out int index))
            {
                if (resetStoreA) StoreA.Set(index, default);
                if (resetStoreB) StoreB.Set(index, default);

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
        /// Tries to retrieve the current value of a specific type for a given key.
        /// </summary>
        /// <typeparam name="T">The store type to query (e.g., TStoreA or TStoreB).</typeparam>
        /// <param name="key">The key to look up.</param>
        /// <param name="value">The retrieved value if found; otherwise, default.</param>
        /// <returns>True if the key exists and the value was successfully retrieved.</returns>
        public bool TryGetValue<T>(TKey key, out T value)
            where T : unmanaged
        {
            value = default;

            if (!m_Mapper.TryGetIndex(key, out int index))
                return false;

            if (typeof(T) == typeof(TStoreA))
            {
                value = (T)(object)StoreA.Get(index);
                return true;
            }

            if (typeof(T) == typeof(TStoreB))
            {
                value = (T)(object)StoreB.Get(index);
                return true;
            }

            return false;
        }

        #endregion

        #region Iteration

        /// <summary>
        /// Executes a specialized action for each dirty segment of the specified store type.
        /// </summary>
        /// <remarks>
        /// By using a struct-based execution handler and aggressive inlining, this method 
        /// effectively eliminates all heap allocations and interface overhead during the 
        /// iteration of modified buffer segments.
        /// </remarks>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void ForEachDirtySegment<TAction>(ref TAction action, bool mergeContiguous = false)
            where TAction : struct, IExecutionHandler<SyncedSegmentMeta<TStoreA, TStoreB>>
        {
            if (!(StoreA.AnyDirty || StoreB.AnyDirty)) return;

            var syncedState = new SyncedDirtySegmentState<TStoreA, TStoreB>(
                StoreA.TypedBuffer, 
                StoreB.TypedBuffer,
                StoreA.DirtyBits,
                StoreB.DirtyBits,
                0,
                m_Capacity,
                m_BatchSize,
                1,
                mergeContiguous);

            var iter = new Iterator<SyncedSegmentMeta<TStoreA, TStoreB>, SyncedDirtySegmentState<TStoreA, TStoreB>>(syncedState);
            
            foreach(var batch in iter)
            {
                action.Execute(batch);
            }
        }

        /// <summary>
        /// Executes a specialized action for each dirty batch index of the specified store type.
        /// </summary>
        /// <remarks>
        /// This is the preferred method for rapid, index-based synchronization logic. 
        /// It leverages static type dispatch to ensure the iteration remains strictly on the stack.
        /// </remarks>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void ForEachDirtyIndex<TAction>(ref TAction action)
            where TAction : struct, IExecutionHandler<SyncedBitIteratorMeta>
        {
            if (!(StoreA.AnyDirty || StoreB.AnyDirty)) return;

            var syncedState = new SyncedBitIteratorState(StoreA.DirtyBits, StoreB.DirtyBits, 0, StoreA.TotalBatchCount);

            var it = new Iterator<SyncedBitIteratorMeta, SyncedBitIteratorState>(syncedState);

            while (it.MoveNext())
            {
                action.Execute(it.Current);
            }
        }

        /// <summary>
        /// Returns an iterator over contiguous dirty element ranges for the specified store type.
        /// </summary>
        /// <remarks>
        /// CAUTION: This method returns an interface, which causes BOXING of the internal iterator struct 
        /// onto the heap. For performance-critical synchronization loops, use <see cref="ForEachDirtySegment{T, TAction}"/> 
        /// to maintain stack-only execution.
        /// </remarks>
        public IIterator<SyncedSegmentMeta<TStoreA, TStoreB>> GetDirtySegmentIterator(bool mergeContiguous = true)
        {
            if (!(StoreA.AnyDirty || StoreB.AnyDirty)) return IIterator<SyncedSegmentMeta<TStoreA, TStoreB>>.Empty();

            var syncedState = new SyncedDirtySegmentState<TStoreA, TStoreB>(
                StoreA.TypedBuffer,
                StoreB.TypedBuffer,
                StoreA.DirtyBits,
                StoreB.DirtyBits,
                0,
                m_Capacity,
                m_BatchSize,
                1,
                mergeContiguous);

            var iter = new Iterator<SyncedSegmentMeta<TStoreA, TStoreB>, SyncedDirtySegmentState<TStoreA, TStoreB>>(syncedState);
            return iter;
        }

        /// <summary>
        /// Returns an iterator over the indices of all segments marked as modified for the specified store type.
        /// </summary>
        /// <remarks>
        /// CAUTION: This method returns an interface, which causes BOXING of the internal iterator struct 
        /// onto the heap. For high-frequency polling, use <see cref="ForEachDirtyIndex{T, TAction}"/> 
        /// to avoid memory pressure.
        /// </remarks>
        public IIterator<int> GetDirtySegmentIndices<T>()
            where T : unmanaged
        {
            if (typeof(T) == typeof(TStoreA))
                return StoreA.GetDirtySegmentIndices();

            if (typeof(T) == typeof(TStoreB))
                return StoreB.GetDirtySegmentIndices();

            return default;
        }

        #endregion

        #region IIterable<SyncedSegmentMeta<TStoreA, TStoreB>> Implementation

        /// <inheritdoc />
        public IIterator<SyncedSegmentMeta<TStoreA, TStoreB>> GetIterator()
        {
            var state = new SyncedSegmentState<TStoreA, TStoreB>(
                StoreA.TypedBuffer, 
                StoreB.TypedBuffer, 
                0,
                m_Capacity,
                m_BatchSize,
                1);

            return new Iterator<SyncedSegmentMeta<TStoreA, TStoreB>, SyncedSegmentState<TStoreA, TStoreB>>(state);
        }

        /// <inheritdoc />
        public void ForEach<TAction>(ref TAction action)
            where TAction : struct, IExecutionHandler<SyncedSegmentMeta<TStoreA, TStoreB>>
        {
            var state = new SyncedSegmentState<TStoreA, TStoreB>(
                StoreA.TypedBuffer,
                StoreB.TypedBuffer,
                0,
                m_Capacity,
                m_BatchSize,
                1);

            var iter = new Iterator<SyncedSegmentMeta<TStoreA, TStoreB>, SyncedSegmentState<TStoreA, TStoreB>>(state);
            
            while (iter.MoveNext())
            {
                action.Execute(iter.Current);
            }
        }

        #endregion
    }
    */
}