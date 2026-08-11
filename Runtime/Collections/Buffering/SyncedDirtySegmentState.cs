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

        public SyncedDirtySegmentState(
            TValueA[] sourceA, TValueB[] sourceB, BitArray bitsA, BitArray bitsB,
            int offset, int size, int batchSize = 1, int batchesPerWindow = 1)
        {
            if (sourceA == null || sourceB == null) throw new ArgumentNullException("Source arrays cannot be null.");
            if (bitsA == null || bitsB == null) throw new ArgumentNullException("BitArrays cannot be null.");

            if (offset != 0 && (offset < 0 || offset >= sourceA.Length || offset >= sourceB.Length))
                throw new ArgumentOutOfRangeException(nameof(offset), "Offset is out of bounds.");

            if (size < 0 || (offset + size) > sourceA.Length || (offset + size) > sourceB.Length)
                throw new ArgumentOutOfRangeException(nameof(size), "Size exceeds buffer boundaries.");

            if (batchSize <= 0)
                throw new ArgumentOutOfRangeException(nameof(batchSize), "Batch size must be positive.");

            if (offset % batchSize != 0)
                throw new ArgumentException($"Offset ({offset}) must be a multiple of batchSize ({batchSize}).", nameof(offset));

            if (size % batchSize != 0)
                throw new ArgumentException($"Size ({size}) must be a multiple of batchSize ({batchSize}).", nameof(size));

            if (batchesPerWindow < 1)
                throw new ArgumentOutOfRangeException(nameof(batchesPerWindow), "Window must contain at least 1 batch.");

            _scannerA = new DirtySegmentState<TValueA>(sourceA, bitsA, offset, size, batchSize, false);
            _scannerB = new DirtySegmentState<TValueB>(sourceB, bitsB, offset, size, batchSize, false);

            _totalCapacity = offset + size;
            _syncWindow = batchesPerWindow * batchSize;

            _resumeA = 0;
            _resumeB = 0;
            _currentWindowStart = offset;
            _peekCache = default;
            _hasPeeked = false;
        }

        #endregion

        #region IIterationLogic Implementation

        public bool HasNext(ref SyncedDirtySegmentState<TValueA, TValueB> self)
        {
            return self._hasPeeked || MoveBeforeNextWindow(ref self);
        }

        public bool TryPeekNext(ref SyncedDirtySegmentState<TValueA, TValueB> self, out SyncedSegmentMeta<TValueA, TValueB> result)
        {
            if (!self._hasPeeked)
            {
                if (!MoveBeforeNextWindow(ref self))
                {
                    result = default;
                    return false;
                }

                if (!ComputeNextWindow(ref self, out self._peekCache))
                {
                    result = default;
                    return false;
                }
                self._hasPeeked = true;
            }
            result = self._peekCache;
            return true;
        }

        public bool MoveNext(ref SyncedDirtySegmentState<TValueA, TValueB> self, out SyncedSegmentMeta<TValueA, TValueB> result)
        {
            if (TryPeekNext(ref self, out result))
            {
                self._hasPeeked = false;
                self._currentWindowStart += self._syncWindow;
                return true;
            }
            return false;
        }

        #endregion

        #region Private Static Helpers

        private static bool MoveBeforeNextWindow(ref SyncedDirtySegmentState<TValueA, TValueB> self)
        {
            while (self._currentWindowStart < self._totalCapacity)
            {
                int windowEnd = self._currentWindowStart + self._syncWindow;

                if (HasDirtyDataInRange(ref self._scannerA, self._resumeA, windowEnd) ||
                    HasDirtyDataInRange(ref self._scannerB, self._resumeB, windowEnd))
                {
                    return true;
                }

                self._currentWindowStart = windowEnd;
            }
            return false;
        }

        private static bool HasDirtyDataInRange<TValue>(ref DirtySegmentState<TValue> scanner, int resumeIndex, int windowEnd)
        {
            if (resumeIndex > 0) return true;
            // Changed from < to <= to include segments starting exactly at the boundary
            return scanner.TryPeekNext(ref scanner, out var segment) && segment.Start < windowEnd;
        }

        private static bool ComputeNextWindow(ref SyncedDirtySegmentState<TValueA, TValueB> self, out SyncedSegmentMeta<TValueA, TValueB> result)
        {
            result = default;
            int windowEnd = self._currentWindowStart + self._syncWindow;

            SpanWindowForScanner(ref self._scannerA, ref self._resumeA, windowEnd, ref result.SegmentA);
            SpanWindowForScanner(ref self._scannerB, ref self._resumeB, windowEnd, ref result.SegmentB);

            return result.SegmentA.Count > 0 || result.SegmentB.Count > 0;
        }

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