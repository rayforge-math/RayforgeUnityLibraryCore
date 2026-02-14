using Rayforge.Core.Environment.Spatial.Chunks;
using System.Collections.Generic;
using UnityEngine;

namespace Rayforge.Core.Environment.Abstractions
{
    public interface ISpatialGridProvider
    {
        /// <summary> The size of one grid cell. </summary>
        GridSize GridSize { get; }

        /// <summary> The world-space origin of the grid. </summary>
        Vector3 Anchor { get; }

        /// <summary> 
        /// Returns all grid coordinates (keys) that are touched by the given world bounds. 
        /// </summary>
        IEnumerable<Vector3Int> GetKeysInBounds(Bounds bounds);

        /// <summary> 
        /// Returns all grid coordinates (keys) that are touched by the given local bounds. 
        /// </summary>
        public IEnumerable<Vector3Int> GetKeysInRelativeBounds(Bounds relativeBounds);
    }
}
