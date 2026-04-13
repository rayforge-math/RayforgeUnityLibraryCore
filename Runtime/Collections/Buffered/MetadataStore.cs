using Rayforge.Core.Collections.Abstractions;
using Rayforge.Core.Collections.Helpers;
using Rayforge.Core.Collections.Iterator;
using Rayforge.Core.Execution.Abstractions;
using System;
using System.Collections;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Rayforge.Core.Collections.Buffered
{
    /// <summary>
    /// A lightweight metadata container that manages a CPU-side array of shader data.
    /// Tracks modified segments (batches) to allow for optimized, partial GPU buffer updates.
    /// This is now a class to ensure stable references when managed by a Registry.
    /// </summary>
    /// <typeparam name="TValue">The unmanaged metadata struct (e.g., SpatialData or AtlasVisualData).</typeparam>
    public sealed class MetadataStore<TValue> : IMetadataStoreController
        where TValue : unmanaged
    {
        #region Properties

        private TValue[] m_CpuData;
        private BitArray m_DirtyBits;
        private int m_BatchSize;
        private int m_TotalBatches;
        private bool m_AnyDirty;

        /// <summary>
        /// Gets the total capacity of the store.
        /// </summary>
        public int Capacity => m_CpuData.Length;

        /// <summary>
        /// Gets the byte size of a single element in the underlying data store.
        /// Directly used as the 'stride' parameter when creating a ComputeBuffer.
        /// </summary>
        public int Stride => Marshal.SizeOf(typeof(TValue));

        /// <summary>
        /// Gets the number of elements per dirty-tracking segment.
        /// </summary>
        public int BatchSize => m_BatchSize;

        /// <summary>
        /// Gets the total number of batches in the buffer.
        /// </summary>
        public int TotalBatchCount => m_TotalBatches;

        /// <summary>
        /// Gets a value indicating whether any segments of the data have been modified.
        /// </summary>
        public bool AnyDirty => m_AnyDirty;

        /// <summary>
        /// Gets the underlying data as a raw Array.
        /// English comment: Use this for untyped operations like ComputeBuffer.SetData.
        /// </summary>
        public Array RawData => m_CpuData;

        #endregion

        /// <summary>
        /// Initializes a new instance of the <see cref="MetadataStore{TValue}"/> class.
        /// </summary>
        /// <param name="capacity">The number of slots to manage.</param>
        /// <param name="batchSize">The size of one segment for dirty tracking.</param>
        public MetadataStore(int capacity, int batchSize)
        {
            if (capacity <= 0) throw new ArgumentException("Capacity must be positive.");

            m_CpuData = new TValue[capacity];
            m_BatchSize = Math.Max(1, batchSize);
            m_TotalBatches = BufferMath.GetTotalBatches(capacity, m_BatchSize);
            m_DirtyBits = new BitArray(m_TotalBatches);
            m_AnyDirty = false;
        }

        #region Public Configuration API (IMetadataStoreController Impl)

        /// <summary>
        /// Resizes the underlying data array to a new capacity.
        /// This is a destructive operation that clears all existing metadata.
        /// The current BatchSize is preserved and applied to the new capacity.
        /// </summary>
        /// <param name="newCapacity">The new maximum number of elements.</param>
        public void Resize(int newCapacity)
        {
            if (newCapacity <= 0) throw new ArgumentException("Capacity must be positive.");
            if (m_CpuData != null && m_CpuData.Length == newCapacity) return;

            m_CpuData = new TValue[newCapacity];

            m_TotalBatches = BufferMath.GetTotalBatches(newCapacity, m_BatchSize);

            m_DirtyBits = new BitArray(m_TotalBatches);
            m_AnyDirty = false;
        }

        /// <summary>
        /// Updates the batching logic without losing any data.
        /// Use this for performance tuning at runtime.
        /// </summary>
        public void UpdateBatchSize(int newBatchSize)
        {
            newBatchSize = Math.Max(1, newBatchSize);
            if (m_BatchSize == newBatchSize) return;

            int oldBatchSize = m_BatchSize;
            int oldTotal = m_TotalBatches;
            BitArray oldBits = m_DirtyBits;

            m_BatchSize = newBatchSize;
            m_TotalBatches = BufferMath.GetTotalBatches(m_CpuData.Length, m_BatchSize);
            m_DirtyBits = new BitArray(m_TotalBatches);

            if (m_AnyDirty)
            {
                for (int i = 0; i < oldTotal; i++)
                {
                    if (oldBits.Get(i))
                    {
                        BufferMath.GetElementRange(i, i, oldBatchSize, m_CpuData.Length, out int start, out int count);
                        int firstNew = BufferMath.GetBatchIndex(start, m_BatchSize);
                        int lastNew = BufferMath.GetBatchIndex(start + count - 1, m_BatchSize);

                        for (int n = firstNew; n <= lastNew; n++)
                            m_DirtyBits.Set(n, true);
                    }
                }
            }
        }

        #endregion

        #region Public Management API

        /// <summary>
        /// Marks a specific index as dirty without changing its value.
        /// </summary>
        /// <param name="index">The index to mark as dirty.</param>
        public void MarkDirty(int index)
        {
            int batchIndex = BufferMath.GetBatchIndex(index, m_BatchSize);
            m_DirtyBits.Set(batchIndex, true);
            m_AnyDirty = true;
        }

        /// <summary>
        /// Marks the entire store as dirty. This ensures all segments will be 
        /// uploaded to the GPU during the next synchronization pass.
        /// Useful for buffer initialization or recovering from graphics context loss.
        /// </summary>
        public void MarkAllDirty()
        {
            m_DirtyBits.SetAll(true);
            m_AnyDirty = true;
        }

        /// <summary>
        /// Clears all dirty segment markers. Call this after the GPU buffers have been updated.
        /// </summary>
        public void ClearDirty()
        {
            m_DirtyBits.SetAll(false);
            m_AnyDirty = false;
        }

        /// <summary>
        /// Returns the internal CPU array for direct access (e.g., for SetData).
        /// </summary>
        /// <returns>The raw CPU-side data array.</returns>
        public TValue[] GetInternalArray() => m_CpuData;

        /// <summary>
        /// Resets the store by zeroing the CPU array and clearing all dirty tracking markers.
        /// Ensures no old data remains and the GPU synchronization starts fresh.
        /// </summary>
        public void Clear()
        {
            Array.Clear(m_CpuData, 0, m_CpuData.Length);
            ClearDirty();
        }

        #endregion

        #region Public Access API

        /// <summary>
        /// Sets the metadata for a specific index and marks the corresponding batch as dirty.
        /// </summary>
        /// <param name="index">The slot index (usually provided by a SlotMapper).</param>
        /// <param name="value">The metadata value to store.</param>
        public void Set(int index, TValue value)
        {
            m_CpuData[index] = value;
            MarkDirty(index);
        }

        /// <summary>
        /// Retrieves the metadata for a specific index.
        /// </summary>
        /// <param name="index">The index to look up.</param>
        /// <returns>The stored metadata value.</returns>
        public TValue Get(int index) => m_CpuData[index];

        /// <summary>
        /// Executes a specialized action for every single element in the store.
        /// <para>
        /// PERFORMANCE: This is the fastest way to perform full-buffer operations. By using a struct constraint 
        /// and ref parameter, the JIT compiler can inline the action and the array traversal logic directly, 
        /// resulting in zero-allocation, stack-only execution.
        /// </para>
        /// </summary>
        /// <typeparam name="TAction">A struct implementing the <see cref="IExecutionHandler{TValue}"/> contract.</typeparam>
        /// <param name="action">The action to execute for each element.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void ForEach<TAction>(ref TAction action)
            where TAction : struct, IExecutionHandler<TValue>
        {
            if (m_CpuData == null || m_CpuData.Length == 0) return;

            var scanner = GetElementScanner();
            var it = new Iterator<TValue, ArrayIteratorState<TValue>>(scanner);

            while (it.MoveNext())
            {
                action.Execute(it.Current);
            }
        }

        /// <summary>
        /// Provides a linear iterator over all elements in the store.
        /// <para>
        /// CAUTION: Returning the iterator as an interface type causes BOXING of the internal struct state.
        /// Use <see cref="ForEach{TAction}"/> for performance-critical loops to keep the operation on the stack.
        /// </para>
        /// </summary>
        /// <returns>A boxed iterator instance or an empty iterator if no data is present.</returns>
        public IIterator<TValue> GetIterator()
        {
            if (m_CpuData == null || m_CpuData.Length == 0)
            {
                return IIterator<TValue>.Empty();
            }

            var logic = new ArrayIteratorState<TValue>(m_CpuData, 0, m_CpuData.Length);
            return new Iterator<TValue, ArrayIteratorState<TValue>>(logic);
        }

        #endregion

        #region Iteration (IMetadataStore Impl)

        /// <summary>
        /// Executes a specialized action for each dirty segment range.
        /// <para>
        /// PERFORMANCE: This is the fastest way to iterate. By using a struct constraint and ref parameter,
        /// the JIT compiler can inline the action and the scanner logic directly, 
        /// resulting in zero-allocation, stack-only execution.
        /// </para>
        /// </summary>
        /// <typeparam name="TAction">A struct implementing the <see cref="IExecutionHandler{BufferSegmentMeta}"/> contract.</typeparam>
        /// <param name="action">The action to execute for each dirty segment.</param>
        /// <param name="mergeContiguous">If true, contiguous dirty batches are merged into a single segment to optimize GPU commands.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void ForEachDirtySegment<TAction>(ref TAction action, bool mergeContiguous = true)
            where TAction : struct, IExecutionHandler<BufferSegmentMeta>
        {
            if (!m_AnyDirty) return;

            var scanner = GetDirtySegmentScanner(mergeContiguous);
            var it = new Iterator<BufferSegmentMeta, DirtySegmentIteratorState>(scanner);

            while (it.MoveNext())
            {
                action.Execute(it.Current);
            }
        }

        /// <summary>
        /// Executes a specialized action for each individual dirty batch index.
        /// <para>
        /// PERFORMANCE: Optimized for high-frequency calls. Avoids boxing and ensures 
        /// the entire iteration state remains on the stack.
        /// </para>
        /// </summary>
        /// <typeparam name="TAction">A struct implementing the <see cref="IExecutionHandler{int}"/> contract.</typeparam>
        /// <param name="action">The action to execute for each dirty index.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void ForEachDirtyIndex<TAction>(ref TAction action)
            where TAction : struct, IExecutionHandler<int>
        {
            if (!m_AnyDirty) return;

            var scanner = new BitIteratorState(m_DirtyBits, 0, m_TotalBatches);
            var it = new Iterator<int, BitIteratorState>(scanner);

            while (it.MoveNext())
            {
                action.Execute(it.Current);
            }
        }

        /// <summary>
        /// Executes a specialized action for every batch in the store, regardless of its dirty state.
        /// <para>
        /// PERFORMANCE: Zero-allocation, stack-only execution via struct inlining. 
        /// This is the most efficient way to process the entire store in segmented chunks.
        /// </para>
        /// </summary>
        /// <typeparam name="TAction">A struct implementing <see cref="IExecutionHandler{BufferSegmentMeta}"/>.</typeparam>
        /// <param name="action">The action to execute for each batch segment.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void ForEachSegment<TAction>(ref TAction action)
            where TAction : struct, IExecutionHandler<BufferSegmentMeta>
        {
            var scanner = GetSegmentScanner();
            var it = new Iterator<BufferSegmentMeta, SegmentIteratorState>(scanner);

            while (it.MoveNext())
            {
                action.Execute(it.Current);
            }
        }

        /// <summary>
        /// Provides a specialized iterator over contiguous dirty element ranges.
        /// <para>
        /// CAUTION: Returning the iterator as an interface type causes BOXING of the internal struct state.
        /// Use <see cref="ForEachDirtySegment{TAction}"/> for performance-critical synchronization loops.
        /// </para>
        /// </summary>
        /// <param name="mergeContiguous">If true, contiguous dirty batches are merged into a single segment.</param>
        /// <returns>A boxed iterator instance or an empty one if no segments are dirty.</returns>
        public IIterator<BufferSegmentMeta> GetDirtySegmentIterator(bool mergeContiguous = true)
        {
            if (!m_AnyDirty)
            {
                return default;
            }

            var scanner = GetDirtySegmentScanner(mergeContiguous);
            return new Iterator<BufferSegmentMeta, DirtySegmentIteratorState>(scanner);
        }

        /// <summary>
        /// Returns an iterator over the indices of all segments marked as modified.
        /// <para>
        /// CAUTION: This method causes the internal iterator struct to be BOXED onto the heap.
        /// Use <see cref="ForEachDirtyIndex{TAction}"/> to keep the operation on the stack.
        /// </para>
        /// </summary>
        /// <returns>A boxed iterator instance or an empty one if no indices are dirty.</returns>
        public IIterator<int> GetDirtySegmentIndices()
        {
            if (!m_AnyDirty)
            {
                return default;
            }

            var logic = new BitIteratorState(m_DirtyBits, 0, m_TotalBatches);
            return new Iterator<int, BitIteratorState>(logic);
        }

        /// <summary>
        /// Provides a specialized iterator over all subsequent element ranges.
        /// <para>
        /// CAUTION: Returning the iterator as an interface type causes BOXING of the internal struct state.
        /// Use <see cref="ForEachSegment{TAction}"/> for performance-critical synchronization loops.
        /// </para>
        /// </summary>
        /// <returns>A boxed iterator instance or an empty one if no segments are present.</returns>
        public IIterator<BufferSegmentMeta> GetSegmentIterator()
        {
            if (!m_AnyDirty)
            {
                return default;
            }

            var scanner = GetSegmentScanner();
            return new Iterator<BufferSegmentMeta, SegmentIteratorState>(scanner);
        }

        #endregion

        #region Internal Helpers

        /// <summary>
        /// Provides the raw scanner state for dirty segments.
        /// Use this for high-performance composition (e.g., in a Registry) 
        /// to avoid heap allocations and interface overhead.
        /// </summary>
        /// <param name="mergeContiguous">If true, contiguous dirty batches are merged into segments.</param>
        /// <returns>A stack-allocated state struct ready for iteration.</returns>
        internal DirtySegmentIteratorState GetDirtySegmentScanner(bool mergeContiguous = false)
        {
            return new DirtySegmentIteratorState(
                m_CpuData,
                m_DirtyBits,
                m_BatchSize,
                m_CpuData.Length,
                mergeContiguous
            );
        }

        /// <summary>
        /// Provides the raw scanner state for all segments.
        /// Use this for high-performance composition (e.g., in a Registry) 
        /// to avoid heap allocations and interface overhead.
        /// </summary>
        /// <returns>A stack-allocated state struct ready for iteration.</returns>
        internal SegmentIteratorState GetSegmentScanner()
        {
            return new SegmentIteratorState(
                m_CpuData,
                m_BatchSize,
                m_CpuData.Length
            );
        }

        /// <summary>
        /// Provides the raw scanner state for single elements.
        /// Use this for high-performance composition to avoid heap allocations and interface overhead.
        /// </summary>
        /// <returns>A stack-allocated state struct ready for iteration.</returns>
        internal ArrayIteratorState<TValue> GetElementScanner()
        {
            return new ArrayIteratorState<TValue>(
                m_CpuData,
                0,
                m_CpuData.Length
            );
        }

        #endregion
    }
}