using UnityEngine;

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
        /// Creates or resizes the array.
        /// </summary>
        /// <param name="count">New size of the array.</param>
        /// <param name="preserve">If true, attempts to keep existing data.</param>
        void Create(int count, bool preserve = false);

        /// <summary>
        /// Releases all allocated resources.
        /// </summary>
        void Release();

        /// <summary>
        /// Copies data from the array at <paramref name="index"/> into <paramref name="element"/>.
        /// Using 'ref' allows this to work efficiently for both structs and classes.
        /// </summary>
        void CopyElementTo(int index, ref TElement element);

        /// <summary>
        /// Sets the data at <paramref name="index"/> using the provided <paramref name="element"/>.
        /// </summary>
        void SetElement(int index, TElement element);
    }
}