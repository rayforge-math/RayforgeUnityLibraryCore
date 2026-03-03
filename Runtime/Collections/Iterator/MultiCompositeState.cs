using Rayforge.Core.Collections.Abstractions;
using System;

namespace Rayforge.Core.Collections.Iterator
{
    /// <summary>
    /// State that bundles multiple Iterators and treats them as a single continuous stream.
    /// English: Compatible with the universal Iterator struct. Useful for merging different data sources (e.g., static vs. dynamic entities).
    /// </summary>
    /// <typeparam name="T">The element type.</typeparam>
    public struct MultiCompositeState<T> : IIterationLogic<T, MultiCompositeState<T>>
    {
        private readonly IIterator<T>[] _sources;
        private int _index;

        /// <summary>
        /// Initializes the composite state with an array of source iterators.
        /// </summary>
        /// <param name="sources">The iterators to be processed sequentially.</param>
        public MultiCompositeState(params IIterator<T>[] sources)
        {
            _sources = sources ?? throw new ArgumentNullException(nameof(sources));
            _index = 0;
        }

        /// <summary>
        /// Checks if any of the remaining iterators in the chain have more elements.
        /// This will skip empty iterators until it finds one with data or reaches the end.
        /// </summary>
        /// <param name="self">Reference to the current state.</param>
        /// <returns>True if a subsequent element is available.</returns>
        public bool HasNext(ref MultiCompositeState<T> self)
        {
            MoveBeforeNext(ref self);
            return self._index < self._sources.Length && self._sources[self._index].HasNext;
        }

        /// <summary>
        /// Allows peeking into the current active sub-iterator.
        /// Crucial for keeping composite streams in sync with other data sources.
        /// </summary>
        public bool TryPeekNext(ref MultiCompositeState<T> self, out T result)
        {
            MoveBeforeNext(ref self);

            if (self._index < self._sources.Length)
            {
                return self._sources[self._index].TryPeekNext(out result);
            }

            result = default;
            return false;
        }

        /// <summary>
        /// Advances the stream to the next element, switching to the next source if necessary.
        /// </summary>
        /// <param name="self">Reference to the current state.</param>
        /// <param name="result">The found element.</param>
        /// <returns>True if an element was found; false if all sources are exhausted.</returns>
        public bool MoveNext(ref MultiCompositeState<T> self, out T result)
        {
            MoveBeforeNext(ref self);

            if (self._index < self._sources.Length)
            {
                if (self._sources[self._index].MoveNext())
                {
                    result = self._sources[self._index].Current;
                    return true;
                }
            }

            result = default;
            return false;
        }

        /// <summary>
        /// Fast-forwards the index to the first iterator that reports HasNext.
        /// This aligns the state so that the current _index is the one to be consumed.
        /// </summary>
        private static void MoveBeforeNext(ref MultiCompositeState<T> self)
        {
            while (self._index < self._sources.Length && !self._sources[self._index].HasNext)
            {
                self._sources[self._index].Dispose();
                self._index++;
            }
        }
    }
}