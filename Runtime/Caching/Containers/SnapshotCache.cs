using Rayforge.Core.Collections.Abstractions;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Rayforge.Core.Caching.Containers
{
    /// <summary>
    /// Maintains a snapshot of a data collection and detects any changes.
    /// Use this to encapsulate change-tracking logic for arrays, lists, or custom iterators.
    /// </summary>
    public class SnapshotCache<T> where T : struct, IEquatable<T>
    {
        private T[] _cache = Array.Empty<T>();

        /// <summary>
        /// Gets the current cached snapshot as a ReadOnlySpan.
        /// </summary>
        public ReadOnlySpan<T> Current => _cache;

        /// <summary>
        /// Gets the number of elements in the current cache.
        /// </summary>
        public int Count => _cache.Length;

        /// <summary>
        /// Compares the provided data against the cache and updates it if differences are found.
        /// Uses SIMD-optimized SequenceEqual for maximum performance.
        /// </summary>
        /// <param name="newData">The new data to compare.</param>
        /// <returns>True if the data differed from the cache and an update occurred; otherwise, false.</returns>
        public bool Apply(ReadOnlySpan<T> newData)
        {
            if (newData.SequenceEqual(_cache))
            {
                return false;
            }

            _cache = newData.ToArray();
            return true;
        }

        /// <summary>
        /// Overload for custom IIterator support. 
        /// Materializes the iterator into a temporary buffer to perform the comparison.
        /// </summary>
        /// <param name="iterator">The iterator providing the new data set.</param>
        /// <returns>True if the iterated data differs from the current cache.</returns>
        public bool Apply(IIterator<T> iterator)
        {
            if (iterator == null) return HandleEmpty();

            List<T> temp = new List<T>(_cache.Length);
            bool mismatchFound = false;
            int index = 0;

            foreach (var item in iterator)
            {
                if (!mismatchFound)
                {
                    if (index >= _cache.Length || !item.Equals(_cache[index]))
                    {
                        mismatchFound = true;
                    }
                }
                temp.Add(item);
                index++;
            }

            if (!mismatchFound && index != _cache.Length) mismatchFound = true;

            if (mismatchFound)
            {
                _cache = temp.ToArray();
                return true;
            }

            return false;
        }

        /// <summary>
        /// Resets the cache to an empty state.
        /// </summary>
        /// <returns>True if the cache was not already empty.</returns>
        private bool HandleEmpty()
        {
            if (_cache.Length == 0) return false;
            _cache = Array.Empty<T>();
            return true;
        }

        /// <summary>
        /// Forcefully clears the internal cache.
        /// </summary>
        public void Clear()
        {
            _cache = Array.Empty<T>();
        }
    }
}