using Rayforge.Core.Collections.Abstractions;
using System.Collections.Generic;

namespace Rayforge.Core.Collections.Iterator
{
    /// <summary>
    /// Wraps a standard HashSet enumerator to make it compatible with the universal Iterator.
    /// </summary>
    public struct EnumeratorState<T, TEnumerator> : IIterationLogic<T, EnumeratorState<T, TEnumerator>>
        where TEnumerator : struct, IEnumerator<T>
    {
        private TEnumerator _enumerator;
        private T _cachedItem;
        private bool _hasCachedItem;
        private bool _isExhausted;

        /// <summary>
        /// Initializes the state with a concrete struct enumerator.
        /// </summary>
        public EnumeratorState(TEnumerator enumerator)
        {
            _enumerator = enumerator;
            _cachedItem = default;
            _hasCachedItem = false;
            _isExhausted = false;
        }

        /// <summary>
        /// Checks if more elements are available by attempting to pre-fetch the next item.
        /// </summary>
        public bool HasNext(ref EnumeratorState<T, TEnumerator> self)
        {
            FetchNext(ref self);
            return self._hasCachedItem;
        }

        /// <summary>
        /// Provides access to the next element without consuming it.
        /// Essential for synchronizing HashSet/Dictionary data with array-based buffers.
        /// </summary>
        public bool TryPeekNext(ref EnumeratorState<T, TEnumerator> self, out T result)
        {
            FetchNext(ref self);
            result = self._cachedItem;
            return self._hasCachedItem;
        }

        /// <summary>
        /// Consumes the cached item or advances the enumerator if the cache is empty.
        /// </summary>
        public bool MoveNext(ref EnumeratorState<T, TEnumerator> self, out T result)
        {
            FetchNext(ref self);

            if (self._hasCachedItem)
            {
                result = self._cachedItem;
                self._cachedItem = default;
                self._hasCachedItem = false;
                return true;
            }

            self._isExhausted = true;
            result = default;
            return false;
        }

        /// <summary>
        /// Unified Fetch: Bridges the gap between Peek-less IEnumerator and our Peek-ready system.
        /// Only calls MoveNext() on the underlying enumerator if the cache is empty.
        /// </summary>
        private static void FetchNext(ref EnumeratorState<T, TEnumerator> self)
        {
            if (self._hasCachedItem || self._isExhausted) return;

            if (self._enumerator.MoveNext())
            {
                self._cachedItem = self._enumerator.Current;
                self._hasCachedItem = true;
            }
            else
            {
                self._isExhausted = true;
            }
        }
    }
}