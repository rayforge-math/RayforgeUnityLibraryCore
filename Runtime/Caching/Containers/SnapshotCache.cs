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
        private int _count = 0;
        private readonly List<T> _reusableList = new List<T>(16);

        /// <summary>
        /// Gets the current cached snapshot as a ReadOnlySpan.
        /// </summary>
        public ReadOnlySpan<T> Current => _cache.AsSpan(0, _count);

        /// <summary>
        /// Gets the number of active elements in the current cache.
        /// </summary>
        public int Count => _count;

        /// <summary>
        /// Initializes a new empty <see cref="SnapshotCache{T}"/>.
        /// </summary>
        public SnapshotCache() { }

        /// <summary>
        /// Initializes a new <see cref="SnapshotCache{T}"/> with an initial snapshot.
        /// The provided data is copied into the cache — no allocation is shared.
        /// </summary>
        /// <param name="initialData">The initial data to populate the cache with.</param>
        public SnapshotCache(ReadOnlySpan<T> initialData)
        {
            _cache = initialData.Length > 0 ? initialData.ToArray() : Array.Empty<T>();
            _count = initialData.Length;
        }

        /// <summary>
        /// Initializes a new <see cref="SnapshotCache{T}"/> with a pre-allocated cache of the given size.
        /// All elements are set to their default value.
        /// </summary>
        /// <param name="capacity">The number of elements to pre-allocate.</param>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="capacity"/> is negative.</exception>
        public SnapshotCache(int capacity)
        {
            if (capacity < 0)
                throw new ArgumentOutOfRangeException(nameof(capacity), "Capacity must be non-negative.");
            _cache = capacity > 0 ? new T[capacity] : Array.Empty<T>();
            _count = capacity;
        }

        /// <summary>
        /// Compares the provided data against the cache and updates it if differences are found.
        /// Uses SIMD-optimized SequenceEqual for maximum performance.
        /// Never shrinks the internal buffer — only grows on demand.
        /// </summary>
        /// <param name="newData">The new data to compare.</param>
        /// <returns>True if the data differed from the cache and an update occurred; otherwise, false.</returns>
        public bool Apply(ReadOnlySpan<T> newData)
        {
            if (newData.SequenceEqual(Current))
                return false;

            // Grow if needed — never shrink to avoid allocation
            if (newData.Length > _cache.Length)
                Array.Resize(ref _cache, newData.Length);

            newData.CopyTo(_cache);
            _count = newData.Length;
            return true;
        }

        /// <summary>
        /// Overload for custom IIterator support.
        /// Materializes the iterator into a temporary buffer to perform the comparison.
        /// Never shrinks the internal buffer — only grows on demand.
        /// </summary>
        /// <param name="iterator">The iterator providing the new data set.</param>
        /// <returns>True if the iterated data differs from the current cache.</returns>
        public bool Apply<TIterator>(TIterator iterator)
            where TIterator : struct, IIterator<T>
        {
            _reusableList.Clear();
            bool mismatchFound = false;
            int index = 0;

            // struct-Constraint ensures this loop is fully inlined by the JIT.
            foreach (var item in iterator)
            {
                if (!mismatchFound)
                {
                    if (index >= _count || !item.Equals(_cache[index]))
                        mismatchFound = true;
                }
                _reusableList.Add(item);
                index++;
            }

            if (!mismatchFound && index != _count)
                mismatchFound = true;

            if (mismatchFound)
            {
                // Grow if needed — never shrink
                if (_reusableList.Count > _cache.Length)
                    Array.Resize(ref _cache, _reusableList.Count);

                _reusableList.CopyTo(_cache);
                _count = _reusableList.Count;
                return true;
            }

            return false;
        }

        /// <summary>
        /// Forcefully clears the internal cache.
        /// Does not release the internal buffer — only resets the active count.
        /// </summary>
        public void Clear()
        {
            _count = 0;
            if (_reusableList.Count > 0)
                _reusableList.Clear();
        }
    }
}