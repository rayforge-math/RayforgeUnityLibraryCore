using Rayforge.Core.Collections.Abstractions;
using System;
using System.Collections;

namespace Rayforge.Core.Collections.Iterator
{
    /// <summary>
    /// Synchronized iteration over two BitArrays.
    /// <para>
    /// Yields a meta-struct containing hit status and values of both bit sources 
    /// whenever at least one of the two arrays matches the target state.
    /// </para>
    /// </summary>
    public struct SyncedBitIteratorState : IIterationLogic<SyncedBitMeta, SyncedBitIteratorState>
    {
        private BitIteratorState _stateA;
        private BitIteratorState _stateB;
        private int _currentIndex;
        private readonly int _endIndex;

        /// <summary>
        /// Initializes a new instance of the <see cref="SyncedBitIteratorState"/> struct.
        /// </summary>
        public SyncedBitIteratorState(BitArray bitsA, BitArray bitsB, int startIndex, int count, bool targetState = true)
        {
            _stateA = new BitIteratorState(bitsA, startIndex, count, targetState);
            _stateB = new BitIteratorState(bitsB, startIndex, count, targetState);

            _currentIndex = startIndex - 1;
            _endIndex = startIndex + count;
        }

        /// <summary>
        /// Checks if there are any remaining matches in either of the two bit sources.
        /// </summary>
        public bool HasNext(ref SyncedBitIteratorState self)
        {
            return self._stateA.HasNext(ref self._stateA) || self._stateB.HasNext(ref self._stateB);
        }

        /// <summary>
        /// Advances the iterator to the next available index and populates the meta-data.
        /// </summary>
        public bool MoveNext(ref SyncedBitIteratorState self, out SyncedBitMeta result)
        {
            bool hasA = self._stateA.TryPeekNext(ref self._stateA, out int peekA);
            bool hasB = self._stateB.TryPeekNext(ref self._stateB, out int peekB);

            // Determine the target index based on the available next hits
            int targetIndex;
            if (hasA && hasB) targetIndex = Math.Min(peekA, peekB);
            else if (hasA) targetIndex = peekA;
            else if (hasB) targetIndex = peekB;
            else { result = default; return false; }

            if (targetIndex < self._endIndex)
            {
                self._currentIndex = targetIndex;

                // Advance specific iterators if they match the current targetIndex
                bool hitA = (hasA && peekA == targetIndex) && self._stateA.MoveNext(ref self._stateA, out _);
                bool hitB = (hasB && peekB == targetIndex) && self._stateB.MoveNext(ref self._stateB, out _);

                result = new SyncedBitMeta(targetIndex, hitA, hitB, hitA ? 1 : 0, hitB ? 1 : 0);
                return true;
            }

            result = default;
            return false;
        }

        /// <summary>
        /// Peeks at the next index that matches the target criteria without advancing internal state.
        /// </summary>
        public bool TryPeekNext(ref SyncedBitIteratorState self, out SyncedBitMeta result)
        {
            bool hasA = self._stateA.TryPeekNext(ref self._stateA, out int peekA);
            bool hasB = self._stateB.TryPeekNext(ref self._stateB, out int peekB);

            int targetIndex;
            if (hasA && hasB) targetIndex = Math.Min(peekA, peekB);
            else if (hasA) targetIndex = peekA;
            else if (hasB) targetIndex = peekB;
            else { result = default; return false; }

            result = new SyncedBitMeta(targetIndex,
                                       hasA && peekA == targetIndex,
                                       hasB && peekB == targetIndex,
                                       (hasA && peekA == targetIndex) ? 1 : 0,
                                       (hasB && peekB == targetIndex) ? 1 : 0);
            return true;
        }
    }
}