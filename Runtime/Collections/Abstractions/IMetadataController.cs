using System;
using System.Collections;

namespace Rayforge.Core.Collections.Abstractions
{
    /// <summary>
    /// Defines the contract for metadata stores, allowing centralized management,
    /// modifications, and synchronization tracking.
    /// </summary>
    public interface IMetadataController
    {
        #region State Management

        /// <summary>
        /// Resets the store to its initial state, clearing all data and dirty flags.
        /// Essential for full scene reloads.
        /// </summary>
        void Clear();

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
        /// Resizes the internal storage and mappings to a new capacity.
        /// </summary>
        /// <param name="newCapacity">The new maximum capacity.</param>
        void Resize(int newCapacity);

        /// <summary>
        /// Updates the batch size used for dirty-tracking segments.
        /// </summary>
        /// <param name="newBatchSize">The new size for tracking segments.</param>
        void UpdateBatchSize(int newBatchSize);

        /// <summary> Provides access to the dirty bit tracking state. </summary>
        BitArray DirtyBits { get; }

        /// <summary> Gets the underlying data as a raw array for low-level GPU operations. </summary>
        Array UntypedBuffer { get; }

        #endregion
    }
}