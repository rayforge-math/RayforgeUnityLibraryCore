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
    public class UniversalLodTable<TEntry> : ISerializationCallbackReceiver
        where TEntry : struct, ILodEntry<TEntry>
    {
        #region Fields & Events

        [SerializeField]
        private TEntry[] _entries = new TEntry[0];

        private TEntry[] _validEntries = Array.Empty<TEntry>();
        private float[] _validDistances = Array.Empty<float>();

        /// <summary>
        /// Notifies listeners that the valid LOD chain has been updated.
        /// Passes the table instance as the sender.
        /// </summary>
        public event Action<UniversalLodTable<TEntry>> OnTableChanged;

        #endregion

        #region Properties & Indexer

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
        /// Indexer for quick access to validated entries.
        /// </summary>
        /// <param name="index">The index of the valid LOD level.</param>
        /// <returns>The validated LOD entry at the specified index.</returns>
        public TEntry this[int index] => _validEntries[index];

        #endregion

        #region Sanitization Logic

        /// <summary>
        /// Validates the LOD chain. 
        /// Ensures distances strictly grow and logical quality (defined by TEntry) is consistent.
        /// Should be called during OnValidate or whenever the source data changes.
        /// </summary>
        /// <param name="minFirstDistance">Minimum distance for the first LOD (LOD0).</param>
        /// <param name="minStep">Minimum distance increase between consecutive levels.</param>
        public void Sanitize(float minFirstDistance = 50f, float minStep = 10f)
        {
            if (_entries == null || _entries.Length == 0)
            {
                _entries = new TEntry[1];
                TEntry defaultEntry = default;
                defaultEntry.DistanceThreshold = minFirstDistance;
                _entries[0] = defaultEntry;
            }

            TEntry prev = default;

            for (int i = 0; i < _entries.Length; i++)
            {
                TEntry current = _entries[i];

                if (i > 0 && current.DistanceThreshold <= prev.DistanceThreshold + minStep)
                {
                    current.DistanceThreshold = prev.DistanceThreshold + minStep;
                }
                else if (i == 0 && current.DistanceThreshold < minFirstDistance)
                { 
                    current.DistanceThreshold = minFirstDistance;
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
                bool wasNotEmpty = _validEntries.Length > 0;
                _validEntries = Array.Empty<TEntry>();
                _validDistances = Array.Empty<float>();

                if (wasNotEmpty) OnTableChanged?.Invoke(this);
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

            if (!AreDistancesEqual(distanceList))
            {
                _validEntries = validList.ToArray();
                _validDistances = distanceList.ToArray();

                OnTableChanged?.Invoke(this);
            }
        }

        /// <summary>
        /// Compares a list of distances against the currently cached valid distances.
        /// </summary>
        /// <param name="newList">The new list of distances to check.</param>
        /// <returns>True if the distances are identical within floating point precision.</returns>
        private bool AreDistancesEqual(List<float> newList)
        {
            if (_validDistances.Length != newList.Count) return false;

            for (int i = 0; i < _validDistances.Length; i++)
            {
                if (!Mathf.Approximately(_validDistances[i], newList[i])) return false;
            }
            return true;
        }

        #endregion

        #region Management

        /// <summary>
        /// Completely clears the table and resets the internal entries and caches.
        /// Triggers <see cref="OnTableChanged"/> if the table was not already empty.
        /// </summary>
        public void Clear()
        {
            if (_entries.Length == 0) return;

            _entries = new TEntry[0];
            RebuildCache();
        }

        #endregion

        #region ISerializationCallbackReceiver

        /// <summary> Internal Unity callback. Not used. </summary>
        void ISerializationCallbackReceiver.OnBeforeSerialize() { }

        /// <summary>
        /// Internal Unity callback. Rebuilds the transient caches after deserialization 
        /// to ensure the table is ready for use after loading a scene or asset.
        /// </summary>
        void ISerializationCallbackReceiver.OnAfterDeserialize()
        {
            RebuildCache();
        }

        #endregion
    }
}