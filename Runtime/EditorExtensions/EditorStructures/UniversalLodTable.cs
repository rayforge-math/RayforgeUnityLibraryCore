using Rayforge.Core.EditorExtensions.Abstractions;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Rayforge.Core.EditorExtensions.EditorStructures
{
    /// <summary>
    /// A generic, serializable table for managing Level of Detail (LOD) chains.
    /// Handles distance-based progression and delegates quality validation to the 
    /// specific <typeparamref name="TEntry"/> implementation.
    /// </summary>
    /// <typeparam name="TEntry">A struct implementing <see cref="ILodEntry{TSelf}"/>.</typeparam>
    [Serializable]
    public class UniversalLodTable<TEntry> where TEntry : struct, ILodEntry<TEntry>
    {
        [SerializeField]
        private TEntry[] _entries = new TEntry[0];

        private TEntry[] _validEntries = Array.Empty<TEntry>();
        private float[] _validDistances = Array.Empty<float>();

        /// <summary>
        /// Provides high-performance, read-only access to the sanitized LOD entries.
        /// Using ReadOnlySpan prevents external modification of the cached data.
        /// </summary>
        public ReadOnlySpan<TEntry> ValidEntries => _validEntries;

        /// <summary>
        /// Provides high-performance, read-only access to the distance thresholds.
        /// Does not allocate managed memory when accessed.
        /// </summary>
        public ReadOnlySpan<float> ValidDistances => _validDistances;

        /// <summary>
        /// Returns the number of validated entries in the cache.
        /// </summary>
        public int Count => _validEntries.Length;

        /// <summary>
        /// Validates the LOD chain. 
        /// Ensures distances strictly grow and logical quality (defined by TEntry) is consistent.
        /// Should be called during OnValidate or whenever the source data changes.
        /// </summary>
        /// <param name="minFirstDistance">Minimum distance for the first LOD (LOD0).</param>
        /// <param name="minStep">Minimum distance increase between consecutive levels.</param>
        public void Sanitize(float minFirstDistance = 50f, float minStep = 10f)
        {
            TEntry prev = default;

            for (int i = 0; i < _entries.Length; i++)
            {
                TEntry current = _entries[i];

                if (i > 0 && current.DistanceThreshold <= prev.DistanceThreshold + minStep)
                {
                    current.DistanceThreshold = prev.DistanceThreshold + minStep;
                }
                else
                {
                    if (current.DistanceThreshold < minFirstDistance) current.DistanceThreshold = minFirstDistance;
                }

                if (!current.IsLogicalSuccessor(prev))
                {
                    current.MakeValidSuccessor(prev);
                }

                _entries[i] = current;
                prev = current;
            }

            RebuildCache();
        }

        /// <summary>
        /// Rebuilds the internal cache of logically sound entries and distance thresholds.
        /// Filters out any entries that might have broken constraints despite sanitization.
        /// </summary>
        private void RebuildCache()
        {
            if (_entries.Length == 0)
            {
                _validEntries = Array.Empty<TEntry>();
                _validDistances = Array.Empty<float>();
                return;
            }

            List<TEntry> validList = new List<TEntry>(capacity: _entries.Length);
            List<float> distanceList = new List<float>(capacity: _entries.Length);

            TEntry lastAccepted = _entries[0];
            validList.Add(lastAccepted);
            distanceList.Add(lastAccepted.DistanceThreshold);

            for (int i = 1; i < _entries.Length; i++)
            {
                TEntry cur = _entries[i];

                if (cur.DistanceThreshold > lastAccepted.DistanceThreshold &&
                    cur.IsLogicalSuccessor(lastAccepted))
                {
                    validList.Add(cur);
                    distanceList.Add(cur.DistanceThreshold);
                    lastAccepted = cur;
                }
            }

            _validEntries = validList.ToArray();
            _validDistances = distanceList.ToArray();
        }

        /// <summary>
        /// Completely clears the table and resets the internal entries and caches.
        /// </summary>
        public void Clear()
        {
            _entries = new TEntry[0];
            _validEntries = Array.Empty<TEntry>();
            _validDistances = Array.Empty<float>();
        }
    }
}