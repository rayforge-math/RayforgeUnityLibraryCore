using Rayforge.Core.Collections.Abstractions;
using Rayforge.Core.Collections.Buffered;
using Rayforge.Core.Collections.Helpers;
using System;

namespace Rayforge.Core.Environment.Spatial
{
    /// <summary>
    /// A synchronized iterator state that merges two independent dirty-segment scanners into fixed-size windows.
    /// Aligns buffer segments. Enforces compatibility checks and grid-based slicing.
    /// </summary>
    public struct SyncedBufferIteratorState : IIterationLogic<SyncedBufferSegmentMeta, SyncedBufferIteratorState>
    {
        private DirtySegmentIteratorState _scannerA;
        private DirtySegmentIteratorState _scannerB;

        private int _resumeA;
        private int _resumeB;

        private int _currentWindowStart;

        private SyncedBufferSegmentMeta _peekCache;
        private bool _hasPeeked;

        private readonly int _maxSyncWindow;
        private readonly int _totalCapacity;

        /// <summary>
        /// Initializes a new sync state. Validates that both scanners share the same capacity and aligned batch sizes.
        /// </summary>
        /// <param name="a">The first dirty segment scanner (e.g., Spatial).</param>
        /// <param name="b">The second dirty segment scanner (e.g., Visual).</param>
        /// <param name="requestedWindowSize">The target size for the synchronization grid slots.</param>
        /// <exception cref="ArgumentException">Thrown if capacities or batch sizes are incompatible.</exception>
        public SyncedBufferIteratorState(DirtySegmentIteratorState a, DirtySegmentIteratorState b, int requestedWindowSize)
        {
            if (a.TotalCapacity != b.TotalCapacity)
            {
                throw new ArgumentException(
                    $"Scanner capacity mismatch: {a.TotalCapacity} vs {b.TotalCapacity}");
            }

            if (!BufferMath.IsPowerOfAligned(a.BatchSize, b.BatchSize))
            {
                throw new ArgumentException(
                    $"BatchSizes must be multiples of each other. Found {a.BatchSize} and {b.BatchSize}.");
            }

            int effectiveRequest = Math.Max(1, requestedWindowSize);
            _maxSyncWindow = BufferMath.GetAlignedBatchSize(effectiveRequest, a.BatchSize, b.BatchSize);
            _totalCapacity = Math.Max(a.TotalCapacity, b.TotalCapacity);

            _scannerA = a;
            _scannerB = b;

            _resumeA = 0;
            _resumeB = 0;

            _currentWindowStart = 0;

            _peekCache = default;
            _hasPeeked = false;
        }

        #region IIterationLogic Implementation

        /// <summary>
        /// Checks if there are more dirty segments to process or if a peeked result is pending.
        /// </summary>
        /// <param name="self">Reference to the current iterator state.</param>
        /// <returns>True if more data is available; otherwise, false.</returns>
        public bool HasNext(ref SyncedBufferIteratorState self)
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
        public bool TryPeekNext(ref SyncedBufferIteratorState self, out SyncedBufferSegmentMeta result)
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
        public bool MoveNext(ref SyncedBufferIteratorState self, out SyncedBufferSegmentMeta result)
        {
            if (TryPeekNext(ref self, out result))
            {
                self._hasPeeked = false;
                return true;
            }
            return false;
        }

        #endregion

        #region Static Helpers

        /// <summary>
        /// Internal logic to slide the fixed window grid until dirty data is found or capacity is reached.
        /// </summary>
        /// <param name="self">Reference to the iterator state to advance.</param>
        /// <param name="result">The metadata container to fill.</param>
        /// <returns>True if data was found within a window; false if the end was reached.</returns>
        private static bool ComputeNextWindow(ref SyncedBufferIteratorState self, out SyncedBufferSegmentMeta result)
        {
            result = default;

            while (self._currentWindowStart < self._totalCapacity)
            {
                int windowEnd = self._currentWindowStart + self._maxSyncWindow;

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
        private static void SpanWindowForScanner(
            ref DirtySegmentIteratorState scanner,
            ref int resumeIndex,
            int windowEnd,
            ref BufferSegmentMeta result)
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

            Array buffer = null;
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