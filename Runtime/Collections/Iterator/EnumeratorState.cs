using Rayforge.Core.Collections.Abstractions;
using System.Collections.Generic;

namespace Rayforge.Core.Collections.Iterator
{
    /// <summary>
    /// Wraps a standard <typeparamref name="TEnumerator"/> to make it compatible with the universal Iterator.
    /// Acts as a robust bridge that handles uninitialized states safely.
    /// </summary>
    public struct EnumeratorState<T, TEnumerator> : IIterationLogic<T, EnumeratorState<T, TEnumerator>>
        where TEnumerator : struct, IEnumerator<T>
    {
        private TEnumerator _enumerator;
        private T _cachedItem;
        private bool _hasCachedItem;
        private bool _isExhausted;
        private bool _isInitialized;

        /// <summary>
        /// Initializes the state with a concrete struct enumerator.
        /// </summary>
        /// <param name="enumerator">The struct enumerator to wrap.</param>
        public EnumeratorState(TEnumerator enumerator)
        {
            _enumerator = enumerator;
            _cachedItem = default;
            _hasCachedItem = false;
            _isExhausted = false;
            _isInitialized = !EqualityComparer<TEnumerator>.Default.Equals(enumerator, default);
        }

        /// <inheritdoc />
        public bool HasNext(ref EnumeratorState<T, TEnumerator> self)
        {
            if (!self._isInitialized) return false;
            FetchNext(ref self);
            return self._hasCachedItem;
        }

        /// <inheritdoc />
        public bool TryPeekNext(ref EnumeratorState<T, TEnumerator> self, out T result)
        {
            if (!self._isInitialized)
            {
                result = default;
                return false;
            }
            FetchNext(ref self);
            result = self._cachedItem;
            return self._hasCachedItem;
        }

        /// <inheritdoc />
        public bool MoveNext(ref EnumeratorState<T, TEnumerator> self, out T result)
        {
            if (!self._isInitialized)
            {
                result = default;
                return false;
            }

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
        /// Bridges the gap between Peek-less IEnumerator and our Peek-ready system.
        /// Only calls MoveNext() on the underlying enumerator if the cache is empty.
        /// </summary>
        private static void FetchNext(ref EnumeratorState<T, TEnumerator> self)
        {
            if(!self._isInitialized || self._hasCachedItem || self._isExhausted) return;

            try
            {
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
            catch
            {
                self._isExhausted = true;
                throw;
            }
        }
    }
}