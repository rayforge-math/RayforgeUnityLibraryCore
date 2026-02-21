using Rayforge.Core.Collections.Iterator;

namespace Rayforge.Core.Collections.Abstractions
{
    /// <summary>
    /// A clean, type-safe interface for custom iterators.
    /// Hides internal state implementation (like TState) from the caller.
    /// </summary>
    /// <typeparam name="TType">The type of the objects being iterated.</typeparam>
    public interface IIterator<out TType>
    {
        /// <summary>
        /// Gets the element at the current position of the iterator.
        /// </summary>
        TType Current { get; }

        /// <summary>
        /// Advances the iterator to the next element.
        /// </summary>
        /// <returns>True if the next element was successfully found; false otherwise.</returns>
        bool MoveNext();

        /// <summary>
        /// Allows the 'foreach' pattern to work on the interface directly.
        /// Returns the interface itself as the enumerator.
        /// </summary>
        /// <returns>The current iterator instance.</returns>
        IIterator<TType> GetEnumerator();
    }
}