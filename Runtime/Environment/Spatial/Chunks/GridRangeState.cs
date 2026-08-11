using Rayforge.Core.Collections.Abstractions;
using UnityEngine;

namespace Rayforge.Core.Environment.Spatial.Chunks
{
    /// <summary>
    /// A struct-based state that contains the data for a 3D grid traversal.
    /// Implements IIterationLogic to allow self-contained, zero-allocation iteration.
    /// </summary>
    public struct GridRangeState : IIterationLogic<Vector3Int, GridRangeState>
    {
        private readonly Vector3Int _min;
        private readonly Vector3Int _max;
        private Vector3Int _current;
        private bool _hasStarted;
        private bool _isExhausted;

        /// <summary>
        /// Initializes the traversal state with a defined min and max boundary.
        /// Automatically ensures that min is less than or equal to max for all axes.
        /// </summary>
        /// <param name="min">First boundary corner.</param>
        /// <param name="max">Second boundary corner (can be smaller than v1).</param>
        public GridRangeState(Vector3Int min, Vector3Int max)
        {
            _min = min;
            _max = max;

            _current = _min;
            _hasStarted = false;

            _isExhausted = (_min.x > _max.x || _min.y > _max.y || _min.z > _max.z);
        }

        /// <summary>
        /// Predicts if a successor to the current coordinate exists.
        /// </summary>
        public bool HasNext(ref GridRangeState self)
        {
            if (self._isExhausted) return false;
            if (!self._hasStarted) return true;

            Vector3Int next = CalculateNext(self._current, self._min, self._max, out bool exhausted);
            return !exhausted;
        }

        /// <summary>
        /// Returns the next coordinate in the sequence without advancing the internal state.
        /// </summary>
        public bool TryPeekNext(ref GridRangeState self, out Vector3Int result)
        {
            if (self._isExhausted)
            {
                result = default;
                return false;
            }

            if (!self._hasStarted)
            {
                result = self._min;
                return true;
            }

            Vector3Int next = CalculateNext(self._current, self._min, self._max, out bool exhausted);

            if (exhausted)
            {
                result = default;
                return false;
            }

            result = next;
            return true;
        }

        /// <summary>
        /// Advances the state and returns the next coordinate.
        /// </summary>
        public bool MoveNext(ref GridRangeState self, out Vector3Int result)
        {
            if (self._isExhausted)
            {
                result = default;
                return false;
            }

            if (!self._hasStarted)
            {
                self._hasStarted = true;
                result = self._min;
                return true;
            }

            self._current = CalculateNext(self._current, self._min, self._max, out self._isExhausted);

            if (!self._isExhausted)
            {
                result = self._current;
                return true;
            }

            result = default;
            return false;
        }

        /// <summary>
        /// Pure functional logic to determine the next coordinate in Z-Y-X order.
        /// </summary>
        private static Vector3Int CalculateNext(Vector3Int current, Vector3Int min, Vector3Int max, out bool exhausted)
        {
            exhausted = false;
            Vector3Int next = current;

            // X-Dimension
            if (min.x <= max.x)
            {
                next.x++;
                if (next.x > max.x)
                {
                    next.x = min.x;

                    // Y-Dimension
                    if (min.y <= max.y)
                    {
                        next.y++;
                        if (next.y > max.y)
                        {
                            next.y = min.y;

                            // Z-Dimension
                            if (min.z <= max.z)
                            {
                                next.z++;
                                if (next.z > max.z)
                                {
                                    exhausted = true;
                                }
                            }
                            else
                            {
                                exhausted = true;
                            }
                        }
                    }
                    else
                    {
                        exhausted = true;
                    }
                }
            }
            else
            {
                exhausted = true;
            }

            return next;
        }
    }
}