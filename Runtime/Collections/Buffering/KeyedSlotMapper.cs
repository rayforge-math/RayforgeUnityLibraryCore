using System;
using System.Collections.Generic;

namespace Rayforge.Core.Collections.Buffering
{
    /// <summary>
    /// A lightweight index broker for GPU resource management. 
    /// Maps unique keys to stable indices and tracks modified segments (batches) for optimized GPU buffer uploads.
    /// </summary>
    /// <typeparam name="TKey">The type of the unique identifier (must be a struct and equatable).</typeparam>
    public class KeyedSlotMapper<TKey> where TKey : struct, IEquatable<TKey>
    {
        #region Properties

        private readonly Dictionary<TKey, int> m_KeyToSlot = new();
        private readonly Stack<int> m_ReuseStack = new();
        private int m_Capacity;

        private int m_NextAvailableIndex;

        /// <summary>
        /// Gets the total number of available slots.
        /// </summary>
        public int Capacity => m_Capacity;

        /// <summary>
        /// Gets the number of currently active mappings.
        /// </summary>
        public int Count => m_KeyToSlot?.Count ?? 0;

        /// <summary>
        /// Gets the highest index currently in use. 
        /// This is the "high-water mark" for GPU buffer uploads.
        /// </summary>
        public int HighestActiveIndex => m_NextAvailableIndex;

        /// <summary>
        /// Gets a value indicating whether the mapper has been properly initialized.
        /// </summary>
        public bool IsInitialized => m_Capacity > 0;

        #endregion

        #region Init

        /// <summary>
        /// Initializes a new instance of the <see cref="KeyedSlotMapper{TKey}"/> class.
        /// </summary>
        public KeyedSlotMapper() { }

        /// <summary>
        /// Initializes a new instance of the <see cref="KeyedSlotMapper{TKey}"/> class with a specific capacity.
        /// </summary>
        /// <param name="initialCapacity">The maximum number of slots. Must be greater than zero.</param>
        /// <exception cref="ArgumentOutOfRangeException">Thrown if initialCapacity is less than or equal to zero.</exception>
        public KeyedSlotMapper(int initialCapacity)
        {
            Initialize(initialCapacity);
        }

        /// <summary>
        /// Reconfigures the mapper with a new capacity.
        /// Completely clears all mappings and rebuilds the internal structures.
        /// </summary>
        /// <param name="newCapacity">The new maximum number of slots.</param>
        public void Initialize(int newCapacity)
        {
            if (newCapacity <= 0)
                throw new ArgumentOutOfRangeException(nameof(newCapacity), "Capacity must be greater than zero.");

            m_Capacity = newCapacity;
            Reset();
        }

        #endregion

        #region Management

        /// <summary>
        /// Resets the mapper by clearing all active mappings and returning all indices to the pool.
        /// Clears the dictionary and reuse stack, and resets the high-water mark.
        /// </summary>
        public void Reset()
        {
            if (!IsInitialized) return;

            m_KeyToSlot.Clear();
            m_ReuseStack.Clear();

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
            
            var valid = m_KeyToSlot.TryGetValue(key, out index);

            if (valid)
            {
                return true;
            }
            else
            {
                index = -1;
                return false;
            }
        }

        #endregion
    }
}