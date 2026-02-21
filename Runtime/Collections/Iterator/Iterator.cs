using Rayforge.Core.Collections.Abstractions;

namespace Rayforge.Core.Collections.Iterator
{
    /// <summary>
    /// A universal black-box iterator that encapsulates iteration logic and state.
    /// TType represents the element type, TState holds the traversal data (e.g., indices, references).
    /// </summary>
    /// <typeparam name="TType">The type of the objects being iterated.</typeparam>
    /// <typeparam name="TState">The custom state struct required to track progress.</typeparam>
    public struct Iterator<TType, TState> : IIterator<TType>
    {
        /// <summary>
        /// A delegate that defines the iteration step.
        /// It takes the current state, finds the next element, updates the state, and returns success.
        /// </summary>
        /// <param name="state">A reference to the tracking state.</param>
        /// <param name="result">The found element, or null if finished.</param>
        /// <returns>True if an element was found; false if the iteration is complete.</returns>
        public delegate bool NextDelegate(ref TState state, out TType result);

        private readonly NextDelegate _nextMethod;
        private TState _state;
        private TType _current;

        /// <summary>
        /// Initializes a new instance of the Iterator struct.
        /// Requires an initial state and the logic provider.
        /// </summary>
        /// <param name="initialState">The starting state (e.g., indices set to 0 or -1).</param>
        /// <param name="nextMethod">The logic that moves to the next element.</param>
        public Iterator(TState initialState, NextDelegate nextMethod)
        {
            _state = initialState;
            _nextMethod = nextMethod;
            _current = default;
        }

        /// <summary>
        /// Gets the element at the current position of the iterator.
        /// </summary>
        public TType Current => _current;

        /// <summary>
        /// Advances the iterator to the next element.
        /// Delegates the work to the internal nextMethod using the stored state.
        /// </summary>
        /// <returns>True if the next element was successfully found; false otherwise.</returns>
        public bool MoveNext()
        {
            if (_nextMethod == null) return false;
            return _nextMethod(ref _state, out _current);
        }

        /// <summary>
        /// Explicitly implements the interface method to allow foreach support.
        /// Returns this struct as an IIterator interface, effectively hiding TState.
        /// </summary>
        /// <returns>The current instance as an IIterator.</returns>
        public IIterator<TType> GetEnumerator() => this;
    }
}