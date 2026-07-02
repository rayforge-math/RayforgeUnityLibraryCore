using Rayforge.Core.Collections.Abstractions;
using System;
using System.Runtime.CompilerServices;

namespace Rayforge.Core.Collections.Buffering
{
    /// <summary>
    /// A high-performance, linear iterator state that treats a section of a buffer 
    /// (defined by an offset and size) as a sequence of fixed batches.
    /// This implementation eliminates caching overhead and branch complexity for maximum hardware throughput.
    /// </summary>
    /// <typeparam name="T">The unmanaged type of the elements in the buffer.</typeparam>
    public struct BufferSegmentState<T> : IIterationLogic<BufferSegmentMeta<T>, BufferSegmentState<T>>
    {
        #region Properties

        /// <summary> The reference to the data source being scanned. </summary>
        private readonly T[] _sourceArray;

        /// <summary> The size of each segment batch. </summary>
        private readonly int _batchSize;

        /// <summary> The starting offset within the buffer for this scanner. </summary>
        private readonly int _offset;

        /// <summary> The total number of elements to process. </summary>
        private readonly int _size;

        /// <summary> The current iteration index relative to the start of the buffer. </summary>
        private int _currentRelativeIndex;

        /// <summary>
        /// Gets the number of elements processed per iteration step.
        /// </summary>
        public int BatchSize => _batchSize;

        /// <summary>
        /// Gets the total number of elements to be processed within the segment.
        /// </summary>
        public int Size => _size;

        /// <summary>
        /// Gets the starting offset within the source array.
        /// </summary>
        public int Offset => _offset;

        /// <summary>
        /// Gets the reference to the raw source array.
        /// </summary>
        public T[] SourceArray => _sourceArray;

        #endregion

        #region Constructor

        /// <summary>
        /// Initializes a new linear iterator state with validation checks.
        /// </summary>
        /// <param name="source">The source array to iterate over.</param>
        /// <param name="offset">The starting index within the source array.</param>
        /// <param name="size">The number of elements to iterate, starting from the offset.</param>
        /// <param name="batchSize">The maximum size of each segment. Set to 0 or size for single-segment iteration.</param>
        /// <exception cref="ArgumentNullException">Thrown when source is null.</exception>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when offset or size are invalid relative to the array length.</exception>
        public BufferSegmentState(T[] source, int offset, int size, int batchSize)
        {
            _sourceArray = source ?? throw new ArgumentNullException(nameof(source), "Source array cannot be null.");

            if (offset < 0 || offset > source.Length)
            {
                throw new ArgumentOutOfRangeException(nameof(offset), "Offset must be non-negative and within array bounds.");
            }

            if (size < 0 || (offset + size) > source.Length)
            {
                throw new ArgumentOutOfRangeException(nameof(size), "Size must be non-negative and the range must not exceed the array length.");
            }

            if (batchSize < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(batchSize), "Batch size cannot be negative.");
            }

            _offset = offset;
            _size = size;
            _batchSize = batchSize;
            _currentRelativeIndex = 0;
        }

        #endregion

        #region IIterationLogic Impl

        /// <summary>
        /// Performs a direct bounds check to determine if more segments are available.
        /// </summary>
        /// <param name="self">The current iterator state reference.</param>
        /// <returns>True if the current relative index is within the defined size; otherwise, false.</returns>
        public bool HasNext(ref BufferSegmentState<T> self)
            => self._currentRelativeIndex < self._size;

        /// <summary>
        /// Advances the iterator to the next segment and calculates its metadata on the fly.
        /// </summary>
        /// <param name="self">The current iterator state reference to be mutated.</param>
        /// <param name="result">When this method returns, contains the metadata for the next segment.</param>
        /// <returns>True if a segment was successfully retrieved; false if the end of the buffer was reached.</returns>
        public bool MoveNext(ref BufferSegmentState<T> self, out BufferSegmentMeta<T> result)
        {
            if (self._currentRelativeIndex >= self._size)
            {
                result = default;
                return false;
            }

            int count = CalculateNextBatchSize(self._currentRelativeIndex, self._size, self._batchSize);

            result = new BufferSegmentMeta<T>
            {
                Source = self._sourceArray,
                Start = self._offset + self._currentRelativeIndex,
                Count = count
            };

            // Advance the internal cursor by the size of the processed batch
            self._currentRelativeIndex += count;
            return true;
        }

        /// <summary>
        /// Returns the metadata for the next segment without advancing the internal cursor.
        /// </summary>
        /// <param name="self">The current iterator state reference.</param>
        /// <param name="result">When this method returns, contains the metadata for the upcoming segment.</param>
        /// <returns>True if a segment is available to peek; otherwise, false.</returns>
        public bool TryPeekNext(ref BufferSegmentState<T> self, out BufferSegmentMeta<T> result)
        {
            if (self._currentRelativeIndex >= self._size)
            {
                result = default;
                return false;
            }

            result = new BufferSegmentMeta<T>
            {
                Source = self._sourceArray,
                Start = self._offset + self._currentRelativeIndex,
                Count = CalculateNextBatchSize(self._currentRelativeIndex, self._size, self._batchSize)
            };
            return true;
        }

        #endregion

        #region Private Helpers

        /// <summary>
        /// Calculates the size of the next segment based on the remaining capacity and the configured batch size.
        /// </summary>
        /// <param name="currentRelativeIndex">The current index relative to the start offset.</param>
        /// <param name="totalSize">The total number of elements to be processed.</param>
        /// <param name="batchSize">The configured maximum size for segments.</param>
        /// <returns>The number of elements for the next segment.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static int CalculateNextBatchSize(int currentRelativeIndex, int totalSize, int batchSize)
        {
            int remaining = totalSize - currentRelativeIndex;

            // Use the remaining elements if no batch size is set or if the remainder is smaller than a full batch.
            return (batchSize <= 0 || batchSize > remaining)
                    ? remaining
                    : batchSize;
        }

        #endregion
    }
}