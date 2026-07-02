using UnityEngine;

namespace Rayforge.Core.Collections.Abstractions
{
    /// <summary>
    /// A lightweight container representing a synchronized pair of values at a specific index.
    /// <para>
    /// SYNOPSIS: Holds the data payload from two parallel streams along with their 
    /// absolute and relative position information within the buffer.
    /// </para>
    /// </summary>
    public readonly struct SyncedArrayMeta<TValueA, TValueB>
    {
        /// <summary> The absolute index in the underlying backing array. </summary>
        public readonly int AbsoluteIndex;

        /// <summary> The index relative to the start of the iterator's range. </summary>
        public readonly int RelativeIndex;

        /// <summary> The value from the primary data stream. </summary>
        public readonly TValueA ValueA;

        /// <summary> The value from the secondary data stream. </summary>
        public readonly TValueB ValueB;

        /// <summary>
        /// Initializes a new instance of the <see cref="SyncedArrayMeta{TValueA, TValueB}"/> struct.
        /// </summary>
        public SyncedArrayMeta(int absoluteIndex, int relativeIndex, TValueA valueA, TValueB valueB)
        {
            AbsoluteIndex = absoluteIndex;
            RelativeIndex = relativeIndex;
            ValueA = valueA;
            ValueB = valueB;
        }
    }
}
