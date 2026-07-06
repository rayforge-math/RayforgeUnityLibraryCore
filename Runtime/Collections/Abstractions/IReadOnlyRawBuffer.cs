using System;

namespace Rayforge.Core.Collections.Abstractions
{
    /// <summary>
    /// Provides strict read-only access to a raw data buffer via spans, 
    /// ensuring that observers cannot modify the underlying storage.
    /// </summary>
    /// <typeparam name="T">The element type, must be unmanaged.</typeparam>
    public interface IReadOnlyRawBuffer<T> 
        where T : unmanaged
    {
        /// <summary>
        /// 3. ReadOnlySpan&lt;T&gt;: The modern view for zero-allocation 
        /// hot-paths and iteration logic.
        /// </summary>
        ReadOnlySpan<T> AsSpan();

        /// <summary>
        /// Gets the total number of elements in the buffer.
        /// </summary>
        int Capacity { get; }

        /// <summary>
        /// Gets the byte size of a single element in the underlying data store.
        /// Used directly as the 'stride' parameter when creating a ComputeBuffer.
        /// </summary>
        int Stride { get; }
    }
}
