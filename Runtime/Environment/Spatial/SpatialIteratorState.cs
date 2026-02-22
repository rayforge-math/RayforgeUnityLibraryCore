using Rayforge.Core.Collections.Abstractions;
using UnityEngine;
using System;
using System.Collections.Generic;

namespace Rayforge.Core.Environment.Spatial
{
    /// <summary>
    /// A specialized, zero-allocation state container that resolves internal IDs from a spatial bucket 
    /// into actual component instances by accessing the storage dictionary directly.
    /// </summary>
    /// <typeparam name="TKey">The spatial identifier type (e.g., Vector3Int).</typeparam>
    /// <typeparam name="TType">The component type being iterated.</typeparam>
    public struct SpatialIteratorState<TKey, TType> : IIterationLogic<TType, SpatialIteratorState<TKey, TType>>
        where TKey : struct, IEquatable<TKey>
        where TType : Component
    {
        /// <summary>
        /// The internal enumerator for the ID collection (bucket).
        /// Public field to allow the Iterator to modify the struct's internal state via ref.
        /// </summary>
        public HashSet<int>.Enumerator _bucket;

        /// <summary>
        /// Direct reference to the registry's storage dictionary.
        /// Stored directly to eliminate one layer of indirection (Registry -> Dictionary).
        /// </summary>
        private readonly Dictionary<int, SpatialState<TType>> _storage;

        /// <summary>
        /// Initializes a new instance of the <see cref="SpatialIteratorState{TKey, TType}"/> struct.
        /// </summary>
        /// <param name="bucket">The struct-based enumerator for the specific spatial cell.</param>
        /// <param name="storage">The internal dictionary used for O(1) ID-to-Object resolution.</param>
        public SpatialIteratorState(HashSet<int>.Enumerator bucket, Dictionary<int, SpatialState<TType>> storage)
        {
            _bucket = bucket;
            _storage = storage;
        }

        /// <summary>
        /// Advances the bucket enumerator and resolves the found ID to its component instance.
        /// </summary>
        /// <param name="self">Reference to the current state to allow in-place modification.</param>
        /// <param name="result">The resolved component instance if found; otherwise default.</param>
        /// <returns>True if a valid component was found and resolved; false otherwise.</returns>
        public bool MoveNext(ref SpatialIteratorState<TKey, TType> self, out TType result)
        {
            while (self._bucket.MoveNext())
            {
                if (self._storage.TryGetValue(self._bucket.Current, out var state))
                {
                    result = state.component;
                    return true;
                }
            }

            result = default;
            return false;
        }
    }
}