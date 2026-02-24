using Rayforge.Core.Collections.Abstractions;
using Rayforge.Core.Collections.Iterator;
using Rayforge.Core.Common.Rendering.Helpers;
using Rayforge.Core.Rendering.Abstractions;
using System;
using System.Collections;
using System.Runtime.InteropServices;

namespace Rayforge.Core.Rendering.Collections.Buffered
{
    /// <summary>
    /// A lightweight metadata container that manages a CPU-side array of shader data.
    /// Tracks modified segments (batches) to allow for optimized, partial GPU buffer updates.
    /// This is now a class to ensure stable references when managed by a Registry.
    /// </summary>
    /// <typeparam name="TValue">The unmanaged metadata struct (e.g., SpatialData or AtlasVisualData).</typeparam>
    public class MetadataStore<TValue> : IMetadataStore
        where TValue : unmanaged
    {
        private readonly TValue[] m_CpuData;
        private readonly BitArray m_DirtyBits;
        private readonly int m_BatchSize;
        private readonly int m_TotalBatches;
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
        /// Gets a value indicating whether any segments of the data have been modified.
        /// </summary>
        public bool AnyDirty => m_AnyDirty;

        /// <summary>
        /// Gets the underlying data as a raw Array.
        /// English comment: Use this for untyped operations like ComputeBuffer.SetData.
        /// </summary>
        public Array RawData => m_CpuData;

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

        /// <summary>
        /// Provides an iterator over all currently dirty batch indices.
        /// Allows external logic to inspect which segments are modified without allocations.
        /// </summary>
        public IIterator<int> GetDirtyBatchIndices()
        {
            var logic = new BitIteratorState(m_DirtyBits, m_TotalBatches);
            return new Iterator<int, BitIteratorState>(logic);
        }

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
        /// Group dirty batches into contiguous ranges and invoke the provided callback.
        /// Clears the dirty flags after processing.
        /// </summary>
        /// <param name="uploadCallback">Callback for (Array source, int start, int count).</param>
        public void ProcessDirtyBatches(Action<Array, int, int> uploadCallback)
        {
            if (!m_AnyDirty) return;

            int current = 0;
            while (current < m_TotalBatches)
            {
                if (!m_DirtyBits.Get(current))
                {
                    current++;
                    continue;
                }

                int startBatch = current;
                while (current < m_TotalBatches && m_DirtyBits.Get(current))
                {
                    current++;
                }
                int endBatch = current - 1;

                BufferMath.GetElementRange(startBatch, endBatch, m_BatchSize, m_CpuData.Length, out int start, out int count);

                uploadCallback?.Invoke(m_CpuData, start, count);
            }
        }

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
    }
}