using Rayforge.Core.Environment.Spatial;
using Rayforge.Core.Environment.Spatial.Chunks;
using System;
using UnityEngine;

namespace Rayforge.Core.Environment.Abstractions
{
    /// <summary>
    /// Provides configuration, structural state, and event notifications for a spatial grid system.
    /// </summary>
    /// <typeparam name="TKey">The unique identifier type for grid cells.</typeparam>
    public interface ISpatialGridConfiguration<TKey>
        where TKey : struct, IEquatable<TKey>
    {
        /// <summary> Gets the size of a single grid cell (width, height, depth). </summary>
        GridSize GridSize { get; }

        /// <summary> Gets the world-space origin (0,0,0) of the grid system. </summary>
        Vector3 Anchor { get; }

        /// <summary> Gets the active World Space axes. </summary>
        SpatialAxes ActiveAxes { get; }

        /// <summary> Checks whether the underlying spatial system is ready for queries. </summary>
        bool IsInitialized { get; }

        /// <summary> Gets the total number of cells currently tracked or present in the spatial index. </summary>
        int TotalCellCount { get; }

        /// <summary> Occurs when the grid's scale or fundamental structure changes. </summary>
        event Action<ISpatialGridConfiguration<TKey>> OnGridStructureChanged;

        /// <summary> Occurs when the grid origin (Anchor) shifts. </summary>
        event Action<ISpatialGridConfiguration<TKey>, Vector3> OnAnchorChanged;
    }
}