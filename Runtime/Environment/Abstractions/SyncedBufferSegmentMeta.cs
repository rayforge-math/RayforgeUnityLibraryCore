using Rayforge.Core.Collections.Abstractions;
using System;

namespace Rayforge.Core.Environment.Abstractions
{
    /// <summary>
    /// A composite container that synchronizes dirty segments from two related data streams.
    /// Provides aligned memory ranges for a primary and a secondary buffer 
    /// that are processed within the same synchronization window.
    /// </summary>
    public struct SyncedBufferSegmentMeta
    {
        /// <summary>
        /// The dirty range for the first data stream (e.g., Culling, Physics, or Logic).
        /// </summary>
        public BufferSegmentMeta SegmentA;

        /// <summary>
        /// The dirty range for the second data stream (e.g., Render, Interpolation, or Bake).
        /// </summary>
        public BufferSegmentMeta SegmentB;

        #region Semantic View (Culling & Render)

        /// <summary>
        /// Explicit alias for SegmentA. Use this for spatial/culling logic.
        /// Points to the same memory range as SegmentA.
        /// </summary>
        public readonly BufferSegmentMeta Culling => SegmentA;

        /// <summary>
        /// Explicit alias for SegmentB. Use this for visual/rendering logic.
        /// Points to the same memory range as SegmentB.
        /// </summary>
        public readonly BufferSegmentMeta Render => SegmentB;

        #endregion

        /// <summary>
        /// Helper property to determine if either of the segments contains pending changes.
        /// </summary>
        public bool HasWork => SegmentA.Count > 0 || SegmentB.Count > 0;

        /// <summary>
        /// Returns the absolute minimum start index of the shared window.
        /// Ensures we get the correct start even if one segment is empty (Count=0).
        /// </summary>
        public int Start
        {
            get
            {
                if (SegmentA.Count > 0 && SegmentB.Count > 0) return Math.Min(SegmentA.Start, SegmentB.Start);
                if (SegmentA.Count > 0) return SegmentA.Start;
                if (SegmentB.Count > 0) return SegmentB.Start;
                return 0;
            }
        }

        /// <summary>
        /// Returns the absolute maximum end index (exclusive) of the shared window.
        /// </summary>
        public int End
        {
            get
            {
                int endA = SegmentA.Count > 0 ? SegmentA.Start + SegmentA.Count : 0;
                int endB = SegmentB.Count > 0 ? SegmentB.Start + SegmentB.Count : 0;
                return Math.Max(endA, endB);
            }
        }

        /// <summary>
        /// Returns the total span from the very first dirty element to the very last.
        /// </summary>
        public int TotalSpan => HasWork ? (End - Start) : 0;
    }
}