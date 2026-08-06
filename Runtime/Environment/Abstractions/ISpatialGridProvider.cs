using System;
using UnityEngine;

namespace Rayforge.Core.Environment.Abstractions
{
    /// <summary>
    /// Provides the fundamental spatial indexing logic for a grid-based system, 
    /// combining configuration, coordinate mapping, and spatial queries.
    /// </summary>
    /// <typeparam name="TKey">The unique identifier type for grid cells (must be an equatable struct).</typeparam>
    public interface ISpatialGridProvider<TKey> :
        ISpatialGridConfiguration<TKey>,
        ISpatialCoordinateMapper<TKey>,
        ISpatialGridQuery<TKey>
        where TKey : struct, IEquatable<TKey>
    { }
}