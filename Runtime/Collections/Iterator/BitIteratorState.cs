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
        /// Scans ahead and moves the internal index to the last invalid bit before the next hit.
        /// This optimizes MoveNext by skipping already evaluated 'false' bits.
        /// </summary>
        public bool HasNext(ref BitIteratorState self)
        {
            MoveBeforeNext(ref self);
            return self._currentIndex < self._endIndex - 1;
        }

        /// <summary>
        /// Returns the index of the next matching bit without advancing the iterator's consumer state.
        /// Useful for syncing bit-masks between different GPU update passes.
        /// </summary>
        public bool TryPeekNext(ref BitIteratorState self, out int result)
        {
            MoveBeforeNext(ref self);

            int nextIndex = self._currentIndex + 1;
            if (IsValid(ref self, nextIndex))
            {
                result = nextIndex;
                return true;
            }

            result = default;
            return false;
        }

        /// <summary>
        /// Finds the next bit that matches the target state within the configured range.
        /// </summary>
        /// <param name="self">Reference to the current iterator state.</param>
        /// <param name="result">The index of the found bit.</param>
        /// <returns>True if a matching bit was found; otherwise, false.</returns>
        public bool MoveNext(ref BitIteratorState self, out int result)
        {
            MoveBeforeNext(ref self);
            self._currentIndex++;

            if (IsValid(ref self, self._currentIndex))
            {
                result = self._currentIndex;
                return true;
            }

            result = default;
            return false;
        }

        /// <summary>
        /// Static helper for bounds and null checks to encourage inlining.
        /// </summary>
        private static bool IsValid(ref BitIteratorState self, int index)
        {
            return self._bits != null && index >= 0 && index < self._endIndex && index < self._bits.Length;
        }

        /// <summary>
        /// Core optimization: Fast-forwards the index to the position immediately preceding the next target bit.
        /// This ensures that the actual data is only scanned once, regardless of HasMore/MoveNext order.
        /// </summary>
        private static void MoveBeforeNext(ref BitIteratorState self)
        {
            if (self._bits == null) return;

            int search = self._currentIndex + 1;

            while (search < self._endIndex && search < self._bits.Length)
            {
                if (self._bits.Get(search) == self._targetState)
                {
                    self._currentIndex = search - 1;
                    return;
                }
                search++;
            }

            self._currentIndex = self._endIndex;
        }
    }
}