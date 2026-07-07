using Rayforge.Core.Collections.Abstractions;
using Rayforge.Core.Collections.Helpers;
using System;
using System.Collections;

namespace Rayforge.Core.Collections.Buffering
{
    /// <summary>
    /// A synchronized iterator state that merges two independent dirty-segment scanners into fixed-size windows.
    /// Aligns buffer segments. Enforces compatibility checks and grid-based slicing.
    /// </summary>
    public struct SyncedDirtySegmentState<TValueA, TValueB>
        : IIterationLogic<SyncedSegmentMeta<TValueA, TValueB>, SyncedDirtySegmentState<TValueA, TValueB>>
    {
        #region Properties

        private DirtySegmentState<TValueA> _scannerA;
        private DirtySegmentState<TValueB> _scannerB;

        private int _resumeA;
        private int _resumeB;

        private int _currentWindowStart;

        private SyncedSegmentMeta<TValueA, TValueB> _peekCache;
        private bool _hasPeeked;

        private readonly int _syncWindow;
        private readonly int _totalCapacity;

        #endregion

        #region Constructor

        /// <summary>
        /// Initializes a new instance with validation checks to ensure consistency across streams.
        /// </summary>
        /// <param name="sourceA">Raw data array for stream A.</param>
        /// <param name="sourceB">Raw data array for stream B.</param>
        /// <param name="bitsA">Dirty-bit mask for stream A.</param>
        /// <param name="bitsB">Dirty-bit mask for stream B.</param>
        /// <param name="offset">Shared starting index.</param>
        /// <param name="size">Shared total size.</param>
        /// <param name="batchSize">Shared batch size (elements per bit).</param>
        /// <param name="batchesPerWindow">Target number of batches per synchronization window.</param>
        /// <param name="merge">Merge adjacent segments.</param>
        /// <exception cref="ArgumentOutOfRangeException">Thrown if batchesPerWindow is less than 1.</exception>
        public SyncedDirtySegmentState(
            TValueA[] sourceA, TValueB[] sourceB, BitArray bitsA, BitArray bitsB,
            int offset, int size, int batchSize = 1, int batchesPerWindow = 1, bool merge = false)
        {
            // 1. Null-Checks
            if (sourceA == null || sourceB == null) throw new ArgumentNullException("Source arrays cannot be null.");
            if (bitsA == null || bitsB == null) throw new ArgumentNullException("BitArrays cannot be null.");

            // 2. Range-Checks
            if (offset != 0 && (offset < 0 || offset >= sourceA.Length || offset >= sourceB.Length))
                throw new ArgumentOutOfRangeException(nameof(offset), "Offset is out of bounds.");

            if (size < 0 || (offset + size) > sourceA.Length || (offset + size) > sourceB.Length)
                throw new ArgumentOutOfRangeException(nameof(size), "Size exceeds buffer boundaries.");

            if (batchSize <= 0)
                throw new ArgumentOutOfRangeException(nameof(batchSize), "Batch size must be positive.");

            if (batchesPerWindow < 1)
                throw new ArgumentOutOfRangeException(nameof(batchesPerWindow), "Window must contain at least 1 batch.");

            // 3. Logic initialization
            _scannerA = new DirtySegmentState<TValueA>(sourceA, bitsA, offset, size, batchSize, merge);
            _scannerB = new DirtySegmentState<TValueB>(sourceB, bitsB, offset, size, batchSize, merge);

            _totalCapacity = size;

            _syncWindow = batchesPerWindow * batchSize;

            _resumeA = 0;
            _resumeB = 0;
            _currentWindowStart = 0;
            _peekCache = default;
            _hasPeeked = false;
        }

        #endregion

        #region IIterationLogic Implementation

        /// <summary>
        /// Checks if there are more dirty segments to process or if a peeked result is pending.
        /// </summary>
        /// <param name="self">Reference to the current iterator state.</param>
        /// <returns>True if more data is available; otherwise, false.</returns>
        public bool HasNext(ref SyncedDirtySegmentState<TValueA, TValueB> self)
        {
            return self._hasPeeked || self._currentWindowStart < self._totalCapacity;
        }

        /// <summary>
        /// Returns the next synchronized window without advancing the iterator's main state.
        /// Populates the lookahead cache if empty.
        /// </summary>
        /// <param name="self">Reference to the current iterator state.</param>
        /// <param name="result">The peeked synchronization metadata.</param>
        /// <returns>True if data was available to peek; otherwise, false.</returns>
        public bool TryPeekNext(ref SyncedDirtySegmentState<TValueA, TValueB> self, out SyncedSegmentMeta<TValueA, TValueB> result)
        {
            if (!self._hasPeeked)
            {
                if (!ComputeNextWindow(ref self, out self._peekCache))
                {
                    result = self._peekCache;
                    return false;
                }
                self._hasPeeked = true;
            }
            result = self._peekCache;
            return true;
        }

        /// <summary>
        /// Advances the iterator to the next synchronized window containing dirty data.
        /// Consumes the peek cache if available, otherwise computes the next window.
        /// </summary>
        /// <param name="self">Reference to the current iterator state.</param>
        /// <param name="result">The synchronized metadata for the window being entered.</param>
        /// <returns>True if a window with dirty data was found; otherwise, false.</returns>
        public bool MoveNext(ref SyncedDirtySegmentState<TValueA, TValueB> self, out SyncedSegmentMeta<TValueA, TValueB> result)
        {
            if (TryPeekNext(ref self, out result))
            {
                self._hasPeeked = false;
                return true;
            }
            return false;
        }

        #endregion

        #region Private Static Helpers

        /// <summary>
        /// Internal logic to slide the fixed window grid until dirty data is found or capacity is reached.
        /// </summary>
        /// <param name="self">Reference to the iterator state to advance.</param>
        /// <param name="result">The metadata container to fill.</param>
        /// <returns>True if data was found within a window; false if the end was reached.</returns>
        private static bool ComputeNextWindow(ref SyncedDirtySegmentState<TValueA, TValueB> self, out SyncedSegmentMeta<TValueA, TValueB> result)
        {
            result = default;

            while (self._currentWindowStart < self._totalCapacity)
            {
                int windowEnd = self._currentWindowStart + self._syncWindow;

                SpanWindowForScanner(ref self._scannerA, ref self._resumeA, windowEnd, ref result.SegmentA);
                SpanWindowForScanner(ref self._scannerB, ref self._resumeB, windowEnd, ref result.SegmentB);

                self._currentWindowStart = windowEnd;

                if (result.SegmentA.Count > 0 || result.SegmentB.Count > 0)
                {
                    return true;
                }

                if (!self._scannerA.HasNext(ref self._scannerA) && self._resumeA <= 0 &&
                    !self._scannerB.HasNext(ref self._scannerB) && self._resumeB <= 0)
                {
                    break;
                }
            }

            return false;
        }

        /// <summary>
        /// Aggregates all dirty segments of a single scanner that fall within the specified window boundary.
        /// Handles partial segments (resumption) and advances the underlying scanner.
        /// </summary>
        /// <param name="scanner">The scanner state to advance.</param>
        /// <param name="resumeIndex">The index to resume from if a segment was previously clipped.</param>
        /// <param name="windowEnd">The exclusive upper boundary of the current grid window.</param>
        /// <param name="result">The segment metadata to populate.</param>
        private static void SpanWindowForScanner<TValue>(
            ref DirtySegmentState<TValue> scanner,
            ref int resumeIndex,
            int windowEnd,
            ref BufferSegmentMeta<TValue> result)
        {
            result.Source = null;
            result.Start = 0;
            result.Count = 0;

            int effectiveStart;
            if (resumeIndex > 0)
            {
                effectiveStart = resumeIndex;
            }
            else if (scanner.TryPeekNext(ref scanner, out var firstPeek) && firstPeek.Start < windowEnd)
            {
                effectiveStart = firstPeek.Start;
            }
            else
            {
                resumeIndex = 0;
                return;
            }

            TValue[] buffer = null;
            int effectiveEnd = effectiveStart;
            while (scanner.TryPeekNext(ref scanner, out var segment))
            {
                buffer = segment.Source;

                if (segment.Start >= windowEnd)
                {
                    resumeIndex = 0;
                    break;
                }

                if (segment.End <= windowEnd)
                {
                    effectiveEnd = segment.End;
                    scanner.MoveNext(ref scanner, out _);
                    resumeIndex = 0;
                }
                else
                {
                    effectiveEnd = windowEnd;
                    resumeIndex = windowEnd;
                    break;
                }
            }

            if (effectiveEnd > effectiveStart)
            {
                result.Source = buffer;
                result.Start = effectiveStart;
                result.Count = effectiveEnd - effectiveStart;
            }
        }

        #endregion
    }
}