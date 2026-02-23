using Rayforge.Core.Collections.Abstractions;
using System;

namespace Rayforge.Core.Rendering.Abstractions
{
    /// <summary>
    /// Defines a non-generic contract for metadata stores to allow centralized management.
    /// Enables the Registry to perform mass operations (Reset, GPU Sync) without knowing the specific TValue type.
    /// </summary>
    public interface IMetadataStore
    {
        /// <summary>
        /// Gets the total number of elements the store can hold.
        /// </summary>
        int Capacity { get; }

        /// <summary>
        /// Gets the byte size of a single element in the underlying data store.
        /// Directly used as the 'stride' parameter when creating a ComputeBuffer.
        /// </summary>
        int Stride { get; }

        /// <summary>
        /// Gets a value indicating whether any data segments have been modified and require synchronization.
        /// </summary>
        bool AnyDirty { get; }

        /// <summary>
        /// Gets the underlying data as a raw Array.
        /// Use this for untyped operations like ComputeBuffer.SetData.
        /// </summary>
        Array RawData { get; }

        /// <summary>
        /// Resets the store to its initial state, clearing all data and dirty flags.
        /// Essential for full scene reloads or clearing the registry.
        /// </summary>
        void Reset();

        /// <summary>
        /// Clears all dirty segment markers. 
        /// Typically called automatically after a successful GPU synchronization.
        /// </summary>
        void ClearDirty();

        /// <summary>
        /// Marks all segments as dirty, forcing a full synchronization of the entire data set.
        /// Useful for recovering from a lost graphics context or initial buffer filling.
        /// </summary>
        void MarkAllDirty();

        /// <summary>
        /// Scans for modified segments and invokes a callback for each contiguous range.
        /// Bridges the gap between the typed CPU array and the untyped GPU upload call.
        /// </summary>
        /// <param name="uploadCallback">A delegate receiving the raw Array, the start index, and the element count.</param>
        void ProcessDirtyBatches(Action<Array, int, int> uploadCallback);

        /// <summary>
        /// Returns an iterator over the indices of all segments marked as modified.
        /// Allows external systems to inspect changes for custom logic or compute dispatching.
        /// </summary>
        /// <returns>An enumerable of dirty batch indices.</returns>
        IIterator<int> GetDirtyBatchIndices();
    }
}