using Rayforge.Core.Collections.Abstractions;
using Rayforge.Core.Collections.Helpers;
using Rayforge.Core.Collections.Iterator;
using System;
using System.Collections;

namespace Rayforge.Core.Collections.Buffered
{
    /// <summary>
    /// A reusable, allocation-free iterator logic that scans a BitArray 
    /// and groups contiguous 'true' bits into index-based ranges.
    /// </summary>
    public struct DirtySegmentIteratorState : IIterationLogic<BufferSegmentMeta, DirtySegmentIteratorState>
    {
        private readonly Array _sourceArray;
        private readonly int _batchSize;
        private readonly int _totalCapacity;
        private readonly bool _mergeContiguous;

        private BitIteratorState _bitScanner;

        private BufferSegmentMeta _cachedSegment;
        private bool _hasCachedSegment;

        /// <summary>
        /// The number of elements per dirty bit. 
        /// </summary>
        public int BatchSize => _batchSize;

        /// <summary>
        /// The total capacity of the underlying store.
        /// </summary>
        public int TotalCapacity => _totalCapacity;

        /// <summary>
        /// The raw source array being scanned.
        /// </summary>
        public Array SourceArray => _sourceArray;

        /// <summary>
        /// Indicates if this scanner is configured to merge contiguous dirty blocks.
        /// </summary>
        public bool IsMergingEnabled => _mergeContiguous;

        /// <summary>
        /// Initializes a new iterator for scanning dirty segments.
        /// </summary>
        /// <param name="source">The raw data array to be synced.</param>
        /// <param name="dirtyBits">The bitmask tracking modified segments.</param>
        /// <param name="batchSize">Number of elements represented by a single bit.</param>
        /// <param name="totalCapacity">The total number of elements in the source array.</param>
        /// <param name="merge">Determines whether contiguous batches will be combined.</param>
        public DirtySegmentIteratorState(Array source, BitArray dirtyBits, int batchSize, int totalCapacity, bool merge)
        {
            _sourceArray = source;
            _batchSize = batchSize;
            _totalCapacity = totalCapacity;
            _mergeContiguous = merge;

            int totalBatches = BufferMath.GetTotalBatches(totalCapacity, batchSize);
            _bitScanner = new BitIteratorState(dirtyBits, 0, totalBatches, targetState: true);

            _cachedSegment = default;
            _hasCachedSegment = false;
        }

        /// <summary>
        /// Checks if another dirty segment exists by pre-calculating the next range.
        /// </summary>
        public bool HasNext(ref DirtySegmentIteratorState self)
        {
            MoveBeforeNext(ref self);
            return self._hasCachedSegment;
        }

        /// <summary>
        /// Returns the next segment without consuming it. 
        /// Directly exposes the pre-calculated segment from the cache.
        /// </summary>
        public bool TryPeekNext(ref DirtySegmentIteratorState self, out BufferSegmentMeta result)
        {
            MoveBeforeNext(ref self);
            result = self._cachedSegment;
            return self._hasCachedSegment;
        }

        /// <summary>
        /// Returns the pre-calculated dirty segment meta-data.
        /// </summary>
        public bool MoveNext(ref DirtySegmentIteratorState self, out BufferSegmentMeta result)
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

        /// <summary>
        /// Core optimization: Scans the BitArray for the next dirty bit and expands the range if merging is enabled.
        /// This aligns the state by pre-calculating the full BufferSegmentMeta before the MoveNext call.
        /// </summary>
        private static void MoveBeforeNext(ref DirtySegmentIteratorState self)
        {
            if (self._hasCachedSegment) return;

            if (!self._bitScanner.MoveNext(ref self._bitScanner, out int startBatch))
            {
                return;
            }

            int endBatch = startBatch;

            if (self._mergeContiguous)
            {
                while (self._bitScanner.TryPeekNext(ref self._bitScanner, out int nextBatch) && nextBatch == endBatch + 1)
                {
                    self._bitScanner.MoveNext(ref self._bitScanner, out endBatch);
                }
            }

            int startElement = startBatch * self._batchSize;
            int endElement = Math.Min((endBatch + 1) * self._batchSize, self._totalCapacity);

            self._cachedSegment = new BufferSegmentMeta
            {
                Source = self._sourceArray,
                Start = startElement,
                Count = endElement - startElement
            };
            self._hasCachedSegment = true;
        }
    }
}