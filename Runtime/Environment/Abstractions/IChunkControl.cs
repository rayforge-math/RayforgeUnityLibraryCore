using System;
using UnityEngine;

/// <summary>
/// Defines the lifecycle and administrative control methods for a chunk.
/// This interface should be used exclusively by factory or registry systems 
/// responsible for managing the chunk's state.
/// </summary>
public interface IChunkControl : IDisposable
{
    /// <summary>
    /// Initializes the chunk with its spatial metadata.
    /// Must be called exactly once before the chunk is added to any registry.
    /// </summary>
    /// <param name="gridKey">The unique coordinate key identifying this chunk in the grid.</param>
    /// <param name="extent">The half-size of the chunk in world units (must be non-negative).</param>
    /// <exception cref="InvalidOperationException">Thrown if the chunk has already been initialized.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown if any extent component is negative.</exception>
    void Initialize(Vector3Int gridKey, Vector3 extent);

    /// <summary>
    /// Cleans up resources and prepares the chunk for removal or pooling.
    /// </summary>
    new void Dispose();
}