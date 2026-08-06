using System;
using UnityEngine;

namespace Rayforge.Core.Environment.Abstractions
{
    public interface ILODGridConfiguration<TKey> : ISpatialGridConfiguration<TKey>
        where TKey : struct, IEquatable<TKey>
    {
        /// <summary> Gets the current world-space position of the player or camera focus. </summary>
        Vector3 ViewerPos { get; }

        /// <summary> Gets the total number of defined LOD levels. </summary>
        int LodCount { get; }

        /// <summary> Gets the number of cells currently active (within visible range). </summary>
        int ActiveCellCount { get; }

        /// <summary> Gets the squared distance thresholds for each LOD level. </summary>
        ReadOnlySpan<float> LodSqrDistances { get; }

        /// <summary> Gets the linear distance thresholds for each LOD level. </summary>
        ReadOnlySpan<float> LodDistances { get; }

        /// <summary> 
        /// Occurs when distance thresholds change while the LOD count remains constant.
        /// Trigger re-evaluation of existing cell LODs without rebuilding the grid.
        /// </summary>
        event Action<ILODGridConfiguration<TKey>> OnLODSettingsChanged;
    }
}
