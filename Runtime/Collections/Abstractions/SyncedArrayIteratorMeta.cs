namespace Rayforge.Core.Collections.Abstractions
{
    /// <summary>
    /// A lightweight container representing a synchronized pair of values at a specific index.
    /// <para>
    /// SYNOPSIS: Holds the data payload from two parallel streams along with their 
    /// absolute and relative position information within the buffer.
    /// </para>
    /// </summary>
    public struct SyncedArrayIteratorMeta<TValueA, TValueB>
    {
        /// <summary> The absolute index in the underlying backing array. </summary>
        public int AbsoluteIndex;

        /// <summary> The index relative to the start of the iterator's range. </summary>
        public int RelativeIndex;

        /// <summary> The value from the primary data stream. </summary>
        public TValueA ValueA;

        /// <summary> The value from the secondary data stream. </summary>
        public TValueB ValueB;

        /// <summary>
        /// Initializes a new instance of the <see cref="SyncedArrayIteratorMeta{TValueA, TValueB}"/> struct.
        /// </summary>
        public SyncedArrayIteratorMeta(int absoluteIndex, int relativeIndex, TValueA valueA, TValueB valueB)
        {
            AbsoluteIndex = absoluteIndex;
            RelativeIndex = relativeIndex;
            ValueA = valueA;
            ValueB = valueB;
        }
    }
}
