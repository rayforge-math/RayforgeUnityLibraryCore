using Rayforge.Core.Collections.Abstractions;
using Rayforge.Core.Collections.Helpers;
using Rayforge.Core.Collections.Iterator;
using System;
using System.Collections;

namespace Rayforge.Core.Collections.Buffering
{
    /// <summary>
    /// A reusable, allocation-free iterator logic that scans a BitArray 
    /// and groups contiguous 'true' bits into index-based ranges within a specific slice of a buffer.
    /// This structure acts as an orchestrator, delegating range calculations
    /// while managing the dirty-bit scanning lifecycle.
    /// </summary>
    /// <typeparam name="T">The unmanaged type of the elements in the buffer.</typeparam>
    public struct DirtySegmentState<T> : IIterationLogic<BufferSegmentMeta<T>, DirtySegmentState<T>>
    {
        #region Properties

        /// <summary> The reference to the raw data source for range validation. </summary>
        private readonly T[] _sourceArray;

        /// <summary> The number of elements covered by a single dirty bit. </summary>
        private readonly int _batchSize;

        /// <summary> The starting index of the scan slice within the buffer. </summary>
        private readonly int _offset;

        /// <summary> The total number of elements to process. </summary>
        private readonly int _size;

        /// <summary> Flag indicating if adjacent dirty bits should be merged into a single segment. </summary>
        private readonly bool _mergeContiguous;

        /// <summary> The underlying bit-scanner orchestrating the bit traversal. </summary>
        private BitIteratorState _bitScanner;

        /// <summary> Stores the currently computed segment result for the iterator. </summary>
        private BufferSegmentMeta<T> _cachedSegment;

        /// <summary> Tracks if a valid segment has been computed and is ready to be consumed. </summary>
        private bool _hasCachedSegment;

        /// <summary>
        /// Gets the number of elements represented by a single bit in the BitArray.
        /// </summary>
        public int BatchSize => _batchSize;

        /// <summary>
        /// Gets the starting offset within the source array.
        /// </summary>
        public int Offset => _offset;

        /// <summary>
        /// Gets the total number of elements to be processed within the slice.
        /// </summary>
        public int Size => _size;

        /// <summary>
        /// Gets the raw source array being scanned.
        /// </summary>
        public T[] SourceArray => _sourceArray;

        /// <summary>
        /// Indicates if this scanner is configured to merge contiguous dirty blocks into single segments.
        /// </summary>
        public bool IsMergingEnabled => _mergeContiguous;

        #endregion

        #region Constructor

        /// <summary>
        /// Initializes a new iterator for scanning dirty segments within a defined buffer slice with validation.
        /// </summary>
        /// <param name="source">The raw data array to be synced.</param>
        /// <param name="dirtyBits">The bitmask tracking modified segments.</param>
        /// <param name="offset">The starting index within the source array.</param>
        /// <param name="size">The number of elements to consider, starting from the offset.</param>
        /// <param name="batchSize">Number of elements represented by a single bit.</param>
        /// <param name="merge">Determines whether contiguous batches will be combined.</param>
        /// <exception cref="ArgumentNullException">Thrown when source or dirtyBits is null.</exception>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when inputs are invalid or inconsistent.</exception>
        public DirtySegmentState(T[] source, BitArray dirtyBits, int offset, int size, int batchSize, bool merge = false)
        {
            _sourceArray = source ?? throw new ArgumentNullException(nameof(source), "Source array cannot be null.");
            if (dirtyBits == null) throw new ArgumentNullException(nameof(dirtyBits), "BitArray cannot be null.");

            if (offset < 0 || offset > source.Length)
                throw new ArgumentOutOfRangeException(nameof(offset), "Offset must be within array bounds.");

            if (size < 0 || (offset + size) > source.Length)
                throw new ArgumentOutOfRangeException(nameof(size), "Size must be non-negative and within array bounds.");

            if (batchSize <= 0)
                throw new ArgumentOutOfRangeException(nameof(batchSize), "Batch size must be greater than zero.");

            if (offset % batchSize != 0)
                throw new ArgumentException($"Offset ({offset}) must be a multiple of batchSize ({batchSize}).", nameof(offset));

            if (size % batchSize != 0)
                throw new ArgumentException($"Size ({size}) must be a multiple of batchSize ({batchSize}).", nameof(size));

            // Calculate relevant batches for this slice
            int startBatch = offset / batchSize;
            int totalBatches = BufferMath.GetTotalBatches(size, batchSize);

            if (dirtyBits.Length - startBatch < totalBatches)
            {
                throw new ArgumentOutOfRangeException(nameof(dirtyBits),
                    $"BitArray length ({dirtyBits.Length}) is too small for the specified slice size and batch size.");
            }

            _bitScanner = new BitIteratorState(dirtyBits, startBatch, totalBatches, targetState: true);

            _batchSize = batchSize;
            _offset = offset;
            _size = size;
            _mergeContiguous = merge;
            _cachedSegment = default;
            _hasCachedSegment = false;
        }

