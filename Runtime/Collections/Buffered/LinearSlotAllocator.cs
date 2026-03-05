using System;
using System.Collections.Generic;

namespace Rayforge.Core.Collections.Buffered
{
    /// <summary>
    /// Manages the occupancy of slots for an array-like buffer or resource pool.
    /// It purely tracks available indices using a stack for recycled slots and a linear counter 
    /// for new allocations. Structural and spatial math is deferred to external layout providers.
    /// </summary>
    public class LinearSlotAllocator
    {
        private int m_NextLocalIndex = 0;
        private int m_Capacity;
        private int m_BaseOffset;
        private readonly Stack<int> m_FreeSlots = new();

        /// <summary>
        /// The current maximum number of slots this allocator can manage.
        /// </summary>
        public int Capacity => m_Capacity;

        /// <summary>
        /// The global start index for this allocator's range.
        /// </summary>
        public int BaseOffset => m_BaseOffset;

        /// <summary>
        /// The number of slots currently available for acquisition (recycled + remaining linear space).
        /// </summary>
        public int AvailableCount => m_FreeSlots.Count + (m_Capacity - m_NextLocalIndex);

        /// <summary>
        /// Initializes a new instance of the <see cref="LinearSlotAllocator"/> class.
        /// </summary>
        /// <param name="capacity">The initial maximum number of slots.</param>
        /// <param name="baseOffset">The global starting index for this allocator.</param>
        public LinearSlotAllocator(int capacity, int baseOffset = 0)
        {
            m_Capacity = capacity;
            m_BaseOffset = baseOffset;
        }

        /// <summary>
        /// Updates the capacity and base offset, then resets the allocator state.
        /// </summary>
        /// <param name="newCapacity">The new maximum capacity of the allocator.</param>
        /// <param name="newBaseOffset">The new global starting index.</param>
        public void Reconfigure(int newCapacity, int newBaseOffset)
        {
            m_Capacity = newCapacity;
            m_BaseOffset = newBaseOffset;
            Reset();
        }

        /// <summary>
        /// Claims the next available slot index. 
        /// Priority is given to recycled indices from the free stack to maintain a compact footprint.
        /// </summary>
        /// <returns>A valid index within the range [0, Capacity - 1].</returns>
        /// <exception cref="OverflowException">Thrown when no slots are available in the current capacity.</exception>
        public int Acquire()
        {
            if (m_FreeSlots.Count > 0)
                return m_FreeSlots.Pop();

            if (m_NextLocalIndex >= m_Capacity)
                throw new OverflowException($"[LinearSlotAllocator] Capacity of {m_Capacity} exceeded. No slots available.");

            return m_BaseOffset + (m_NextLocalIndex++);
        }

        /// <summary>
        /// Returns a global index to the pool.
        /// </summary>
        /// <param name="globalIndex">The global slot index to release.</param>
        /// <remarks>
        /// The index is stored directly to avoid re-calculation during next acquisition.
        /// </remarks>
        public void Release(int globalIndex)
        {
            if (globalIndex < m_BaseOffset || globalIndex >= m_BaseOffset + m_Capacity)
            {
                return;
            }

            m_FreeSlots.Push(globalIndex);
        }

        /// <summary>
        /// Resets the allocator to its initial state.
        /// </summary>
        public void Reset()
        {
            m_NextLocalIndex = 0;
            m_FreeSlots.Clear();
        }
    }
}