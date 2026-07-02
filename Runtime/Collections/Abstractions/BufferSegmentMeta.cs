using System;

namespace Rayforge.Core.Collections.Abstractions
{
    /// <summary>
    /// Represents a contiguous range within a data array for GPU synchronization.
    /// Provides helper methods for index calculations and segment access.
    /// </summary>
    /// <typeparam name="T">The unmanaged type of the elements in the buffer.</typeparam>
    public struct BufferSegmentMeta<T> 
        where T : unmanaged
    {
        /// <summary>
        /// The reference to the underlying data array.
        /// </summary>
        public T[] Source;

        /// <summary>
        /// The starting index of the range.
        /// </summary>
        public int Start;

        /// <summary>
        /// The number of elements included in this segment.
        /// </summary>
        public int Count;

        /// <summary>
        /// Returns the exclusive end index (Start + Count).
        /// Useful for loop boundaries (i < End).
        /// </summary>
        public int End => Start + Count;

        /// <summary>
        /// Checks if this segment contains any elements.
        /// </summary>
        public bool IsEmpty => Count <= 0 || Source == null;

        /// <summary>
        /// Checks if a specific index falls within this segment's range.
        /// </summary>
        /// <param name="index">The index to check.</param>
        /// <returns>True if the index is within [Start, End).</returns>
        public bool Contains(int index)
        {
            return index >= Start && index < End;
        }

        /// <summary>
        /// Provides a convenient way to access the segment as a Span.
        /// </summary>
        public Span<T> AsSpan()
        {
            if (Source == null || Count <= 0) return Span<T>.Empty;
            return Source.AsSpan(Start, Count);
        }
    }
}