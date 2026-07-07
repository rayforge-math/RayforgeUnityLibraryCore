using Rayforge.Core.Collections.Abstractions;
using System;

namespace Rayforge.Core.Collections.Buffering
{
    /// <summary>
    /// A synchronized iterator state that merges two independent linear buffer scanners into aligned windows.
    /// Provides absolute indexing for all generated segments.
    /// </summary>
    /// <typeparam name="TValueA">The unmanaged type of the first buffer.</typeparam>
    /// <typeparam name="TValueB">The unmanaged type of the second buffer.</typeparam>
    public struct SyncedSegmentState<TValueA, TValueB>
        : IIterationLogic<SyncedSegmentMeta<TValueA, TValueB>, SyncedSegmentState<TValueA, TValueB>>
    {
        private BufferSegmentState<TValueA> _scannerA;
        private BufferSegmentState<TValueB> _scannerB;

        private int _currentWindowStart;
        private readonly int _syncWindow;
        private readonly int _absoluteEnd;

        /// <summary>
        /// Initializes a new synchronization state by creating internal linear scanners with strict validation.
        /// </summary>
        /// <param name="sourceA">Raw data for stream A.</param>
        /// <param name="sourceB">Raw data for stream B.</param>
        /// <param name="offset">Shared starting index for both scanners.</param>
        /// <param name="size">Total elements to process.</param>
        /// <param name="batchSize">The grid/batch size for the linear scan.</param>
        /// <param name="windowSize">The size of the synchronization grid slots.</param>
        /// <exception cref="ArgumentNullException">Thrown when a source array is null.</exception>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when offset, size, or batch settings are invalid.</exception>
        public SyncedSegmentState(
            TValueA[] sourceA, TValueB[] sourceB,
            int offset, int size, int batchSize = 1, int windowSize = 1)
        {
            if (sourceA == null || sourceB == null)
                throw new ArgumentNullException("Source arrays cannot be null.");

            if (offset < 0 || (offset + size) > sourceA.Length || (offset + size) > sourceB.Length)
                throw new ArgumentOutOfRangeException(nameof(offset), "Offset or size exceeds buffer boundaries.");

            if (batchSize <= 0)
                throw new ArgumentOutOfRangeException(nameof(batchSize), "Batch size cannot be negative or zero.");

            if (windowSize <= 0)
                throw new ArgumentOutOfRangeException(nameof(windowSize), "Window size must be greater than zero.");

            _scannerA = new BufferSegmentState<TValueA>(sourceA, offset, size, batchSize);
            _scannerB = new BufferSegmentState<TValueB>(sourceB, offset, size, batchSize);

            _syncWindow = windowSize;
            _absoluteEnd = offset + size; // The absolute end point in the source arrays
            _currentWindowStart = offset; // Start point is always the absolute offset
        }

        /// <summary>
        /// Checks if there are more segments to process within the buffer capacity.
        /// </summary>
        /// <param name="self">Reference to the current iterator state.</param>
        /// <returns>True if the window start is within capacity; otherwise, false.</returns>
        public bool HasNext(ref SyncedSegmentState<TValueA, TValueB> self)
            => self._currentWindowStart < self._absoluteEnd;

        /// <summary>
        /// Advances the iterator to the next synchronized segment window.
        /// </summary>
        /// <param name="self">Reference to the current iterator state.</param>
        /// <param name="result">The synchronized segment metadata for the window.</param>
        /// <returns>True if a window was successfully created; false if end of capacity is reached.</returns>
        public bool MoveNext(ref SyncedSegmentState<TValueA, TValueB> self, out SyncedSegmentMeta<TValueA, TValueB> result)
        {
            if (!TryPeekNext(ref self, out result))
                return false;

            // Advance to the next absolute start point based on the processed count
            self._currentWindowStart += result.SegmentA.Count;
            return true;
        }

        /// <summary>
        /// Calculates the metadata for the next segment window without advancing the iterator state.
        /// </summary>
        /// <param name="self">Reference to the current iterator state.</param>
        /// <param name="result">The synchronized segment metadata.</param>
        /// <returns>True if a window is available; otherwise, false.</returns>
        public bool TryPeekNext(ref SyncedSegmentState<TValueA, TValueB> self, out SyncedSegmentMeta<TValueA, TValueB> result)
        {
            if (self._currentWindowStart >= self._absoluteEnd)
            {
                result = default;
                return false;
            }

            int windowEnd = Math.Min(self._currentWindowStart + self._syncWindow, self._absoluteEnd);

            // Metadata contains the absolute start point and calculated count for the current window
            result = new SyncedSegmentMeta<TValueA, TValueB>
            {
                SegmentA = new BufferSegmentMeta<TValueA>
                {
                    Source = self._scannerA.SourceArray,
                    Start = self._currentWindowStart, // Absolute start of the window
                    Count = windowEnd - self._currentWindowStart
                },
                SegmentB = new BufferSegmentMeta<TValueB>
                {
                    Source = self._scannerB.SourceArray,
                    Start = self._currentWindowStart, // Absolute start of the window
                    Count = windowEnd - self._currentWindowStart
                }
            };
            return true;
        }
    }
}