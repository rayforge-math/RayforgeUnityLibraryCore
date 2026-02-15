using System;
using System.Collections.Generic;

namespace Rayforge.Core.Rendering.Collections.Buffered
{
    /// <summary>
    /// A lightweight metadata container that manages a CPU-side array of shader data.
    /// Tracks modified segments (batches) to allow for optimized, partial GPU buffer updates.
    /// This is now a class to ensure stable references when managed by a Registry.
    /// </summary>
    /// <typeparam name="TValue">The unmanaged metadata struct (e.g., SpatialData or AtlasVisualData).</typeparam>
    public class MetadataStore<TValue> where TValue : unmanaged
    {
        private readonly TValue[] m_CpuData;
        private readonly HashSet<int> m_DirtyBatches;
        private readonly int m_BatchSize;

        /// <summary>
        /// Gets the total capacity of the store.
        /// </summary>
        public int Capacity => m_CpuData.Length;

        /// <summary>
        /// Gets the number of elements per dirty-tracking segment.
        /// </summary>
        public int BatchSize => m_BatchSize;

        /// <summary>
        /// Gets a value indicating whether any segments of the data have been modified.
        /// </summary>
        public bool AnyDirty => m_DirtyBatches.Count > 0;

        /// <summary>
        /// Gets the collection of batch indices that require a GPU upload.
        /// </summary>
        public IReadOnlyCollection<int> DirtyBatches => m_DirtyBatches;

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
            m_DirtyBatches = new HashSet<int>();
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
        /// Marks a specific index as dirty without changing its value.
        /// </summary>
        /// <param name="index">The index to mark as dirty.</param>
        public void MarkDirty(int index)
        {
            m_DirtyBatches.Add(index / m_BatchSize);
        }

        /// <summary>
        /// Marks the entire store as dirty. This ensures all segments will be 
        /// uploaded to the GPU during the next synchronization pass.
        /// Useful for buffer initialization or recovering from graphics context loss.
        /// </summary>
        public void MarkAllDirty()
        {
            int totalBatches = (m_CpuData.Length + m_BatchSize - 1) / m_BatchSize;

            for (int i = 0; i < totalBatches; i++)
            {
                m_DirtyBatches.Add(i);
            }
        }

        /// <summary>
        /// Clears all dirty segment markers. Call this after the GPU buffers have been updated.
        /// </summary>
        public void ClearDirty()
        {
            m_DirtyBatches.Clear();
        }

        /// <summary>
        /// Returns the internal CPU array for direct access (e.g., for SetData).
        /// </summary>
        /// <returns>The raw CPU-side data array.</returns>
        public TValue[] GetInternalArray() => m_CpuData;
    }
}