using System.Collections.Generic;
using UnityEngine;

namespace Rayforge.Core.Environment.Spatial
{
    /// <summary>
    /// A static wrapper for the <see cref="ChunkRegistry{T}"/>.
    /// Provides global access to chunk management, spatial queries, and GPU synchronization state.
    /// </summary>
    /// <typeparam name="T">The type of WorldChunk3D managed by this registry.</typeparam>
    public static class GlobalChunkRegistry<T>
        where T : Chunk3D<T>
    {
        private const float k_GridSize = 100.0f;

        /// <summary> The underlying singleton instance of the registry. </summary>
        public static readonly ChunkRegistry<T> Instance = new ChunkRegistry<T>(k_GridSize, Vector3.zero);

        /// <summary> Read-only access to the physical size of one side of a chunk cell. </summary>
        public static float GridSize => Instance.GridSize;

        /// <summary> Current world-space origin offset. </summary>
        public static Vector3 Anchor => Instance.Anchor;

        /// <summary> 
        /// Maps a world position to a grid key and registers the chunk. 
        /// Assigns a unique GPU ID if necessary.
        /// </summary>
        /// <param name="chunk">The chunk instance to add to the registry.</param>
        public static void Register(T chunk) => Instance.Register(chunk);

        /// <summary> 
        /// Removes the chunk from the grid and returns its GPU ID to the pool. 
        /// </summary>
        /// <param name="chunk">The chunk instance to remove.</param>
        public static void Unregister(T chunk) => Instance.Unregister(chunk);

        /// <summary> 
        /// Retrieves a chunk at the specific grid coordinate. Returns null if no chunk is registered there.
        /// </summary>
        /// <param name="key">The integer grid coordinates.</param>
        public static T GetChunk(Vector3Int key) => Instance.GetChunk(key);

        /// <summary> 
        /// Helper to find a chunk based on its absolute world position.
        /// </summary>
        /// <param name="pos">The world space position to query.</param>
        public static T GetChunkAtWorldPos(Vector3 pos) => Instance.GetChunkAtWorldPos(pos);

        /// <summary> 
        /// Provides a read-only collection of all currently active chunks. 
        /// Useful for the Fog Feature to iterate over for baking.
        /// </summary>
        public static IEnumerable<T> AllChunks => Instance.AllChunks;

        /// <summary> 
        /// Returns true if the registry structure has changed or any individual chunk is marked as dirty. 
        /// </summary>
        public static bool NeedsGPUUpdate() => Instance.NeedsGPUUpdate();

        /// <summary> 
        /// Clears all dirty flags for the registry and all contained chunks. 
        /// Should be called after the GPU buffer or Heightmap update is complete.
        /// </summary>
        public static void ResetDirtyFlags() => Instance.ResetDirtyFlags();

        /// <summary>
        /// Clears all registered chunks and resets the ID pool. 
        /// Essential for scene transitions to prevent memory leaks and stale data.
        /// </summary>
        public static void Clear()
        {
            Instance.Clear();
        }
    }
}
