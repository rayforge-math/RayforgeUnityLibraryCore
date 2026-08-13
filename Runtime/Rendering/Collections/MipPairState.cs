using Rayforge.Core.Collections.Abstractions;
using System;
using UnityEngine;

namespace Rayforge.Core.Rendering.Collections
{
    /// <summary>
    /// State struct for iterating over consecutive mip pair transitions of a mip chain.
    /// Implements IIterationLogic to allow self-contained, zero-allocation iteration.
    /// </summary>
    public struct MipPairState<THandle> : IIterationLogic<MipPair<THandle>, MipPairState<THandle>>
    {
        private readonly THandle[] _handles;
        private int _destinationMip;

        /// <summary>
        /// Initializes the mip pair iteration state.
        /// </summary>
        /// <param name="handles">The array of handles to iterate over.</param>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="handles"/> is null.</exception>
        public MipPairState(THandle[] handles)
        {
            if (handles == null)
                throw new ArgumentNullException(nameof(handles), "Handles array cannot be null.");

            _handles = handles;
            _destinationMip = 0;
        }

        /// <summary>
        /// Predicts if a successor mip pair transition exists.
        /// </summary>
        public bool HasNext(ref MipPairState<THandle> self)
        {
            if (self._handles == null || self._handles.Length < 2) return false;
            int nextMip = (self._destinationMip == 0) ? 1 : self._destinationMip + 1;
            return nextMip < self._handles.Length;
        }

        /// <summary>
        /// Returns the next mip pair transition without advancing the internal state.
        /// </summary>
        public bool TryPeekNext(ref MipPairState<THandle> self, out MipPair<THandle> result)
        {
            if (self._handles != null && self._handles.Length >= 2)
            {
                int nextMip = (self._destinationMip == 0) ? 1 : self._destinationMip + 1;
                if (nextMip < self._handles.Length)
                {
                    result = new MipPair<THandle>(self._handles[nextMip - 1], self._handles[nextMip], nextMip);
                    return true;
                }
            }
            result = default;
            return false;
        }

        /// <summary>
        /// Advances the state and returns the next mip pair transition.
        /// </summary>
        public bool MoveNext(ref MipPairState<THandle> self, out MipPair<THandle> result)
        {
            if (self._handles == null || self._handles.Length < 2)
            {
                result = default;
                return false;
            }

            if (self._destinationMip == 0)
            {
                self._destinationMip = 1;
            }
            else
            {
                self._destinationMip++;
            }

            if (self._destinationMip < self._handles.Length)
            {
                result = new MipPair<THandle>(self._handles[self._destinationMip - 1], self._handles[self._destinationMip], self._destinationMip);
                return true;
            }

            result = default;
            return false;
        }
    }
}
