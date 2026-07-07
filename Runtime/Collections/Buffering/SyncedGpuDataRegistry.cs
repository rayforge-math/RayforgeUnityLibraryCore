using Rayforge.Core.Collections.Abstractions;
using Rayforge.Core.Collections.Iterator;
using Rayforge.Core.Execution.Abstractions;
using System;
using System.Runtime.CompilerServices;

namespace Rayforge.Core.Collections.Buffering
{
    /// <summary>
    /// An abstract base class for a registry managing exactly two distinct metadata stores.
    /// Provides high-performance, type-safe access to store-specific buffers and centralized state management.
    /// </summary>
    /// <typeparam name="TKey">The unique identifier type (e.g., Vector3Int).</typeparam>
    /// <typeparam name="TStoreA">The unmanaged data type for the first store.</typeparam>
    /// <typeparam name="TStoreB">The unmanaged data type for the second store.</typeparam>
    public abstract class SyncedGpuDataRegistry<TKey, TStoreA, TStoreB> 
        : GpuDataRegistry<TKey>, IIterable<SyncedSegmentMeta<TStoreA, TStoreB>>
        where TKey : struct, IEquatable<TKey>
        where TStoreA : unmanaged, IGpuData<TStoreA>
        where TStoreB : unmanaged, IGpuData<TStoreB>
    {
        #region Properties

        private readonly IMetadataController[] m_AllStores;

        /// <summary> The first metadata store instance. </summary>
        protected readonly MetadataStore<TStoreA> StoreA;
        /// <summary> The second metadata store instance. </summary>
        protected readonly MetadataStore<TStoreB> StoreB;

        #endregion

        #region Constructor

        /// <summary> 
        /// Initializes a new registry and performs the initial allocation. 
        /// </summary>
        /// <exception cref="ArgumentOutOfRangeException">Thrown if capacity or batchSize is invalid.</exception>
        public SyncedGpuDataRegistry(int capacity, int batchSize)
            : base(capacity, batchSize)
        {
            StoreA = AddStore<TStoreA>();
            StoreB = AddStore<TStoreB>();

            m_AllStores = new IMetadataController[] { StoreA, StoreB };
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
        public void ForEachDirtySegment<TAction>(ref TAction action)
            where TAction : struct, IExecutionHandler<SyncedSegmentMeta<TStoreA, TStoreB>>
        {
            if (!(StoreA.AnyDirty || StoreB.AnyDirty)) return;

            var syncedState = new SyncedDirtySegmentState<TStoreA, TStoreB>(
                StoreA.TypedBuffer, 
                StoreB.TypedBuffer,
                StoreA.DirtyBits,
                StoreB.DirtyBits,
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
        public IIterator<SyncedSegmentMeta<TStoreA, TStoreB>> GetDirtySegmentIterator()
        {
            if (!(StoreA.AnyDirty || StoreB.AnyDirty)) return IIterator<SyncedSegmentMeta<TStoreA, TStoreB>>.Empty();

            var syncedState = new SyncedDirtySegmentState<TStoreA, TStoreB>(
                StoreA.TypedBuffer,
                StoreB.TypedBuffer,
                StoreA.DirtyBits,
                StoreB.DirtyBits,
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
                Capacity,
                BatchSize,
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
                Capacity,
                BatchSize,
                1);

            var iter = new Iterator<SyncedSegmentMeta<TStoreA, TStoreB>, SyncedSegmentState<TStoreA, TStoreB>>(state);
            
            while (iter.MoveNext())
            {
                action.Execute(iter.Current);
            }
        }

        #endregion
    }
}