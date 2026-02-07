using System;
using UnityEngine;

namespace Rayforge.Core.Environment.Abstractions
{
    /// <summary>
    /// Base interface for any object managed by the grid-based spatial system.
    /// Provides the essential identity needed for indexing and world-space lookup.
    /// </summary>
    public interface IChunk
    {
        /// <summary>
        /// The coordinate in the grid (e.g., 5, 0, -2). 
        /// Acts as the unique spatial identifier.
        /// </summary>
        Vector3Int GridKey { get; }

        /// <summary>
        /// The actual center position in world space.
        /// Useful for distance-based calculations without accessing the transform.
        /// </summary>
        Vector3 WorldPosition { get; }
    }
}
