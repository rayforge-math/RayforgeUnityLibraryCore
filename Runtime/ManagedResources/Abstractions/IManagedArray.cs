namespace Rayforge.Core.ManagedResources.Abstractions
{
    /// <summary>
    /// Interface for a managed resource array (GPU or CPU) that requires explicit data transfer.
    /// Provides methods for allocation management and element-wise access.
    /// </summary>
    /// <typeparam name="TElement">The type of the elements stored in the array.</typeparam>
    public interface IManagedArray<TElement>
    {
        /// <summary>
        /// Gets the total number of elements currently allocated in the array.
        /// </summary>
        int Count { get; }

        /// <summary>
        /// Releases all allocated GPU/CPU resources associated with this array.
        /// </summary>
        void Release();

        /// <summary>
        /// Sets the data at a specific index using the provided element.
        /// This typically involves a GPU upload or memory copy.
        /// </summary>
        /// <param name="index">The zero-based index of the element to set.</param>
        /// <param name="element">The data to upload/set.</param>
        void SetElement(int index, TElement element);

        /// <summary>
        /// Copies data from the array at a specific index into the provided element reference.
        /// This typically involves a GPU download or an efficient memory copy.
        /// </summary>
        /// <param name="index">The zero-based index of the element to copy.</param>
        /// <param name="element">The destination where the data will be copied to.</param>
        void CopyElementTo(int index, ref TElement element);
    }
}