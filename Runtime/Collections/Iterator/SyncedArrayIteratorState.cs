using Rayforge.Core.Collections.Abstractions;
using System;

namespace Rayforge.Core.Collections.Iterator
{
    /// <summary>
    /// Synchronized iteration over two parallel raw arrays.
    /// <para>
    /// Provides high-performance, zero-allocation iteration across a specific
    /// range of two synchronized data streams. Designed for Unity/IL2CPP compatibility
    /// by avoiding ref-struct constraints.
    /// </para>
    /// </summary>
    /// <typeparam name="TValueA">The type of the first data stream.</typeparam>
    /// <typeparam name="TValueB">The type of the second data stream.</typeparam>
    public struct SyncedArrayIteratorState<TValueA, TValueB> 
        : IIterationLogic<SyncedArrayMeta<TValueA, TValueB>, SyncedArrayIteratorState<TValueA, TValueB>>
        where TValueA : unmanaged
        where TValueB : unmanaged
    {
        private ArrayIteratorState<TValueA> _stateA;
        private ArrayIteratorState<TValueB> _stateB;

        private readonly int _startOffset;
        private int _currentIndex;

        /// <summary>
        /// Initializes a new instance of the <see cref="SyncedArrayIteratorState{TValueA, TValueB}"/> struct.
        /// </summary>
        /// <param name="arrayA">The primary array.</param>
        /// <param name="arrayB">The secondary array, parallel to arrayA.</param>
        /// <param name="start">The starting index.</param>
        /// <param name="count">The number of elements to process.</param>
        public SyncedArrayIteratorState(TValueA[] arrayA, TValueB[] arrayB, int start, int count)
        {
            // Die ArrayIteratorState-Konstruktoren führen nun die Validierung durch
            _stateA = new ArrayIteratorState<TValueA>(arrayA, start, count);
            _stateB = new ArrayIteratorState<TValueB>(arrayB, start, count);

            _startOffset = start;
            _currentIndex = start - 1;
        }

        /// <summary>
        /// Checks if more elements are available in the synchronized streams.
        /// </summary>
        public bool HasNext(ref SyncedArrayIteratorState<TValueA, TValueB> self)
        {
            return self._stateA.HasNext(ref self._stateA);
        }

        /// <summary>
        /// Peeks at the next synchronized segment without advancing the state.
        /// </summary>
        public bool TryPeekNext(ref SyncedArrayIteratorState<TValueA, TValueB> self, out SyncedArrayMeta<TValueA, TValueB> result)
        {
            if (self._stateA.TryPeekNext(ref self._stateA, out var valA) &&
                self._stateB.TryPeekNext(ref self._stateB, out var valB))
            {
                int nextIndex = self._currentIndex + 1;
                result = new SyncedArrayMeta<TValueA, TValueB>(
                    nextIndex,
                    nextIndex - self._startOffset,
                    valA,
                    valB
                );
                return true;
            }

            result = default;
            return false;
        }

        /// <summary>
        /// Advances the iterator and yields the synchronized data pair.
        /// </summary>
        public bool MoveNext(ref SyncedArrayIteratorState<TValueA, TValueB> self, out SyncedArrayMeta<TValueA, TValueB> result)
        {
            if (self._stateA.MoveNext(ref self._stateA, out var valA) &&
                self._stateB.MoveNext(ref self._stateB, out var valB))
            {
                self._currentIndex++;
                result = new SyncedArrayMeta<TValueA, TValueB>(
                    self._currentIndex,
                    self._currentIndex - self._startOffset,
                    valA,
                    valB
                );
                return true;
            }

            result = default;
            return false;
        }
    }
}