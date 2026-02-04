using Rayforge.Core.Environment.Spatial.Helpers;
using UnityEngine;

namespace Rayforge.Core.Environment.Spatial
{
    /// <summary>
    /// A specialized registry for fixed-grid WorldChunk3D instances.
    /// Implements spatial indexing, factory logic, and Floating Origin support.
    /// </summary>
    public class ChunkRegistry<T> : SpatialRegistry<Vector3Int, T>
        where T : Chunk3D<T>
    {
        #region Grid Settings
        /// <summary> The physical size of one side of a chunk cell. </summary>
        public int GridSize { get; }

        /// <summary> The world-space origin offset for the grid calculation. </summary>
        public Vector3 Anchor { get; protected set; }
        #endregion

        public ChunkRegistry(ChunkSize gridSize, Vector3 initialAnchor, Transform container = null)
            : base(container)
        {
            GridSize = (int)gridSize;
            Anchor = initialAnchor;
        }

        #region Factory & Queries
        /// <summary>
        /// Retrieves an existing chunk or creates a new one if the coordinate is empty.
        /// </summary>
        public virtual T GetOrCreateChunk(Vector3Int key)
        {
            T existing = GetEntry(key);
            if (existing != null) return existing;

            GameObject go = new GameObject($"Chunk_{key.x}_{key.y}_{key.z}");
            if (_container != null) go.transform.SetParent(_container);
            go.transform.position = GridToWorld(key);

            T chunk = go.AddComponent<T>();
            chunk.currentGridKey = key;

            _storage[key] = chunk;
            _globalDirty = true;

            return chunk;
        }

        /// <summary> Ensures a chunk exists at the given world position. </summary>
        public T GetOrCreateChunkAtWorldPos(Vector3 pos) => GetOrCreateChunk(WorldToGrid(pos));

        /// <summary> Retrieves a chunk at world position without creating it. </summary>
        public T GetChunkAtWorldPos(Vector3 pos) => GetEntry(WorldToGrid(pos));
        #endregion

        #region Spatial Mapping
        /// <summary> Maps world position to grid key via Anchor. </summary>
        public Vector3Int WorldToGrid(Vector3 pos)
            => SpatialUtils.PositionToKey3D(pos, GridSize, Anchor);

        /// <summary> Calculates the world-space center of a grid cell. </summary>
        public Vector3 GridToWorld(Vector3Int key)
        {
            return Anchor + new Vector3(
                key.x * GridSize + (GridSize * 0.5f),
                key.y * GridSize + (GridSize * 0.5f),
                key.z * GridSize + (GridSize * 0.5f)
            );
        }
        #endregion

        #region Origin Shift
        /// <summary> Adjusts the anchor and suppresses transform updates. </summary>
        public void NotifyOriginShift(Vector3 delta)
        {
            Anchor += delta;
            foreach (var chunk in AllEntries)
                chunk.SuppressTransformDirtyOnce();
        }
        #endregion
    }
}
