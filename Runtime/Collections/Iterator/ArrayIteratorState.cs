using Rayforge.Core.Collections.Abstractions;
using System;

namespace Rayforge.Core.Collections.Iterator
{
    /// <summary>
    /// Optimized iteration logic for raw arrays. 
    /// Avoids the overhead of IEnumerator and works directly with indices.
    /// </summary>
    /// <typeparam name="T">The element type.</typeparam>
    public struct ArrayIteratorState<T> : IIterationLogic<T, ArrayIteratorState<T>>
    {
        private readonly T[] _array;
        private readonly int _end;
        private int _index;

        /// <summary>
        /// Initializes the state with a target array and a specific range.
        /// </summary>
        /// <param name="array">The source array.</param>
        /// <param name="start">The starting index.</param>
        /// <param name="count">The number of elements to iterate.</param>
        public ArrayIteratorState(T[] array, int start, int count)
        {
            _array = array;

            if (array == null || array.Length == 0)
            {
                _index = -1;
                _end = 0;
                return;
            }

            int clampedStart = Math.Max(0, Math.Min(start, array.Length));
            int maxRemaining = array.Length - clampedStart;
            int clampedCount = Math.Max(0, Math.Min(count, maxRemaining));

            _index = clampedStart - 1;
            _end = clampedStart + clampedCount;
        }

        /// <summary>
        /// Non-destructive check if more elements are available in the specified range.
        /// </summary>
        /// <param name="self">Reference to the current state.</param>
        /// <returns>True if the next index is within bounds; false otherwise.</returns>
        public bool HasNext(ref ArrayIteratorState<T> self)
        {
            return IsValid(ref self, self._index + 1);
        }

        /// <summary>
        /// Peeks at the next element (+1) without advancing the internal pointer.
        /// Critical for synchronization between multiple array-based streams.
        /// </summary>
        public bool TryPeekNext(ref ArrayIteratorState<T> self, out T result)
        {
            int nextIndex = self._index + 1;
            if (IsValid(ref self, nextIndex))
            {
                result = self._array[nextIndex];
                return true;
            }

            result = default;
            return false;
        }

        /// <summary>
        /// Advances the internal index and retrieves the next element.
        /// </summary>
        /// <param name="self">Reference to the current state.</param>
        /// <param name="result">The element at the new index.</param>
        /// <returns>True if an element was retrieved; false if the end was reached.</returns>
        public bool MoveNext(ref ArrayIteratorState<T> self, out T result)
        {
            self._index++;

            if (IsValid(ref self, self._index))
            {
                result = self._array[self._index];
                return true;
            }

            result = default;
            return false;
        }

        /// <summary>
        /// Centralized bounds check. 
        /// Static to ensure no accidental 'this' capture and to encourage inlining.
        /// </summary>
        private static bool IsValid(ref ArrayIteratorState<T> self, int index)
        {
            return self._array != null && index >= 0 && index < self._end && index < self._array.Length;
        }
    }
}