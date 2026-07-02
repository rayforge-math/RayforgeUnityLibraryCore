using System;

namespace Rayforge.Core.Collections.Abstractions
{
    /// <summary>
    /// Provides a unified access layer for raw data buffers, supporting 
    /// legacy Unity APIs, typed access, and high-performance memory spans.
    /// </summary>
    /// <para>
    /// This interface bridges current Unity C# / API limitations with future-proof 
    /// memory management. By exposing raw arrays and spans simultaneously, it 
    /// enables immediate compatibility with legacy systems while providing the 
    /// infrastructure for zero-allocation, high-performance memory operations.
    /// </para>
    /// <typeparam name="T">The element type, must be unmanaged for memory safety.</typeparam>
    public interface IRawBuffer<T> where T : unmanaged
    {
        /// <summary>
        /// 1. Array: The lowest common denominator for reflection, 
        /// UI bindings, or legacy Unity APIs.
        /// </summary>
        Array UntypedBuffer { get; }

        /// <summary>
        /// 2. T[]: The standard buffer for type-safe 
        /// CPU handling and interop.
        /// </summary>
        T[] TypedBuffer { get; }

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