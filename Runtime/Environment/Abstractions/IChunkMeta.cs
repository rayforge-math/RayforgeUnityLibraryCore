using System;
using UnityEngine;

namespace Rayforge.Core.Environment.Abstractions
{
    /// <summary>
    /// Base interface for any object managed by the grid-based spatial system.
    /// Provides the essential identity needed for indexing and world-space lookup.
    /// </summary>
    public interface IChunkMeta
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

        /// <summary>
        /// The local half-size extent of the chunk.
        /// </summary>
        Vector3 LocalExtent { get; }

        /// <summary>
        /// The full dimensions (Extent * 2) of the chunk along the axes.
        /// </summary>
        Vector3 Size { get; }

        /// <summary>
        /// Signals whether the chunk has been initialized or not.
        /// </summary>
        bool IsInitialized { get; }
    }
}
