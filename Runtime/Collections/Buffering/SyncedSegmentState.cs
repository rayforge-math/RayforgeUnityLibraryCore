using Rayforge.Core.Collections.Abstractions;
using System;

namespace Rayforge.Core.Collections.Buffering
{
    /// <summary>
    /// A synchronized iterator state that merges two independent linear buffer scanners into aligned windows.
    /// Enables full-buffer processing by enforcing grid-based slicing without dirty-state tracking.
    /// </summary>
    /// <typeparam name="TValueA">The unmanaged type of the first buffer.</typeparam>
    /// <typeparam name="TValueB">The unmanaged type of the second buffer.</typeparam>
    public struct SyncedSegmentState<TValueA, TValueB>
        : IIterationLogic<SyncedSegmentMeta<TValueA, TValueB>, SyncedSegmentState<TValueA, TValueB>>
        where TValueA : unmanaged
        where TValueB : unmanaged
    {
        private BufferSegmentState<TValueA> _scannerA;
        private BufferSegmentState<TValueB> _scannerB;

        private int _currentWindowStart;
        private readonly int _syncWindow;
        private readonly int _totalCapacity;

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
            int offset, int size, int batchSize, int windowSize)
        {
            // 1. Validation Checks
            if (sourceA == null || sourceB == null)
                throw new ArgumentNullException("Source arrays cannot be null.");

            if (offset < 0 || offset >= sourceA.Length || offset >= sourceB.Length)
                throw new ArgumentOutOfRangeException(nameof(offset), "Offset must be within array bounds.");

            if (size < 0 || (offset + size) > sourceA.Length || (offset + size) > sourceB.Length)
                throw new ArgumentOutOfRangeException(nameof(size), "Size exceeds buffer boundaries.");

            if (batchSize < 0)
                throw new ArgumentOutOfRangeException(nameof(batchSize), "Batch size cannot be negative.");

            if (windowSize <= 0)
                throw new ArgumentOutOfRangeException(nameof(windowSize), "Window size must be greater than zero.");

            // 2. Internal Scanner Creation
            // This ensures both scanners share identical configuration, preventing divergence.
            _scannerA = new BufferSegmentState<TValueA>(sourceA, offset, size, batchSize);
            _scannerB = new BufferSegmentState<TValueB>(sourceB, offset, size, batchSize);

            _syncWindow = windowSize;
            _totalCapacity = size;
            _currentWindowStart = 0;
        }

        /// <summary>
        /// Checks if there are more segments to process within the buffer capacity.
        /// </summary>
        /// <param name="self">Reference to the current iterator state.</param>
        /// <returns>True if the window start is within capacity; otherwise, false.</returns>
        public bool HasNext(ref SyncedSegmentState<TValueA, TValueB> self)
            => self._currentWindowStart < self._totalCapacity;

        /// <summary>
        /// Advances the iterator to the next synchronized segment window.
        /// </summary>
        /// <param name="self">Reference to the current iterator state.</param>
        /// <param name="result">The synchronized segment metadata for the window.</param>
        /// <returns>True if a window was successfully created; false if end of capacity is reached.</returns>
        public bool MoveNext(ref SyncedSegmentState<TValueA, TValueB> self, out SyncedSegmentMeta<TValueA, TValueB> result)
        {
            if (self._currentWindowStart >= self._totalCapacity)
            {
                result = default;
                return false;
            }

            int windowEnd = Math.Min(self._currentWindowStart + self._syncWindow, self._totalCapacity);

            result = new SyncedSegmentMeta<TValueA, TValueB> {
                SegmentA = new BufferSegmentMeta<TValueA>
                {
                    Source = self._scannerA.SourceArray,
                    Start = self._currentWindowStart,
                    Count = windowEnd - self._currentWindowStart
                },
                SegmentB = new BufferSegmentMeta<TValueB>
                {
                    Source = self._scannerB.SourceArray,
                    Start = self._currentWindowStart,
                    Count = windowEnd - self._currentWindowStart
                }            
            };

            self._currentWindowStart = windowEnd;
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
            if (self._currentWindowStart >= self._totalCapacity)
            {
                result = default;
                return false;
            }

            int windowEnd = Math.Min(self._currentWindowStart + self._syncWindow, self._totalCapacity);

            result = new SyncedSegmentMeta<TValueA, TValueB> {
                SegmentA = new BufferSegmentMeta<TValueA>
                {
                    Source = self._scannerA.SourceArray,
                    Start = self._currentWindowStart,
                    Count = windowEnd - self._currentWindowStart
                },
                SegmentB = new BufferSegmentMeta<TValueB>
                {
                    Source = self._scannerB.SourceArray,
                    Start = self._currentWindowStart,
                    Count = windowEnd - self._currentWindowStart
                }            
            };
            return true;
        }
    }
}