using System;

namespace Rayforge.Core.Collections.Abstractions
{
    /// <summary>
    /// A composite container that synchronizes dirty segments from two related data streams.
    /// Provides aligned memory ranges for a primary and a secondary buffer 
    /// that are processed within the same synchronization window.
    /// </summary>
    public struct SyncedSegmentMeta<TValueA, TValueB>
        where TValueA : unmanaged
        where TValueB : unmanaged
    {
        /// <summary> The dirty range for the first data stream (e.g., Spatial/Culling). </summary>
        public BufferSegmentMeta<TValueA> SegmentA;

        /// <summary> The dirty range for the second data stream (e.g., Visual/Render). </summary>
        public BufferSegmentMeta<TValueB> SegmentB;

        /// <summary> Determines if either of the segments contains pending changes. </summary>
        public readonly bool HasWork => !SegmentA.IsEmpty || !SegmentB.IsEmpty;

        /// <summary> Returns the absolute minimum start index of the shared window. </summary>
        public readonly int Start
        {
            get
            {
                if (SegmentA.IsEmpty) return SegmentB.Start;
                if (SegmentB.IsEmpty) return SegmentA.Start;
                return Math.Min(SegmentA.Start, SegmentB.Start);
            }
        }

        /// <summary> Returns the absolute maximum end index (exclusive) of the shared window. </summary>
        public readonly int End
        {
            get
            {
                if (SegmentA.IsEmpty) return SegmentB.End;
                if (SegmentB.IsEmpty) return SegmentA.End;
                return Math.Max(SegmentA.End, SegmentB.End);
            }
        }

        /// <summary> Returns the total span from the very first dirty element to the very last. </summary>
        public readonly int TotalSpan => End - Start;
    }
}