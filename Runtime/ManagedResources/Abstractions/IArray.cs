using System;

namespace Rayforge.Core.ManagedResources.Abstractions
{
    /// <summary>
    /// Provides a read/write abstraction for a collection of elements.
    /// This interface focuses purely on element access and lifecycle state, 
    /// without assuming how the underlying storage is allocated.
    /// </summary>
    /// <typeparam name="TIn">The type used for input operations (e.g., source data).</typeparam>
    /// <typeparam name="TOut">The type used for output operations (e.g., the stored resource).</typeparam>
    public interface IArray<in TIn, TOut> : IDisposable
    {
        #region Metadata

        /// <summary>
        /// Gets the current number of elements available for access.
        /// </summary>
        int Count { get; }

        /// <summary>
        /// Returns true if the underlying resource is currently allocated and valid.
        /// </summary>
        bool IsCreated { get; }

        #endregion

        #region Accessors

        /// <summary>
        /// Updates the element at the specified index with new data.
        /// </summary>
        /// <param name="index">The zero-based index of the element.</param>
        /// <param name="data">The input data to apply.</param>
        void Set(int index, TIn data);

        /// <summary>
        /// Retrieves the element at the specified index.
        /// </summary>
        /// <param name="index">The zero-based index.</param>
        /// <param name="result">The reference to store the output data.</param>
        void Get(int index, ref TOut result);

        #endregion

        #region Lifecycle

        /// <summary>
        /// Explicitly invalidates the array and releases its handle or reference.
        /// </summary>
        void Release();

        #endregion
    }
}