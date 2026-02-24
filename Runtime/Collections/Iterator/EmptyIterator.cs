using Rayforge.Core.Collections.Abstractions;

namespace Rayforge.Core.Collections.Iterator
{
    /// <summary>
    /// Provides a globally accessible empty iterator to prevent null-reference exceptions.
    /// </summary>
    public struct EmptyState<T> : IIterationLogic<T, EmptyState<T>>
    {
        public static readonly Iterator<T, EmptyState<T>> Self = new Iterator<T, EmptyState<T>>();

        /// <summary>
        /// A state that never finds an element. 
        /// </summary>
        public bool MoveNext(ref EmptyState<T> state, out T result)
        {
            result = default;
            return false;
        }
    }
}