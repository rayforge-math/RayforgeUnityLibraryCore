using System;
using System.Collections.Generic;

namespace Rayforge.Core.Collections.Abstractions
{
    /// <summary>
    /// A clean, type-safe interface for custom iterators.
    /// Hides internal state implementation (like TState) from the caller.
    /// </summary>
    /// <typeparam name="TType">The type of the objects being iterated.</typeparam>
    public interface IIterator<TType> : IEnumerator<TType>, IEnumerable<TType>, IDisposable
    {
        /// <summary>
        /// Indicates if there are more elements to process.
        /// By contract, accessing this property may advance internal state pointers 
        /// to the next valid element to optimize subsequent MoveNext() calls.
        /// </summary>
        bool HasNext { get; }

        /// <summary>
        /// Attempts to look at the next element without advancing the iterator.
        /// Critical for synchronizing multiple iterators (e.g., Render vs Culling buffers).
        /// Following the Eager-Fetch contract, this returns the same element as the next MoveNext().
        /// </summary>
        /// <param name="result">The next element if available; otherwise default.</param>
        /// <returns>True if a next element exists.</returns>
        bool TryPeekNext(out TType result);

        /// <summary>
        /// Allows the 'foreach' pattern to work on the interface directly.
        /// Returns the interface itself as the enumerator.
        /// </summary>
        /// <returns>The current iterator instance.</returns>
        new IIterator<TType> GetEnumerator();

        /// <summary>
        /// Provides an empty iterator for the specified type.
        /// </summary>
        public static IIterator<TType> Empty() => EmptyState<TType>.Self;
    }
}