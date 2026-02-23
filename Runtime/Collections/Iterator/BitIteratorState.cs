using Rayforge.Core.Collections.Abstractions;
using System.Collections;

namespace Rayforge.Core.Collections.Iterator
{
    /// <summary>
    /// A struct-based state for traversing set bits in a BitArray.
    /// Implements IIterationLogic to allow high-performance, zero-allocation iteration via 'ref self'.
    /// </summary>
    public struct BitIteratorState : IIterationLogic<int, BitIteratorState>
    {
        private readonly BitArray _bits;
        private readonly int _totalCount;
        private int _currentIndex;

        /// <summary>
        /// Initializes the traversal state with a target BitArray and its bounds.
        /// </summary>
        /// <param name="bits">The source BitArray to traverse.</param>
        /// <param name="totalCount">The number of bits (total batches) to check.</param>
        public BitIteratorState(BitArray bits, int totalCount)
        {
            _bits = bits;
            _totalCount = totalCount;
            _currentIndex = -1;
        }

        /// <summary>
        /// Finds the next true bit in the array.
        /// </summary>
        public bool MoveNext(ref BitIteratorState self, out int result)
        {
            self._currentIndex++;

            while (self._currentIndex < self._totalCount)
            {
                if (self._bits.Get(self._currentIndex))
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