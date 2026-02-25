using Rayforge.Core.Collections.Abstractions;
using System.Collections;

namespace Rayforge.Core.Collections.Iterator
{
    /// <summary>
    /// A struct-based state for traversing bits in a BitArray within a specific range.
    /// Optimized for batch processing and can search for both 'set' (true) and 'unset' (false) states.
    /// </summary>
    public struct BitIteratorState : IIterationLogic<int, BitIteratorState>
    {
        private readonly BitArray _bits;
        private readonly int _endIndex;
        private readonly bool _targetState;
        private int _currentIndex;

        /// <summary>
        /// Initializes the traversal state for a specific sub-range of the BitArray.
        /// </summary>
        /// <param name="bits">The source BitArray to scan.</param>
        /// <param name="startIndex">The starting bit index (inclusive).</param>
        /// <param name="count">Number of bits to evaluate from the start index.</param>
        /// <param name="targetState">The bit value to search for (true = set, false = unset).</param>
        public BitIteratorState(BitArray bits, int startIndex, int count, bool targetState = true)
        {
            _bits = bits;
            _targetState = targetState;
            _currentIndex = startIndex - 1;
            _endIndex = startIndex + count;
        }

        /// <summary>
        /// Finds the next bit that matches the target state within the configured range.
        /// </summary>
        /// <param name="self">Reference to the current iterator state.</param>
        /// <param name="result">The index of the found bit.</param>
        /// <returns>True if a matching bit was found; otherwise, false.</returns>
        public bool MoveNext(ref BitIteratorState self, out int result)
        {
            self._currentIndex++;

            while (self._currentIndex < self._endIndex)
            {
                if (self._bits.Get(self._currentIndex) == self._targetState)
                {
                    result = self._currentIndex;
                    return true;
                }
                self._currentIndex++;
            }

            result = default;
            return false;
        }
    }
}