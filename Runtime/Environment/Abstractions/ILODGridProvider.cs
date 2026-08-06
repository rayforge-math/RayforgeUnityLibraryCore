using System;

namespace Rayforge.Core.Environment.Abstractions
{
    /// <summary>
    /// Provides Level of Detail (LOD) information and spatial iteration for grid-based systems.
    /// Handles distance-based LOD calculations and provides optimized iteration over cell keys.
    /// </summary>
    /// <typeparam name="TKey">The unique identifier type for grid cells (must be an equatable struct).</typeparam>
    public interface ILODGridProvider<TKey> : 
        ISpatialGridProvider<TKey>,
        ILODGridConfiguration<TKey>,
        ILODGridQuery<TKey>,
        ILODGridMetrics<TKey>
        where TKey : struct, IEquatable<TKey>
    { }
}