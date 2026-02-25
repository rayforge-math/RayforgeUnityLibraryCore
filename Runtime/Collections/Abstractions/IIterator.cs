using Rayforge.Core.Collections.Iterator;
using System;
using System.Collections.Generic;

namespace Rayforge.Core.Collections.Abstractions
{
    /// <summary>
    /// A clean, type-safe interface for custom iterators.
    /// Hides internal state implementation (like TState) from the caller.
    /// </summary>
    /// <typeparam name="TType">The type of the objects being iterated.</typeparam>
    public interface IIterator<out TType> : IEnumerator<TType>, IEnumerable<TType>, IDisposable
    {
        /// <summary>
        /// Allows the 'foreach' pattern to work on the interface directly.
        /// Returns the interface itself as the enumerator.
        /// </summary>
        /// <returns>The current iterator instance.</returns>
        new IIterator<TType> GetEnumerator();

        /// <summary>
        /// Provides a zero-allocation empty iterator for the specified type.
        /// This allows syntax like: IIterator<int>.Empty
        /// </summary>
        public static IIterator<TType> Empty => EmptyState<TType>.Self;
    }
}