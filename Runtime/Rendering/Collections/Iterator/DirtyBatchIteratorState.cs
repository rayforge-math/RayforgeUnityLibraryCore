using Rayforge.Core.Collections.Abstractions;
using Rayforge.Core.Common.Rendering.Helpers;
using System;
using System.Collections;

namespace Rayforge.Core.Rendering.Collections.Iterator
{
    /// <summary>
    /// A reusable, allocation-free iterator logic that scans a BitArray 
    /// and groups contiguous 'true' bits into index-based ranges.
    /// </summary>
    public struct DirtyBatchIteratorState : IIterationLogic<BufferSegmentMeta, DirtyBatchIteratorState>
    {
        private readonly Array _sourceArray;
        private readonly BitArray _dirtyBits;
        private readonly int _batchSize;
        private readonly int _totalCapacity;
        private readonly int _totalBatches;
        private int _currentBatch;

        /// <summary>
        /// Initializes a new iterator for scanning dirty segments.
        /// </summary>
        /// <param name="source">The raw data array to be synced.</param>
        /// <param name="dirtyBits">The bitmask tracking modified segments.</param>
        /// <param name="batchSize">Number of elements represented by a single bit.</param>
        /// <param name="totalCapacity">The total number of elements in the source array.</param>
        public DirtyBatchIteratorState(Array source, BitArray dirtyBits, int batchSize, int totalCapacity)
        {
            _sourceArray = source;
            _dirtyBits = dirtyBits;
            _batchSize = batchSize;
            _totalCapacity = totalCapacity;
            _totalBatches = BufferMath.GetTotalBatches(totalCapacity, batchSize);
            _currentBatch = 0;
        }

        /// <summary>
        /// Scans for the next contiguous block of dirty bits and calculates the element range.
        /// </summary>
        public bool MoveNext(ref DirtyBatchIteratorState self, out BufferSegmentMeta result)
        {
            result = default;

            while (self._currentBatch < self._totalBatches)
            {
                if (!self._dirtyBits.Get(self._currentBatch))
                {
                    self._currentBatch++;
                    continue;
                }

                int startBatch = self._currentBatch;

                while (self._currentBatch < self._totalBatches && self._dirtyBits.Get(self._currentBatch))
                {
                    self._currentBatch++;
                }
                int endBatch = self._currentBatch - 1;

                int start = startBatch * self._batchSize;
                int end = Math.Min((endBatch + 1) * self._batchSize, self._totalCapacity);
                int count = end - start;

                result = new BufferSegmentMeta
                {
                    Source = self._sourceArray,
                    Start = start,
                    Count = count
                };
                return true;
            }

            return false;
        }
    }
}