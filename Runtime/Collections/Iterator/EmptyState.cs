using Rayforge.Core.Collections.Iterator;

namespace Rayforge.Core.Collections.Abstractions
{
    /// <summary>
    /// Provides a globally accessible empty iterator to prevent null-reference exceptions.
    /// </summary>
    public struct EmptyState<T> : IIterationLogic<T, EmptyState<T>>
    {
        /// <summary>
        /// Static instance of an empty iterator for the specified type.
        /// Pre-initialized to avoid even the tiny cost of 'new' at runtime.
        /// </summary>
        public static readonly Iterator<T, EmptyState<T>> Self = new Iterator<T, EmptyState<T>>(default);

        /// <summary>
        /// Always returns false as there are no elements to process.
        /// </summary>
        /// <param name="state">Reference to the empty state.</param>
        /// <returns>Always false.</returns>
        public bool HasNext(ref EmptyState<T> state)
        {
            return false;
        }

        /// <summary>
        /// Always returns false as there is nothing to peek at.
        /// </summary>
        public bool TryPeekNext(ref EmptyState<T> state, out T result)
        {
            result = default;
            return false;
        }

        /// <summary>
        /// A state that never finds an element.
        /// </summary>
        /// <param name="state">Reference to the empty state.</param>
        /// <param name="result">Always returns default(T).</param>
        /// <returns>Always false.</returns>
        public bool MoveNext(ref EmptyState<T> state, out T result)
        {
            result = default;
            return false;
        }
    }
}