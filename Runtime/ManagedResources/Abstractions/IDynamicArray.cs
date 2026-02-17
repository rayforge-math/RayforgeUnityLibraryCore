namespace Rayforge.Core.ManagedResources.Abstractions
{
    /// <summary>
    /// Extends <see cref="IArray{TIn, TOut}"/> with capabilities to define 
    /// and modify the structure of the underlying storage.
    /// </summary>
    public interface IDynamicArray<in TIn, TOut> : IArray<TIn, TOut>
    {
        /// <summary>
        /// Allocates or re-initializes the array to accommodate the specified number of elements.
        /// </summary>
        /// <param name="count">The number of elements to allocate.</param>
        void Reallocate(int count);
    }
}