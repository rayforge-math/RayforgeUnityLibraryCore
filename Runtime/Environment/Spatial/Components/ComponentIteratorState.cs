using Rayforge.Core.Collections.Abstractions;
using System;
using System.Collections.Generic;

namespace Rayforge.Core.Environment.Spatial.Components
{
    /// <summary>
    /// A generic, zero-allocation state container that resolves keys from any struct-based enumerator
    /// into actual values by accessing a storage dictionary directly.
    /// </summary>
    /// <typeparam name="TKey">The identifier type (e.g., int for InstanceID).</typeparam>
    /// <typeparam name="TValue">The resolved value type (e.g., MeshRenderer).</typeparam>
    /// <typeparam name="TEnumerator">The specific enumerator type to avoid boxing.</typeparam>
    public struct ComponentIteratorState<TKey, TValue, TEnumerator> : IIterationLogic<TValue, ComponentIteratorState<TKey, TValue, TEnumerator>>
        where TKey : struct, IEquatable<TKey>
        where TEnumerator : struct, IEnumerator<TKey>
    {
        #region Fields

        /// <summary>
        /// The internal enumerator for the keys. 
        /// Must be public for the Iterator-Wrapper to access it via reference.
        /// </summary>
        public TEnumerator _bucketEnumerator;

        /// <summary>
        /// Reference to the storage dictionary for O(1) resolution.
        /// </summary>
        private readonly Dictionary<TKey, ComponentState<TValue>> m_Storage;

        private TValue _cachedValue;
        private bool _hasCachedValue;
        private bool _isExhausted;

        #endregion

        #region Constructor

        /// <summary>
        /// Initializes a new instance of the iterator state.
        /// </summary>
        /// <param name="bucketEnumerator">The specific enumerator (e.g. HashSet{int}.Enumerator).</param>
        /// <param name="storage">The dictionary used to resolve keys to values.</param>
        public ComponentIteratorState(TEnumerator bucketEnumerator, Dictionary<TKey, ComponentState<TValue>> storage)
        {
            _bucketEnumerator = bucketEnumerator;
            m_Storage = storage ?? throw new ArgumentNullException(nameof(storage));
            _cachedValue = default;
            _hasCachedValue = false;
            _isExhausted = false;
        }

        #endregion

        #region IIterationLogic Implementation

        /// <inheritdoc />
        public bool HasNext(ref ComponentIteratorState<TKey, TValue, TEnumerator> self)
        {
            MoveBeforeNext(ref self);
            return self._hasCachedValue;
        }

        /// <inheritdoc />
        public bool TryPeekNext(ref ComponentIteratorState<TKey, TValue, TEnumerator> self, out TValue result)
        {
            MoveBeforeNext(ref self);
            result = self._cachedValue;
            return self._hasCachedValue;
        }

        /// <inheritdoc />
        public bool MoveNext(ref ComponentIteratorState<TKey, TValue, TEnumerator> self, out TValue result)
        {
            MoveBeforeNext(ref self);

            if (self._hasCachedValue)
            {
                result = self._cachedValue;
                self._cachedValue = default;
                self._hasCachedValue = false;
                return true;
            }

            result = default;
            return false;
        }

        #endregion

        #region Internal Logic

        /// <summary>
        /// Advances the generic enumerator and pre-resolves the value from storage.
        /// </summary>
        private static void MoveBeforeNext(ref ComponentIteratorState<TKey, TValue, TEnumerator> self)
        {
            if (self._hasCachedValue || self._isExhausted) return;

            while (self._bucketEnumerator.MoveNext())
            {
                TKey currentKey = self._bucketEnumerator.Current;
                if (self.m_Storage != null && self.m_Storage.TryGetValue(currentKey, out var value))
                {
                    self._cachedValue = value.component;
                    self._hasCachedValue = true;
                    return;
                }
            }

            self._isExhausted = true;
        }

        #endregion
    }
}