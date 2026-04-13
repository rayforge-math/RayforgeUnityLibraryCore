using Rayforge.Core.Collections.Abstractions;
using System;
using System.Runtime.CompilerServices;

namespace Rayforge.Core.Collections.Iterator
{
    /// <summary>
    /// State that bundles multiple Iterators and treats them as a single continuous stream.
    /// Compatible with the universal Iterator struct. 
    /// NOTE: Intended for cold paths as it involves array allocation and interface dispatch.
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
            _sources = sources ?? Array.Empty<IIterator<T>>();
            _index = 0;
        }

        /// <summary>
        /// Checks if any of the remaining iterators in the chain have more elements.
        /// Skips empty iterators until it finds one with data or reaches the end.
        /// </summary>
        public bool HasNext(ref MultiCompositeState<T> self)
        {
            MoveBeforeNext(ref self);
            return self._index < self._sources.Length && self._sources[self._index].HasNext;
        }

        /// <summary>
        /// Allows peeking into the current active sub-iterator.
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
        public bool MoveNext(ref MultiCompositeState<T> self, out T result)
        {
            MoveBeforeNext(ref self);

            if (self._index < self._sources.Length)
            {
                var current = self._sources[self._index];
                if (current.MoveNext())
                {
                    result = current.Current;
                    return true;
                }
            }

            result = default;
            return false;
        }

        /// <summary>
        /// Fast-forwards the index to the first iterator that reports HasNext.
        /// Aligns the state so that the current _index is the one to be consumed.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void MoveBeforeNext(ref MultiCompositeState<T> self)
        {
            var sources = self._sources;
            while (self._index < sources.Length)
            {
                var current = sources[self._index];

                // Skip null or empty iterators
                if (current != null && current.HasNext)
                {
                    return;
                }

                // Cleanup and move to next source
                current?.Dispose();
                self._index++;
            }
        }
    }
}