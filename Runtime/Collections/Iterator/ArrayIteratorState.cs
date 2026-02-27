using Rayforge.Core.Collections.Abstractions;

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

        public ArrayIteratorState(T[] array, int start, int count)
        {
            _array = array;
            _index = start - 1;
            _end = start + count;
        }

        public bool MoveNext(ref ArrayIteratorState<T> self, out T result)
        {
            self._index++;

            if (self._array != null && self._index < self._end && self._index < self._array.Length)
            {
                result = self._array[self._index];
                return true;
            }

            result = default;
            return false;
        }
    }
}