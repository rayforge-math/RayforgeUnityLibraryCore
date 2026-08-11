using System;
using System.Collections;
using System.Collections.Generic;

namespace Rayforge.Core.Collections.Buffering
{
    /// <summary>
    /// Manages the occupancy of slots for an array-like buffer or resource pool.
    /// It purely tracks available indices using a stack for recycled slots and a linear counter 
    /// for new allocations. Structural and spatial math is deferred to external layout providers.
    /// </summary>
    public class LinearSlotAllocator
    {
        #region Properties

        private int m_NextLocalIndex = 0;
        private int m_Capacity;
        private int m_BaseOffset;
        private BitArray m_IsSlotFreed;
        private Stack<int> m_FreeSlots;

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
        /// The number of slots currently available in the recycle stack.
        /// </summary>
        public int RecycleCount => m_FreeSlots.Count;

        #endregion

        #region Init

        /// <summary>
        /// Initializes a new instance of the <see cref="LinearSlotAllocator"/> class.
        /// </summary>
        /// <param name="capacity">The initial maximum number of slots.</param>
        /// <param name="baseOffset">The global starting index for this allocator.</param>
        public LinearSlotAllocator(int capacity, int baseOffset = 0)
        {
            if (capacity <= 0)
                throw new ArgumentOutOfRangeException(nameof(capacity), "Capacity must be at least 1.");
            if (baseOffset < 0)
                throw new ArgumentOutOfRangeException(nameof(baseOffset), "BaseOffset cannot be negative.");

            m_Capacity = capacity;
            m_BaseOffset = baseOffset;

            m_IsSlotFreed = new BitArray(capacity);
            m_FreeSlots = new Stack<int>(capacity);
        }

        #endregion

        #region Public API

        /// <summary>
        /// Claims the next available slot index. 
        /// Priority is given to recycled indices from the free stack to maintain a compact footprint.
        /// </summary>
        /// <returns>A valid index within the range [0, Capacity - 1].</returns>
        /// <exception cref="OverflowException">Thrown when no slots are available in the current capacity.</exception>
        public int Acquire()
        {
            // 1. Priority: Recycled slots from the free stack
            if (m_FreeSlots.Count > 0)
            {
                int index = m_FreeSlots.Pop();

                // Mark as no longer free since we are handing it out
                m_IsSlotFreed[index - m_BaseOffset] = false;
                return index;
            }

            // 2. Linear allocation: Check if we still have room in the linear range
            if (m_NextLocalIndex >= m_Capacity)
                throw new OverflowException($"[LinearSlotAllocator] Capacity of {m_Capacity} exceeded. No slots available.");

            // Return the next linear slot and increment counter
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
            int localIndex = globalIndex - m_BaseOffset;

            // Bounds check
            if (localIndex < 0 || localIndex >= m_Capacity)
                return;

            if (localIndex >= m_NextLocalIndex)
                return;

            // Fast check (Idempotency)
            if (m_IsSlotFreed[localIndex])
                return;

            m_IsSlotFreed[localIndex] = true;
            m_FreeSlots.Push(globalIndex);
        }

        #endregion

        #region Management API

        /// <summary>
        /// Resets the allocator to its initial state.
        /// </summary>
        public void Reset()
        {
            m_NextLocalIndex = 0;
            m_FreeSlots.Clear();
            m_IsSlotFreed.SetAll(false);
        }

        #endregion
    }
}