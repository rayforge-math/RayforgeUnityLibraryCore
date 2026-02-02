using Rayforge.Core.Environment.Spatial.Helpers;
using System.Collections.Generic;
using UnityEngine;

namespace Rayforge.Core.Environment.Spatial
{
    /// <summary>
    /// A high-performance registry for managing WorldChunk3D instances.
    /// Handles spatial indexing, GPU ID pooling, and change tracking.
    /// Generically handles Floating Origin via the Anchor property.
    /// </summary>
    /// <typeparam name="T">The type of chunk managed by this registry.</typeparam>
    public class ChunkRegistry<T>
        where T : WorldChunk3D<T>
    {
        #region Data Structures
        /// <summary> Main storage for chunks indexed by their 3D grid coordinates. </summary>
        private readonly Dictionary<Vector3Int, T> _grid = new Dictionary<Vector3Int, T>();

        /// <summary> A stack of recycled IDs to keep the GPU buffer indices compact. </summary>
        private readonly Stack<int> _idPool = new Stack<int>();

        /// <summary> Flag indicating if the collection itself (count/composition) has changed. </summary>
        private bool _globalDirty = false;

        /// <summary> The next available ID if the pool is empty. </summary>
        private int _nextId = 0;

        /// <summary> The physical size of one side of a chunk cell. </summary>
        public float GridSize { get; set; } = 100f;

        /// <summary> 
        /// The world-space origin offset for the grid calculation. 
        /// In Floating Origin systems, shift this Anchor whenever the origin changes.
        /// </summary>
        public Vector3 Anchor { get; set; } = Vector3.zero;

        /// <summary> Provides access to all currently registered chunks. </summary>
        public Dictionary<Vector3Int, T>.ValueCollection AllChunks => _grid.Values;
        #endregion

        #region Registration & ID Pooling
        /// <summary>
        /// Registers a chunk, assigns a GPU identifier, and maps it to the grid.
        /// </summary>
        /// <param name="chunk">The chunk instance to register.</param>
        public void Register(T chunk)
        {
            // Calculate key and store it in the chunk for stable unregistration
            Vector3Int key = WorldToGrid(chunk.transform.position);
            chunk.currentGridKey = key;

            _grid[key] = chunk;

            // Assign a GPU ID: Reuse from pool or create a new one
            if (chunk.gpuIdentifier == -1)
            {
                chunk.gpuIdentifier = _idPool.Count > 0 ? _idPool.Pop() : _nextId++;
            }

            _globalDirty = true;
        }

        /// <summary>
        /// Removes a chunk from the grid and returns its GPU ID to the pool for reuse.
        /// Uses the stored key to remain immune to floating origin shifts.
        /// </summary>
        /// <param name="chunk">The chunk instance to unregister.</param>
        public void Unregister(T chunk)
        {
            // Use the stored key instead of recalculating from current position.
            // This ensures we find the chunk even if the world has shifted.
            Vector3Int key = chunk.currentGridKey;

            if (_grid.TryGetValue(key, out T current) && current == chunk)
            {
                _grid.Remove(key);

                if (chunk.gpuIdentifier != -1)
                {
                    _idPool.Push(chunk.gpuIdentifier);
                    chunk.gpuIdentifier = -1;
                }

                _globalDirty = true;
            }
        }
        #endregion

        #region Spatial Queries
        /// <summary>
        /// Retrieves a chunk at a specific grid coordinate.
        /// </summary>
        public T GetChunk(Vector3Int key)
        {
            _grid.TryGetValue(key, out T chunk);
            return chunk;
        }

        /// <summary>
        /// Retrieves a chunk based on its absolute world position.
        /// </summary>
        public T GetChunkAtWorldPos(Vector3 pos) => GetChunk(WorldToGrid(pos));

        /// <summary> 
        /// Returns all chunks within a specified cell radius around a world position.
        /// </summary>
        public List<T> GetChunksInRadius(Vector3 center, int cellRadius)
        {
            List<T> results = new List<T>();
            Vector3Int centerKey = WorldToGrid(center);

            for (int x = -cellRadius; x <= cellRadius; x++)
                for (int y = -cellRadius; y <= cellRadius; y++)
                    for (int z = -cellRadius; z <= cellRadius; z++)
                    {
                        T chunk = GetChunk(centerKey + new Vector3Int(x, y, z));
                        if (chunk != null) results.Add(chunk);
                    }
            return results;
        }
        #endregion

        #region State Management & Utilities
        /// <summary>
        /// Maps a world position to a grid key using the registry's settings.
        /// </summary>
        public Vector3Int WorldToGrid(Vector3 pos)
            => SpatialUtils.PositionToKey3D(pos, GridSize, Anchor);

        /// <summary>
        /// Clears all data and resets the ID pool.
        /// </summary>
        public void Clear()
        {
            _grid.Clear();
            _idPool.Clear();
            _nextId = 0;
            _globalDirty = true;
        }

        /// <summary> Marks the entire registry as dirty, forcing a full GPU re-sync. </summary>
        public void MarkGlobalDirty() => _globalDirty = true;

        /// <summary>
        /// Determines if any data needs a GPU update.
        /// </summary>
        public bool NeedsGPUUpdate()
        {
            if (_globalDirty) return true;

            foreach (var chunk in _grid.Values)
            {
                if (chunk.IsDirty()) return true;
            }
            return false;
        }

        /// <summary>
        /// Resets all dirty flags. Call this AFTER the GPU sync.
        /// </summary>
        public void ResetDirtyFlags()
        {
            _globalDirty = false;
            foreach (var chunk in _grid.Values)
                chunk.ClearDirty();
        }
        #endregion
    }
}
