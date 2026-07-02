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
        private readonly bool _isInitialized;

        /// <summary>
        /// Initializes the composite state with an array of source iterators.
        /// </summary>
        /// <param name="sources">The iterators to be processed sequentially.</param>
        public MultiCompositeState(params IIterator<T>[] sources)
        {
            if (sources == null)
                throw new ArgumentNullException(nameof(sources), "The sources array cannot be null.");

            for (int i = 0; i < sources.Length; i++)
            {
                if (sources[i] == null)
                    throw new ArgumentNullException(nameof(sources), $"Iterator at index {i} cannot be null.");
            }

            _sources = sources;
            _index = 0;
            _isInitialized = true;
        }

        /// <inheritdoc />
        public bool HasNext(ref MultiCompositeState<T> self)
        {
            if (!self._isInitialized) return false;
            MoveBeforeNext(ref self);
            return self._index < self._sources.Length && self._sources[self._index].HasNext;
        }

        /// <inheritdoc />
        public bool TryPeekNext(ref MultiCompositeState<T> self, out T result)
        {
            if (!self._isInitialized)
            {
                result = default;
                return false;
            }
            MoveBeforeNext(ref self);

            if (self._index < self._sources.Length)
            {
                return self._sources[self._index].TryPeekNext(out result);
            }

            result = default;
            return false;
        }

        /// <inheritdoc />
        public bool MoveNext(ref MultiCompositeState<T> self, out T result)
        {
            if (!self._isInitialized)
            {
                result = default;
                return false;
            }
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
        /// <param name="self">Reference to the current iterator state.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void MoveBeforeNext(ref MultiCompositeState<T> self)
        {
            if (!self._isInitialized) return;

            var sources = self._sources;
            while (self._index < sources.Length)
            {
                var current = sources[self._index];

                // Skip null iterators or iterators without elements
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