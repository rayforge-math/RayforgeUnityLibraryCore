using Rayforge.Core.Execution.Abstractions;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Rayforge.Core.Environment.Spatial.Chunks
{
    /// <summary>
    /// A static wrapper for the specialized <see cref="ChunkRegistry{T}"/>.
    /// Provides global access to grid-based chunk management, spatial queries, and GPU synchronization state.
    /// </summary>
    /// <typeparam name="T">The type of WorldChunk3D managed by this registry.</typeparam>
    public static class GlobalChunkRegistry<T>
        where T : Chunk<T>
    {
        private const GridSize k_DefaultGridSize = (GridSize)GridSizeBinary.Size128;

        /// <summary> 
        /// The underlying singleton instance of the registry. 
        /// Initialized with default settings.
        /// </summary>
        public static readonly ChunkRegistry<T> Instance;

        /// <summary>
        /// Static constructor: Initializes the shared instance with world-defaults.
        /// This runs exactly once per type T when the class is first accessed.
        /// </summary>
        static GlobalChunkRegistry()
        {
            Instance = new ChunkRegistry<T>();
            Instance.Initialize(k_DefaultGridSize, Vector3.zero);
        }

        #region Grid Properties
        /// <summary> Read-only access to the physical size of one side of a chunk cell. </summary>
        public static GridSize GridSize => Instance.GridSize;

        /// <summary> Current world-space origin offset used for grid calculations. </summary>
        public static Vector3 Anchor => Instance.Anchor;
        #endregion

        #region Lifecycle & Factory

        /// <summary>
        /// Retrieves an existing chunk or creates a new one at the specified grid coordinate.
        /// Uses a struct-based handler for zero-allocation configuration.
        /// </summary>
        /// <typeparam name="THandler">The struct handler type used for configuration.</typeparam>
        /// <param name="key">The 3D grid coordinate (e.g., world-space index).</param>
        /// <param name="onConfigure">A struct handler executed to set up the chunk if it's newly created or needs refresh.</param>
        /// <param name="chunk">When this method returns, contains the chunk associated with the key.</param>
        /// <returns>True if a brand new chunk was created; false if an existing one was retrieved.</returns>
        public static bool GetOrCreateChunk<THandler>(Vector3Int key, ref THandler onConfigure, out T chunk)
            where THandler : struct, IExecutionHandler<T>
            => Instance.GetOrCreateChunk(key, ref onConfigure, out chunk);

        /// <summary>
        /// Ensures a chunk exists at the given world position, creating it if necessary.
        /// Uses a struct-based handler for zero-allocation configuration.
        /// </summary>
        /// <typeparam name="THandler">The struct handler type used for configuration.</typeparam>
        /// <param name="pos">The world-space position to check for a chunk.</param>
        /// <param name="onConfigure">A struct handler executed to set up the chunk if it's newly created or needs refresh.</param>
        /// <param name="chunk">When this method returns, contains the chunk corresponding to the world position.</param>
        /// <returns>True if a brand new chunk was created; false if an existing one was retrieved.</returns>
        public static bool GetOrCreateChunkAtWorldPos<THandler>(Vector3 pos, ref THandler onConfigure, out T chunk)
            where THandler : struct, IExecutionHandler<T>
            => Instance.GetOrCreateChunkAtWorldPos(pos, ref onConfigure, out chunk);

        /// <summary>
        /// Safely removes and destroys a chunk from the grid based on its coordinate.
        /// </summary>
        /// <param name="key">The grid coordinate of the chunk to remove.</param>
        public static void DestroyChunk(Vector3Int key) => Instance.RemoveAndDestroy(key);

        #endregion

        #region Spatial Queries
        /// <summary> 
        /// Attempts to retrieve a chunk at the specific grid coordinate.
        /// Returns true and the chunk if found, otherwise false and null.
        /// </summary>
        /// <param name="key">The integer grid coordinates.</param>
        /// <param name="chunk">The found chunk or null.</param>
        public static bool TryGetChunk(Vector3Int key, out T chunk)
        {
            if (Instance == null)
            {
                chunk = null;
                return false;
            }

            return Instance.TryGetEntry(key, out chunk);
        }

        /// <summary> 
        /// Helper to find a chunk based on its absolute world position without creating it.
        /// Returns true and the chunk if found, otherwise false and null.
        /// </summary>
        /// <param name="pos">The world space position to query.</param>
        /// <param name="chunk">The found chunk or null.</param>
        public static bool TryGetChunkAtWorldPos(Vector3 pos, out T chunk)
        {
            if (Instance == null)
            {
                chunk = null;
                return false;
            }

            return Instance.TryGetChunkAtWorldPos(pos, out chunk);
        }

        /// <summary>
        /// Maps a world position to the corresponding grid key using the current Anchor.
        /// </summary>
        public static Vector3Int WorldToGrid(Vector3 pos) => Instance.WorldToGrid(pos);
        #endregion

        #region Global State & Sync
        /// <summary> 
        /// Provides a read-only collection of all currently active chunks. 
        /// </summary>
        public static IEnumerable<T> AllChunks => Instance.AllEntries;

        /// <summary> 
        /// Returns true if the registry structure has changed or any individual chunk is marked as dirty. 
        /// </summary>
        public static bool NeedsGPUUpdate() => Instance.NeedsUpdate();

        /// <summary> 
        /// Clears all dirty flags for the registry and all contained chunks after a sync.
        /// </summary>
        public static void ResetDirtyFlags() => Instance.ResetDirtyFlags();

        /// <summary>
        /// Clears all registered chunks and destroys their GameObjects. 
        /// </summary>
        public static void Clear() => Instance.Clear();

        /// <summary>
        /// Adjusts the global anchor to handle floating origin shifts and suppresses transform alerts.
        /// </summary>
        /// <param name="delta">The shift offset.</param>
        public static void ApplyOriginShift(Vector3 delta) => Instance.NotifyOriginShift(delta);
        #endregion
    }
}
