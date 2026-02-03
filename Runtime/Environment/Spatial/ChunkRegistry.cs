using Rayforge.Core.Environment.Spatial.Helpers;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using static Codice.Client.Commands.WkTree.WorkspaceTreeNode;

namespace Rayforge.Core.Environment.Spatial
{
    /// <summary>
    /// A high-performance C# class for managing WorldChunk3D instances.
    /// Handles spatial indexing and Floating Origin via an internal Anchor.
    /// Resource lifecycle (Leases) is managed in derived classes.
    /// </summary>
    /// <typeparam name="T">The type of chunk managed by this registry.</typeparam>
    public class ChunkRegistry<T> 
        where T : Chunk3D<T>
    {
        #region Data Structures
        /// <summary> Main storage for chunks indexed by their 3D grid coordinates. </summary>
        protected readonly Dictionary<Vector3Int, T> _grid = new Dictionary<Vector3Int, T>();

        /// <summary> Flag indicating if the collection composition (count) has changed. </summary>
        protected bool _globalDirty = false;

        /// <summary> The physical size of one side of a chunk cell. Set via constructor. </summary>
        public float GridSize { get; }

        /// <summary> 
        /// The world-space origin offset for the grid calculation. 
        /// Initialized to the owner's position and updated during Origin Shifts.
        /// </summary>
        public Vector3 Anchor { get; protected set; }

        /// <summary> Provides access to all currently registered chunks. </summary>
        public Dictionary<Vector3Int, T>.ValueCollection AllChunks => _grid.Values;
        #endregion

        /// <summary>
        /// Initializes the registry with a fixed grid size and an initial world anchor.
        /// </summary>
        /// <param name="gridSize">The size of the grid cells.</param>
        /// <param name="initialAnchor">The starting world position (usually the manager's position).</param>
        public ChunkRegistry(float gridSize, Vector3 initialAnchor)
        {
            GridSize = gridSize;
            Anchor = initialAnchor;
        }

        #region Registration
        /// <summary>
        /// Registers a chunk and maps it to the grid based on its current position and the anchor.
        /// </summary>
        /// <param name="chunk">The chunk instance to register.</param>
        public virtual void Register(T chunk)
        {
            Vector3Int key = WorldToGrid(chunk.transform.position);
            chunk.currentGridKey = key;

            _grid[key] = chunk;
            _globalDirty = true;
        }

        /// <summary>
        /// Removes a chunk from the grid using its stored key.
        /// Immune to floating origin shifts since the key is stored internally.
        /// </summary>
        /// <param name="chunk">The chunk instance to unregister.</param>
        public virtual void Unregister(T chunk)
        {
            Vector3Int key = chunk.currentGridKey;

            if (_grid.TryGetValue(key, out T current) && current == chunk)
            {
                _grid.Remove(key);
                _globalDirty = true;
            }
        }
        #endregion

        #region Origin Shift Awareness
        /// <summary>
        /// Adjusts the internal anchor and notifies all chunks to ignore the transform jump.
        /// This should be called by an external relay or manager when the world shifts.
        /// </summary>
        /// <param name="delta">The world-space offset of the shift.</param>
        public void ApplyOriginShift(Vector3 delta)
        {
            Anchor += delta;

            foreach (var chunk in _grid.Values)
            {
                chunk.SuppressTransformDirtyOnce();
            }
        }
        #endregion

        #region Spatial Queries
        /// <summary> Retrieves a chunk at a specific grid coordinate. </summary>
        public T GetChunk(Vector3Int key)
        {
            _grid.TryGetValue(key, out T chunk);
            return chunk;
        }

        /// <summary> Retrieves a chunk based on its absolute world position. </summary>
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

        #region State Management
        /// <summary>
        /// Maps a world position to a grid key using the registry's settings and anchor.
        /// </summary>
        public Vector3Int WorldToGrid(Vector3 pos)
            => SpatialUtils.PositionToKey3D(pos, GridSize, Anchor);

        /// <summary> Clears all data from the registry. </summary>
        public virtual void Clear()
        {
            _grid.Clear();
            _globalDirty = true;
        }

        /// <summary> Determines if any data needs a GPU update. </summary>
        public bool NeedsGPUUpdate()
        {
            if (_globalDirty) return true;

            foreach (var chunk in _grid.Values)
            {
                if (chunk.IsDirty()) return true;
            }
            return false;
        }

        /// <summary> Resets all dirty flags. Call this AFTER the GPU sync/bake. </summary>
        public void ResetDirtyFlags()
        {
            _globalDirty = false;
            foreach (var chunk in _grid.Values)
                chunk.ClearDirty();
        }
        #endregion
    }
}
