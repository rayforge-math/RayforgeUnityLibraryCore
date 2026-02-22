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

        /// <summary>
        /// Initializes the traversal state with a defined min and max boundary.
        /// </summary>
        public GridRangeState(Vector3Int min, Vector3Int max)
        {
            _min = min;
            _max = max;
            _current = min;
            _hasStarted = false;
        }

        /// <summary>
        /// Executes the next step in the grid traversal.
        /// Called by the Iterator via 'ref self' to modify the actual state memory.
        /// </summary>
        public bool MoveNext(ref GridRangeState self, out Vector3Int result)
        {
            if (!self._hasStarted)
            {
                self._hasStarted = true;
                result = self._current;
                return self.ValidateInitial();
            }

            self._current.x++;
            if (self._current.x > self._max.x)
            {
                self._current.x = self._min.x;
                self._current.y++;
                if (self._current.y > self._max.y)
                {
                    self._current.y = self._min.y;
                    self._current.z++;
                }
            }

            result = self._current;
            return self._current.z <= self._max.z;
        }

        private bool ValidateInitial() => _current.x <= _max.x && _current.y <= _max.y && _current.z <= _max.z;
    }
}