using System;

namespace Rayforge.Core.Collections.Abstractions
{
    /// <summary>
    /// Extends read-only access with full write, array-based interop, 
    /// and modification capabilities.
    /// </summary>
    /// <typeparam name="T">The element type, must be unmanaged.</typeparam>
    public interface IRawBuffer<T> : IReadOnlyRawBuffer<T> 
        where T : unmanaged
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
        /// Sets a value at a specific index within the buffer.
        /// </summary>
        void Set(int index, T value);
    }
}