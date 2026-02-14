namespace Rayforge.Core.ManagedResources.Abstractions
{
    /// <summary>
    /// Interface for managing dynamic arrays (GPU or CPU), 
    /// providing access to element operations and resizing.
    /// </summary>
    public interface IDynamicArray<TElement>
    {
        /// <summary>
        /// Gets the total number of elements currently allocated.
        /// </summary>
        int Count { get; }

        /// <summary>
        /// Allocates or resizes the array to the specified count.
        /// </summary>
        /// <param name="count">The new desired size of the array. Must be non-negative.</param>
        /// <param name="preserve">
        /// If true, the implementation should attempt to copy existing data from the old 
        /// buffer to the new one.
        /// </param>
        /// <remarks>
        /// Implementations should include a necessity check to avoid redundant 
        /// re-allocations if the count remains unchanged.
        /// </remarks>
        void Create(int count, bool preserve = false);
    }
}