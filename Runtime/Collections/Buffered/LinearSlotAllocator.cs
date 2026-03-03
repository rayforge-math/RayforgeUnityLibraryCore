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
        private int m_NextAvailableIndex = 0;
        private int m_Capacity;
        private readonly Stack<int> m_FreeSlots = new();

        /// <summary>
        /// The current maximum number of slots this allocator can manage.
        /// </summary>
        public int Capacity => m_Capacity;

        /// <summary>
        /// The number of slots currently available for acquisition (recycled + remaining linear space).
        /// </summary>
        public int AvailableCount => m_FreeSlots.Count + (m_Capacity - m_NextAvailableIndex);

        /// <summary>
        /// Initializes a new instance of the <see cref="LinearSlotAllocator"/> class.
        /// </summary>
        /// <param name="capacity">The initial maximum number of slots.</param>
        public LinearSlotAllocator(int capacity)
        {
            m_Capacity = capacity;
        }

        /// <summary>
        /// Updates the capacity and resets the allocator state.
        /// Use this to resize resource pools without re-allocating the allocator instance or 
        /// triggering Garbage Collection for the internal stack.
        /// </summary>
        /// <param name="newCapacity">The new maximum capacity of the allocator.</param>
        public void Reconfigure(int newCapacity)
        {
            m_Capacity = newCapacity;
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

            if (m_NextAvailableIndex >= m_Capacity)
                throw new OverflowException($"[LinearSlotAllocator] Capacity of {m_Capacity} exceeded. No slots available.");

            return m_NextAvailableIndex++;
        }

        /// <summary>
        /// Returns an index to the pool, making it available for future <see cref="Acquire"/> calls.
        /// </summary>
        /// <param name="index">The slot index to release.</param>
        /// <remarks>
        /// Ensure the index being released was previously acquired and is not already in the free pool.
        /// </remarks>
        public void Release(int index)
        {
            if (index < 0 || index >= m_Capacity)
                return;

            m_FreeSlots.Push(index);
        }

        /// <summary>
        /// Resets the allocator to its initial state.
        /// All indices are marked as available, and the internal free stack is cleared.
        /// </summary>
        public void Reset()
        {
            m_NextAvailableIndex = 0;
            m_FreeSlots.Clear();
        }
    }
}