        #endregion

        #region IIterationLogic Impl

        /// <summary>
        /// Checks if another dirty segment exists by pre-calculating the next range.
        /// </summary>
        /// <param name="self">The current iterator state reference.</param>
        /// <returns>True if a dirty segment is available; otherwise, false.</returns>
        public bool HasNext(ref DirtySegmentState<T> self)
        {
            MoveBeforeNext(ref self);
            return self._hasCachedSegment;
        }

        /// <summary>
        /// Returns the next segment without consuming it. 
        /// Directly exposes the pre-calculated segment from the internal cache.
        /// </summary>
        /// <param name="self">The current iterator state reference.</param>
        /// <param name="result">When this method returns, contains the metadata for the upcoming segment.</param>
        /// <returns>True if a segment is available to peek; otherwise, false.</returns>
        public bool TryPeekNext(ref DirtySegmentState<T> self, out BufferSegmentMeta<T> result)
        {
            MoveBeforeNext(ref self);
            result = self._cachedSegment;
            return self._hasCachedSegment;
        }

        /// <summary>
        /// Returns the pre-calculated dirty segment metadata and clears the cache.
        /// </summary>
        /// <param name="self">The current iterator state reference.</param>
        /// <param name="result">When this method returns, contains the metadata for the next segment.</param>
        /// <returns>True if a segment was successfully retrieved; false if no more segments exist.</returns>
        public bool MoveNext(ref DirtySegmentState<T> self, out BufferSegmentMeta<T> result)
        {
            MoveBeforeNext(ref self);

            if (self._hasCachedSegment)
            {
                result = self._cachedSegment;
                self._cachedSegment = default;
                self._hasCachedSegment = false;
                return true;
            }

            result = default;
            return false;
        }

        #endregion

        #region Private Helpers

        /// <summary>
        /// Core optimization: Scans the BitArray for the next dirty bit and expands the range if merging is enabled.
        /// This aligns the state by pre-calculating the full BufferSegmentMeta before the MoveNext call.
        /// </summary>
        /// <param name="self">The current iterator state reference.</param>
        private static void MoveBeforeNext(ref DirtySegmentState<T> self)
        {
            if (self._hasCachedSegment) return;

            if (!self._bitScanner.MoveNext(ref self._bitScanner, out int startBatchIndex))
            {
                return;
            }

            int endBatchIndex = startBatchIndex;

            if (self._mergeContiguous)
            {
                while (self._bitScanner.TryPeekNext(ref self._bitScanner, out int nextBatch) && nextBatch == endBatchIndex + 1)
                {
                    self._bitScanner.MoveNext(ref self._bitScanner, out endBatchIndex);
                }
            }

            int start = startBatchIndex * self._batchSize;
            int end = (endBatchIndex + 1) * self._batchSize;

            int sliceEnd = self._offset + self._size;
            int count = Math.Min(end, sliceEnd) - start;

            self._cachedSegment = new BufferSegmentMeta<T>
            {
                Source = self._sourceArray,
                Start = start,
                Count = count
            };
            self._hasCachedSegment = true;
        }

        #endregion
    }
}