using System.Collections.Generic;
using UnityEngine;

namespace Rayforge.Core.Environment.Abstractions
{
    public interface ISpatialGridProvider
    {
        /// <summary> English: The size of one grid cell. </summary>
        int GridSize { get; }

        /// <summary> English: The world-space origin of the grid. </summary>
        Vector3 Anchor { get; }

        /// <summary> 
        /// English: Returns all grid coordinates (keys) that are touched by the given bounds. 
        /// </summary>
        IEnumerable<Vector3Int> GetKeysInBounds(Bounds bounds);
    }
}
