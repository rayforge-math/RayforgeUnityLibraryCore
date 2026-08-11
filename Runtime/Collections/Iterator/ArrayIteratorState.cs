using Rayforge.Core.Collections.Abstractions;
using System;

namespace Rayforge.Core.Collections.Iterator
{
    /// <summary>
    /// Optimized iteration logic for raw arrays.
    /// <para>
    /// Provides zero-allocation, index-based iteration over an array. 
    /// Designed for compatibility with Unity/IL2CPP by avoiding ref-struct constraints.
    /// </para>
    /// </summary>
    /// <typeparam name="T">The element type.</typeparam>
    public struct ArrayIteratorState<T> : IIterationLogic<T, ArrayIteratorState<T>>
    {
        private readonly T[] _array;
        private readonly int _end;
        private int _index;

        /// <summary>
        /// Initializes the state with a target array and a specific range.
        /// Validates that the provided start and count are within the bounds of the array.
        /// </summary>
        /// <param name="array">The source array.</param>
        /// <param name="start">The starting index.</param>
        /// <param name="count">The number of elements to iterate.</param>
        /// <exception cref="ArgumentNullException">Thrown if the array is null.</exception>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when start or count are invalid.</exception>
        public ArrayIteratorState(T[] array, int start, int count)
        {
            if (array == null) throw new ArgumentNullException(nameof(array));

            if (start < 0 || (start > array.Length && array.Length > 0))
            {
                throw new ArgumentOutOfRangeException(nameof(start),
                    $"Start index {start} is out of range for array of length {array.Length}.");
            }

            if (count < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(count), "Iteration count cannot be negative.");
            }

            if (start + count > array.Length)
            {
                throw new ArgumentOutOfRangeException(nameof(count),
                    $"The requested range (start: {start}, count: {count}) exceeds the array length of {array.Length}.");
            }

            _array = array;
            _index = start - 1;
            _end = start + count;
        }

        /// <summary>
        /// Non-destructive check if more elements are available in the specified range.
        /// </summary>
        public bool HasNext(ref ArrayIteratorState<T> self)
        {
            return IsValid(ref self, self._index + 1);
        }

        /// <summary>
        /// Peeks at the next element (+1) without advancing the internal pointer.
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
        /// </summary>
        private static bool IsValid(ref ArrayIteratorState<T> self, int index)
        {
            return index >= 0 && index < self._end;
        }
    }
}