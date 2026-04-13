using Rayforge.Core.Collections.Abstractions;
using Rayforge.Core.Collections.Helpers;
using System;
using System.Runtime.CompilerServices;

namespace Rayforge.Core.Collections.Buffered
{
    /// <summary>
    /// A high-performance, linear iterator state that treats the entire buffer as a single 
    /// continuous segment or a sequence of fixed batches.
    /// This implementation eliminates caching overhead and branch complexity for maximum hardware throughput.
    /// </summary>
    public struct SegmentIteratorState : IIterationLogic<BufferSegmentMeta, SegmentIteratorState>
    {
        private readonly Array _sourceArray;
        private readonly int _batchSize;
        private readonly int _totalCapacity;

        private int _currentElement;

        /// <summary>
        /// Gets the number of elements processed per iteration step.
        /// </summary>
        public int BatchSize => _batchSize;

        /// <summary>
        /// Gets the total number of elements in the underlying source array.
        /// </summary>
        public int TotalCapacity => _totalCapacity;

        /// <summary>
        /// Gets the reference to the raw source array.
        /// </summary>
        public Array SourceArray => _sourceArray;

        /// <summary>
        /// Initializes a new linear iterator state.
        /// </summary>
        /// <param name="source">The source array to iterate over.</param>
        /// <param name="batchSize">The maximum size of each segment. Set to 0 or totalCapacity for single-segment iteration.</param>
        /// <param name="totalCapacity">The relevant capacity within the source array.</param>
        public SegmentIteratorState(Array source, int batchSize, int totalCapacity)
        {
            _sourceArray = source;
            _batchSize = batchSize;
            _totalCapacity = totalCapacity;
            _currentElement = 0;
        }

        /// <summary>
        /// Performs a direct bounds check to determine if more segments are available.
        /// </summary>
        /// <param name="self">The current iterator state reference.</param>
        /// <returns>True if the current index is within the total capacity; otherwise, false.</returns>
        public bool HasNext(ref SegmentIteratorState self)
            => self._currentElement < self._totalCapacity;

        /// <summary>
        /// Advances the iterator to the next segment and calculates its metadata on the fly.
        /// </summary>
        /// <param name="self">The current iterator state reference to be mutated.</param>
        /// <param name="result">When this method returns, contains the metadata for the next segment.</param>
        /// <returns>True if a segment was successfully retrieved; false if the end of the buffer was reached.</returns>
        public bool MoveNext(ref SegmentIteratorState self, out BufferSegmentMeta result)
        {
            if (self._currentElement >= self._totalCapacity)
            {
                result = default;
                return false;
            }

            int count = CalculateNextBatchSize(self._currentElement, self._totalCapacity, self._batchSize);

            result = new BufferSegmentMeta
            {
                Source = self._sourceArray,
                Start = self._currentElement,
                Count = count
            };

            self._currentElement += count;
            return true;
        }

        /// <summary>
        /// Returns the metadata for the next segment without advancing the internal cursor.
        /// </summary>
        /// <param name="self">The current iterator state reference.</param>
        /// <param name="result">When this method returns, contains the metadata for the upcoming segment.</param>
        /// <returns>True if a segment is available to peek; otherwise, false.</returns>
        public bool TryPeekNext(ref SegmentIteratorState self, out BufferSegmentMeta result)
        {
            if (self._currentElement >= self._totalCapacity)
            {
                result = default;
                return false;
            }

            result = new BufferSegmentMeta
            {
                Source = self._sourceArray,
                Start = self._currentElement,
                Count = CalculateNextBatchSize(self._currentElement, self._totalCapacity, self._batchSize)
            };
            return true;
        }

        /// <summary>
        /// Calculates the size of the next segment based on the remaining capacity and the configured batch size.
        /// </summary>
        /// <param name="currentElement">The current index within the buffer.</param>
        /// <param name="totalCapacity">The total number of elements to be processed.</param>
        /// <param name="batchSize">The configured maximum size for segments.</param>
        /// <returns>The number of elements for the next segment.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static int CalculateNextBatchSize(int currentElement, int totalCapacity, int batchSize)
        {
            int remaining = totalCapacity - currentElement;

            // Use the remaining elements if no batch size is set or if the remainder is smaller than a full batch.
            return (batchSize <= 0 || batchSize > remaining)
                    ? remaining
                    : batchSize;
        }
    }
}