using System;
using System.Collections.Generic;

namespace Rayforge.Core.Rendering.Collections.Buffered
{
    /// <summary>
    /// A lightweight index broker for GPU resource management. 
    /// Maps unique keys to stable indices and tracks modified segments (batches) for optimized GPU buffer uploads.
    /// </summary>
    /// <typeparam name="TKey">The type of the unique identifier (must be a struct and equatable).</typeparam>
    public struct KeyedSlotMapper<TKey> where TKey : struct, IEquatable<TKey>
    {
        private readonly Dictionary<TKey, int> m_KeyToSlot;
        private readonly Stack<int> m_ReuseStack;
        private readonly int m_Capacity;

        private int m_NextAvailableIndex;

        /// <summary>
        /// Gets the total number of available slots.
        /// </summary>
        public int Capacity => m_Capacity;

        /// <summary>
        /// Gets a value indicating whether the mapper has been properly initialized.
        /// </summary>
        public bool IsInitialized => m_KeyToSlot != null;

        /// <summary>
        /// Initializes a new instance of the <see cref="KeyedSlotMapper{TKey}"/> struct.
        /// </summary>
        /// <param name="capacity">The maximum number of slots to manage.</param>
        public KeyedSlotMapper(int capacity)
        {
            m_Capacity = capacity;
            m_KeyToSlot = new Dictionary<TKey, int>(capacity);
            m_ReuseStack = new Stack<int>();
            m_NextAvailableIndex = 0;
        }

        /// <summary>
        /// Retrieves the stable index for a given key. If the key is new, a new index is allocated.
        /// </summary>
        /// <param name="key">The unique key to map.</param>
        /// <returns>The allocated or existing index for the key.</returns>
        /// <exception cref="InvalidOperationException">Thrown if the mapper is not initialized or capacity is reached.</exception>
        public int GetOrAllocate(TKey key)
        {
            if (!IsInitialized) throw new InvalidOperationException("Mapper not initialized.");

            if (m_KeyToSlot.TryGetValue(key, out int index))
            {
                return index;
            }

            if (m_ReuseStack.Count > 0)
            {
                index = m_ReuseStack.Pop();
            }
            else if (m_NextAvailableIndex < m_Capacity)
            {
                index = m_NextAvailableIndex++;
            }
            else
            {
                throw new InvalidOperationException("Mapper capacity reached.");
            }

            m_KeyToSlot[key] = index;
            return index;
        }

        /// <summary>
        /// Releases the index associated with the key back to the reuse pool.
        /// </summary>
        /// <param name="key">The key to release.</param>
        public void Release(TKey key)
        {
            if (!IsInitialized) return;

            if (m_KeyToSlot.Remove(key, out int index))
            {
                m_ReuseStack.Push(index);
            }
        }

        /// <summary>
        /// Tries to get the current index for a specific key without allocating a new one.
        /// </summary>
        /// <param name="key">The key to look up.</param>
        /// <param name="index">The found index, or -1 if not found.</param>
        /// <returns>True if the key was found; otherwise, false.</returns>
        public bool TryGetIndex(TKey key, out int index)
        {
            if (!IsInitialized)
            {
                index = -1;
                return false;
            }
            return m_KeyToSlot.TryGetValue(key, out index);
        }
    }
}