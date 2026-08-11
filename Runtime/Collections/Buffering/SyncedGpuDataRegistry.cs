using Rayforge.Core.Collections.Abstractions;
using Rayforge.Core.Collections.Iterator;
using Rayforge.Core.Execution.Abstractions;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace Rayforge.Core.Collections.Buffering
{
    /// <summary>
    /// A high-performance registry managing exactly two distinct metadata stores.
    /// Provides type-safe access to store-specific buffers, centralized state management, 
    /// and optimized iteration patterns for synchronized data access.
    /// </summary>
    /// <typeparam name="TKey">The unique identifier type (e.g., Vector3Int).</typeparam>
    /// <typeparam name="TStoreA">The unmanaged data type for the first store.</typeparam>
    /// <typeparam name="TStoreB">The unmanaged data type for the second store.</typeparam>
    public class SyncedGpuDataRegistry<TKey, TStoreA, TStoreB> 
        : GpuDataRegistry<TKey>, IIterable<SyncedSegmentMeta<TStoreA, TStoreB>>
        where TKey : struct, IEquatable<TKey>
        where TStoreA : unmanaged, IGpuData<TStoreA>
        where TStoreB : unmanaged, IGpuData<TStoreB>
    {
        #region Properties

        /// <summary> The first metadata store instance. </summary>
        protected readonly MetadataStore<TStoreA> m_StoreA;
        /// <summary> The second metadata store instance. </summary>
        protected readonly MetadataStore<TStoreB> m_StoreB;

        /// <summary> Gets the metadata interface for the first metadata store. </summary>
        public IBufferMetadata StoreAMetadata => m_StoreA;

        /// <summary> Gets the metadata interface for the second metadata store. </summary>
        public IBufferMetadata StoreBMetadata => m_StoreB;

        /// <summary> Gets the raw buffer access for the first metadata store. </summary>
        public IRawBuffer<TStoreA> StoreARawBuffer => m_StoreA;

        /// <summary> Gets the raw buffer access for the second metadata store. </summary>
        public IRawBuffer<TStoreB> StoreBRawBuffer => m_StoreB;

        /// <summary> Gets the iterable interface for the first metadata store. </summary>
        public IIterable<TStoreA> StoreAIterable => m_StoreA;

        /// <summary> Gets the iterable interface for the second metadata store. </summary>
        public IIterable<TStoreB> StoreBIterable => m_StoreB;

        #endregion

        #region Constructor

        /// <summary> 
        /// Initializes a new registry and performs the initial allocation. 
        /// </summary>
        /// <exception cref="ArgumentOutOfRangeException">Thrown if capacity or batchSize is invalid.</exception>
        public SyncedGpuDataRegistry(int capacity, int batchSize)
            : base(capacity, batchSize)
        {
            m_StoreA = AddStore<TStoreA>();
            m_StoreB = AddStore<TStoreB>();
        }

        #endregion

        #region Public Access

        /// <summary>
        /// Sets the data for both metadata stores for the specified key.
        /// </summary>
        /// <param name="key">The unique key identifying the segment.</param>
        /// <param name="valA">The value to set in the first store.</param>
        /// <param name="valB">The value to set in the second store.</param>
        /// <returns>The index at which the data was stored.</returns>
        public int Set(TKey key, TStoreA valA, TStoreB valB)
        {
            int index = m_Mapper.GetOrAllocate(key);

            m_StoreA.Set(index, valA);
            m_StoreB.Set(index, valB);

            return index;
        }

        /// <summary>
        /// Retrieves the data from both stores for the specified key.
        /// </summary>
        /// <param name="key">The unique key identifying the segment.</param>
        /// <param name="valA">Output for the first store's value.</param>
        /// <param name="valB">Output for the second store's value.</param>
        /// <returns>True if the key was found, otherwise false.</returns>
        public bool TryGet(TKey key, out TStoreA valA, out TStoreB valB)
        {
            if (m_Mapper.TryGetIndex(key, out int index))
            {
                valA = m_StoreA.Get(index);
                valB = m_StoreB.Get(index);
                return true;
            }

            valA = default;
            valB = default;
            return false;
        }

        /// <summary>
        /// Retrieves the data from both stores. Throws if the key does not exist.
        /// </summary>
        public void Get(TKey key, out TStoreA valA, out TStoreB valB)
        {
            if (!TryGet(key, out valA, out valB))
            {
                throw new KeyNotFoundException($"Key {key} not found in registry.");
            }
        }

        /// <summary>
        /// Retrieves the value from the first metadata store at the index associated with the given key.
        /// </summary>
        public TStoreA GetStoreA(TKey key)
        {
            if (!m_Mapper.TryGetIndex(key, out int index))
                throw new KeyNotFoundException($"Key {key} not found.");
            return m_StoreA.Get(index);
        }

        /// <summary>
        /// Sets the value in the first metadata store for the given key.
        /// </summary>
        public void SetStoreA(TKey key, TStoreA value)
        {
            int index = m_Mapper.GetOrAllocate(key);
            m_StoreA.Set(index, value);
        }

        /// <summary>
        /// Retrieves the value from the second metadata store at the index associated with the given key.
        /// </summary>
        public TStoreB GetStoreB(TKey key)
        {
            if (!m_Mapper.TryGetIndex(key, out int index))
                throw new KeyNotFoundException($"Key {key} not found.");
            return m_StoreB.Get(index);
        }

        /// <summary>
        /// Sets the value in the second metadata store for the given key.
        /// </summary>
        public void SetStoreB(TKey key, TStoreB value)
        {
            int index = m_Mapper.GetOrAllocate(key);
            m_StoreB.Set(index, value);
        }

        /// <summary>
        /// Attempts to retrieve the value from the first metadata store at the index associated with the given key.
        /// </summary>
        /// <returns>True if the key was found, otherwise false.</returns>
        public bool TryGetStoreA(TKey key, out TStoreA value)
        {
            if (m_Mapper.TryGetIndex(key, out int index))
            {
                value = m_StoreA.Get(index);
                return true;
            }

            value = default;
            return false;
        }

        /// <summary>
        /// Attempts to retrieve the value from the second metadata store at the index associated with the given key.
        /// </summary>
        /// <returns>True if the key was found, otherwise false.</returns>
        public bool TryGetStoreB(TKey key, out TStoreB value)
        {
            if (m_Mapper.TryGetIndex(key, out int index))
            {
                value = m_StoreB.Get(index);
                return true;
            }

            value = default;
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
        public void ForEachSyncedDirtySegment<TAction>(ref TAction action)
            where TAction : struct, IExecutionHandler<SyncedSegmentMeta<TStoreA, TStoreB>>
        {
            if (!(m_StoreA.AnyDirty || m_StoreB.AnyDirty)) return;

            var syncedState = new SyncedDirtySegmentState<TStoreA, TStoreB>(
                m_StoreA.TypedBuffer, 
                m_StoreB.TypedBuffer,
                m_StoreA.DirtyBits,
                m_StoreB.DirtyBits,
                0,
                Capacity,
                BatchSize,
                1);

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
        public void ForEachSyncedDirtyIndex<TAction>(ref TAction action)
            where TAction : struct, IExecutionHandler<SyncedBitIteratorMeta>
        {
            if (!(m_StoreA.AnyDirty || m_StoreB.AnyDirty)) return;

            var syncedState = new SyncedBitIteratorState(m_StoreA.DirtyBits, m_StoreB.DirtyBits, 0, m_StoreA.TotalBatchCount);

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
        public IIterator<SyncedSegmentMeta<TStoreA, TStoreB>> GetSyncedDirtySegments()
        {
            if (!(m_StoreA.AnyDirty || m_StoreB.AnyDirty)) return IIterator<SyncedSegmentMeta<TStoreA, TStoreB>>.Empty();

            var syncedState = new SyncedDirtySegmentState<TStoreA, TStoreB>(
                m_StoreA.TypedBuffer,
                m_StoreB.TypedBuffer,
                m_StoreA.DirtyBits,
                m_StoreB.DirtyBits,
                0,
                Capacity,
                BatchSize,
                1);

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
        public IIterator<SyncedBitIteratorMeta> GetSyncedDirtyIndices<T>()
            where T : unmanaged
        {
            if (!(m_StoreA.AnyDirty || m_StoreB.AnyDirty)) return IIterator<SyncedBitIteratorMeta>.Empty();

            var syncedState = new SyncedBitIteratorState(m_StoreA.DirtyBits, m_StoreB.DirtyBits, 0, m_StoreA.TotalBatchCount);

            var it = new Iterator<SyncedBitIteratorMeta, SyncedBitIteratorState>(syncedState);
            return it;
        }

        #endregion

        #region IIterable<SyncedSegmentMeta<TStoreA, TStoreB>> Implementation

        /// <inheritdoc />
        public void ForEach<TAction>(ref TAction action)
            where TAction : struct, IExecutionHandler<SyncedSegmentMeta<TStoreA, TStoreB>>
        {
            var state = new SyncedSegmentState<TStoreA, TStoreB>(
                m_StoreA.TypedBuffer,
                m_StoreB.TypedBuffer,
                0,
                Capacity,
                BatchSize,
                1);

            var iter = new Iterator<SyncedSegmentMeta<TStoreA, TStoreB>, SyncedSegmentState<TStoreA, TStoreB>>(state);

            while (iter.MoveNext())
            {
                action.Execute(iter.Current);
            }
        }

        /// <inheritdoc />
        public IIterator<SyncedSegmentMeta<TStoreA, TStoreB>> GetIterator()
        {
            var state = new SyncedSegmentState<TStoreA, TStoreB>(
                m_StoreA.TypedBuffer, 
                m_StoreB.TypedBuffer, 
                0,
                Capacity,
                BatchSize,
                1);

            return new Iterator<SyncedSegmentMeta<TStoreA, TStoreB>, SyncedSegmentState<TStoreA, TStoreB>>(state);
        }

        #endregion
    }
}