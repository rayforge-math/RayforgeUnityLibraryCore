namespace Rayforge.Core.ManagedResources.Abstractions
{
    /// <summary>
    /// Represents a fixed-size hardware resource (GPU/CPU).
    /// Focuses on data transfer and lifecycle.
    /// </summary>
    public interface IManagedArray<in TIn, TOut>
    {
        /// <summary>
        /// Gets the total number of elements currently allocated.
        /// </summary>
        int Count { get; }

        /// <summary>
        /// Releases all allocated GPU/CPU resources associated with this array.
        /// </summary>
        void Release();

        /// <summary>
        /// Sets data at a specific index using the TIn type.
        /// </summary>
        public void SetElement(int index, TIn element);

        /// <summary>
        /// Copies data from a specific index into a TOut reference.
        /// </summary>
        public void CopyElementTo(int index, ref TOut element);
    }
}