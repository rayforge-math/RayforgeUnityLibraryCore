using System;

namespace Rayforge.Core.Collections.Abstractions
{
    /// <summary>
    /// A composite container that synchronizes dirty segments from two related data streams.
    /// Provides aligned memory ranges for a primary and a secondary buffer 
    /// that are processed within the same synchronization window.
    /// </summary>
    public struct SyncedBufferSegmentMeta<T>
        where T : unmanaged
    {
        /// <summary>
        /// The dirty range for the first data stream (e.g., Culling, Physics, or Logic).
        /// </summary>
        public BufferSegmentMeta<T> SegmentA;

        /// <summary>
        /// The dirty range for the second data stream (e.g., Render, Interpolation, or Bake).
        /// </summary>
        public BufferSegmentMeta<T> SegmentB;

        /// <summary>
        /// Helper property to determine if either of the segments contains pending changes.
        /// </summary>
        public readonly bool HasWork => !SegmentA.IsEmpty || !SegmentB.IsEmpty;

        /// <summary>
        /// Returns the absolute minimum start index of the shared window.
        /// </summary>
        public readonly int Start
        {
            get
            {
                bool hasA = !SegmentA.IsEmpty;
                bool hasB = !SegmentB.IsEmpty;

                if (hasA && hasB) return Math.Min(SegmentA.Start, SegmentB.Start);
                if (hasA) return SegmentA.Start;
                if (hasB) return SegmentB.Start;
                return 0;
            }
        }

        /// <summary>
        /// Returns the absolute maximum end index (exclusive) of the shared window.
        /// </summary>
        public readonly int End
        {
            get
            {
                bool hasA = !SegmentA.IsEmpty;
                bool hasB = !SegmentB.IsEmpty;

                if (hasA && hasB) return Math.Max(SegmentA.End, SegmentB.End);
                if (hasA) return SegmentA.End;
                if (hasB) return SegmentB.End;
                return 0;
            }
        }

        /// <summary>
        /// Returns the total span from the very first dirty element to the very last.
        /// </summary>
        public readonly int TotalSpan => HasWork ? (End - Start) : 0;
    }
}