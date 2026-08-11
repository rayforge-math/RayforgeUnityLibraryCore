using System;
using System.Collections;

namespace Rayforge.Core.Collections.Abstractions
{
    /// <summary>
    /// Provides read-only diagnostic and access capabilities for a metadata store.
    /// Used by systems that need to inspect store state without modifying it.
    /// </summary>
    public interface IBufferMetadata
    {
        #region General Properties

        /// <summary> Gets the total number of elements the store can hold. </summary>
        int Capacity { get; }

        /// <summary> Gets the byte size of a single element for GPU buffer creation (stride). </summary>
        int Stride { get; }

        /// <summary> Gets the number of elements per dirty-tracking segment. </summary>
        int BatchSize { get; }

        /// <summary> Gets the total number of batches in the buffer. </summary>
        int TotalBatchCount { get; }

        /// <summary> Indicates if any data segments have been modified and require synchronization. </summary>
        bool AnyDirty { get; }

        #endregion
    }
}