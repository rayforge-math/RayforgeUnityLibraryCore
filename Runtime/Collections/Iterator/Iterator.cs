using Rayforge.Core.Collections.Abstractions;
using System;
using System.Collections;
using System.Collections.Generic;

namespace Rayforge.Core.Collections.Iterator
{
    /// <summary>
    /// A universal black-box iterator that encapsulates iteration logic and state.
    /// TType represents the element type, TState holds traversal data.
    /// 
    /// PERFORMANCE NOTE:
    /// This implementation is optimized for zero-allocation performance by using 
    /// the "Self-Logic" pattern (ref TState). By constraining TState to a struct and 
    /// using interface constraints, the JIT compiler can perform "Devirtualization" 
    /// and "Inlining", making this as fast as a manual for-loop.
    ///
    /// FLEXIBILITY:
    /// While the core is a high-performance struct, it can be seamlessly boxed into 
    /// an IIterator interface for IoC (Inversion of Control) or passing through 
    /// abstract APIs without changing the underlying iteration logic.
    /// </summary>
    /// <typeparam name="TType">The type of the objects being iterated.</typeparam>
    /// <typeparam name="TState">The custom state struct required to track progress.</typeparam>
    public struct Iterator<TType, TState> : IIterator<TType>
        where TState : struct, IIterationLogic<TType, TState>
    {
        private TState _state;
        private TType _current;
        private readonly bool _isInitialized;

        /// <summary>
        /// Initializes a new instance of the Iterator struct.
        /// </summary>
        /// <param name="initialState">The starting state for the iteration.</param>
        public Iterator(TState initialState)
        {
            _state = initialState;
            _current = default;
            _isInitialized = true;
        }

        /// <summary>
        /// Gets the element at the current position of the iterator.
        /// </summary>
        public TType Current => _current;

        object IEnumerator.Current => Current;

        /// <summary>
        /// Indicates if there are more elements to process.
        /// Use this to check the iterator state without advancing the iterator via MoveNext().
        /// </summary>
        public bool HasNext => _isInitialized && _state.HasNext(ref _state);

        /// <summary>
        /// Implements the Peek functionality from the interface.
        /// Delegates directly to the underlying state logic.
        /// </summary>
        public bool TryPeekNext(out TType result)
        {
            if (!_isInitialized)
            {
                result = default;
                return false;
            }
            return _state.TryPeekNext(ref _state, out result);
        }

        /// <summary>
        /// Advances the iterator to the next element by invoking the internal logic struct.
        /// </summary>
        /// <returns>True if the next element was successfully found; false otherwise.</returns>
        public bool MoveNext()
        {
            if (!_isInitialized) return false;
            return _state.MoveNext(ref _state, out _current);
        }

        /// <summary>
        /// Priority Method: Supports the 'foreach' pattern for the concrete struct type.
        /// When the compiler knows this is a struct, it picks this method, avoiding any boxing.
        /// </summary>
        /// <returns>The current iterator instance as a struct.</returns>
        public Iterator<TType, TState> GetEnumerator() => this;

        /// <summary>
        /// Fallback Method: Explicitly implements the interface method to allow foreach support via IIterator.
        /// English comment: This is only called when the iterator is treated as an IIterator interface.
        /// </summary>
        /// <returns>The current instance cast to the IIterator interface.</returns>
        IIterator<TType> IIterator<TType>.GetEnumerator() => this;

        /// <summary>
        /// Explicit IEnumerable<T> implementation for LINQ and generic usage.
        /// </summary>
        IEnumerator<TType> IEnumerable<TType>.GetEnumerator() => this;

        /// <summary>
        /// Explicit non-generic IEnumerable implementation.
        /// </summary>
        IEnumerator IEnumerable.GetEnumerator() => this;

        /// <summary>
        /// Resets the iterator to its initial state.
        /// Not supported because state-based iterators are intended to be single-pass and immutable in their initial configuration.
        /// </summary>
        void IEnumerator.Reset()
        {
            throw new NotSupportedException("Reset is not supported on state-based struct iterators. Create a new iterator instead.");
        }

        /// <summary>
        /// Cleans up any resources used by the iterator. 
        /// Required by the IDisposable interface for the foreach pattern.
        /// </summary>
        public void Dispose() { }

        /// <summary>
        /// Returns an empty iterator instance. 
        /// The _isInitialized flag ensures MoveNext immediately returns false without allocation.
        /// </summary>
        /// <returns>A default, uninitialized iterator.</returns>
        public static Iterator<TType, TState> Empty() => default;
    }
}