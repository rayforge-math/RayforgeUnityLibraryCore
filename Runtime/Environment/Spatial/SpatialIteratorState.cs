namespace Rayforge.Core.Environment.Spatial
{
    /// <summary>
    /// A specialized state container that wraps a spatial key and its corresponding internal enumerator.
    /// This acts as the "payload" for the universal Iterator, bridging the gap between 
    /// the spatial grid and the underlying data collection.
    /// </summary>
    /// <typeparam name="TKey">The type used for spatial indexing (e.g., Vector3Int).</typeparam>
    /// <typeparam name="TInternalState">The enumerator type of the internal collection (e.g., HashSet.Enumerator).</typeparam>
    public struct SpatialIteratorState<TKey, TInternalState>
        where TKey : struct
        where TInternalState : struct
    {
        /// <summary>
        /// The spatial identifier (e.g., grid cell coordinates) for the current iteration.
        /// </summary>
        public readonly TKey Key;

        /// <summary>
        /// The internal enumerator or state tracker for the specific data bucket.
        /// This is modified as the iteration progresses.
        /// </summary>
        public TInternalState Internal;

        /// <summary>
        /// Initializes a new instance of the SpatialIteratorState struct.
        /// </summary>
        /// <param name="key">The spatial key representing the bucket or cell.</param>
        /// <param name="internalState">The starting state of the internal enumerator.</param>
        public SpatialIteratorState(TKey key, TInternalState internalState)
        {
            Key = key;
            Internal = internalState;
        }
    }
}