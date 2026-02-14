namespace Rayforge.Core.ManagedResources.Abstractions
{
    /// <summary>
    /// Interface for a managed resource array (GPU or CPU) that requires explicit data transfer.
    /// Provides methods for allocation management and element-wise access.
    /// </summary>
    /// <typeparam name="TIn">The type used for setting/uploading data.</typeparam>
    /// <typeparam name="TOut">The type used for getting/downloading data.</typeparam>
    public interface IManagedArray<in TIn, TOut>
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
        /// Sets the data at a specific index using the provided input element.
        /// </summary>
        /// <param name="index">The zero-based index.</param>
        /// <param name="element">The input data (e.g., a Texture or a Struct).</param>
        void SetElement(int index, TIn element);

        /// <summary>
        /// Copies data from the array at a specific index into the output reference.
        /// </summary>
        /// <param name="index">The zero-based index.</param>
        /// <param name="element">The destination reference for the output data.</param>
        void CopyElementTo(int index, ref TOut element);
    }
}