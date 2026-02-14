namespace Rayforge.Core.ManagedResources.Abstractions
{
    /// <summary>
    /// Interface for managing dynamic arrays (GPU or CPU), 
    /// providing access to element operations and resizing.
    /// </summary>
    public interface IDynamicArray<in TIn, TOut>
    {
        /// <summary>
        /// Gets the total number of elements currently allocated.
        /// </summary>
        int Count { get; }

        /// <summary>
        /// Allocates or resizes the array to the specified count.
        /// </summary>
        /// <param name="count">The new desired size of the array. Must be non-negative.</param>
        /// <remarks>
        /// Implementations should include a necessity check to avoid redundant 
        /// re-allocations if the count remains unchanged.
        /// </remarks>
        void Create(int count);

        /// <summary>
        /// Explicitly releases the resource (e.g., returns lease to pool).
        /// </summary>
        void Release();
    }
}