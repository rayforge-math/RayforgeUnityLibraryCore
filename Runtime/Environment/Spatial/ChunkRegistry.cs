using Rayforge.Core.Environment.Spatial.Helpers;
using System.Collections.Generic;
using UnityEngine;

namespace Rayforge.Core.Environment.Spatial
{
    /// <summary>
    /// A specialized registry for fixed-grid WorldChunk3D instances.
    /// Implements spatial indexing, factory logic, and Floating Origin support.
    /// </summary>
    /// <typeparam name="T">The specific chunk type.</typeparam>
    public class ChunkRegistry<T> : SpatialRegistry<Vector3Int, T>
        where T : Chunk<T>
    {
        #region Grid Settings
        /// <summary> The physical size of one side of a chunk cell. </summary>
        public int GridSize { get; }

        /// <summary> 
        /// The world-space origin offset for the grid calculation. 
        /// Treated as a full 3D point to allow vertical offsets even for 2D grids.
        /// </summary>
        public Vector3 Anchor { get; protected set; }

        /// <summary> Cached mask to determine which axes are handled by this registry's indexing. </summary>
        private readonly SpatialAxes _axes;
        #endregion

        public ChunkRegistry(ChunkSize gridSize, Vector3 initialAnchor, Transform container = null)
        : base(container, $"ChunkRegistry_{gridSize}")
        {
            GridSize = (int)gridSize;
            Anchor = initialAnchor;
            _axes = Chunk<T>.ActiveAxes;
        }

        #region Factory Implementation

        public virtual T GetOrCreateChunk(Vector3Int key)
        {
            Vector3Int validKey = MaskKey(key);

            return GetOrCreate(
                validKey,
                $"Chunk_{validKey.x}_{validKey.y}_{validKey.z}",
                GridToWorld(validKey),
                (go, k) => {
                    T chunk = go.AddComponent<T>();
                    chunk.GridKey = k;

                    // Configure AABB extents for the chunk.
                    float half = GridSize * 0.5f;
                    chunk.localExtent = new Vector3(half, half, half);

                    chunk.SuppressTransformDirtyOnce();
                    return chunk;
                }
            );
        }

        public T GetOrCreateChunkAtWorldPos(Vector3 pos)
            => GetOrCreateChunk(WorldToGrid(pos));

        public T GetChunkAtWorldPos(Vector3 pos)
            => GetEntry(WorldToGrid(pos));

        #endregion

        #region Spatial Mapping

        /// <summary> 
        /// Maps position to key, masking out inactive axes for dictionary storage. 
        /// </summary>
        public Vector3Int WorldToGrid(Vector3 pos)
        {
            Vector3Int rawKey = SpatialUtils.PositionToKey3D(pos, GridSize, Anchor);
            return MaskKey(rawKey);
        }

        /// <summary> 
        /// Calculates world-space center. 
        /// Inactive axes default to the Anchor's position, allowing vertical shifts for XZ grids.
        /// </summary>
        public Vector3 GridToWorld(Vector3Int key)
        {
            float half = GridSize * 0.5f;
            return new Vector3(
                ((_axes & SpatialAxes.X) != 0) ? (Anchor.x + key.x * GridSize + half) : Anchor.x,
                ((_axes & SpatialAxes.Y) != 0) ? (Anchor.y + key.y * GridSize + half) : Anchor.y,
                ((_axes & SpatialAxes.Z) != 0) ? (Anchor.z + key.z * GridSize + half) : Anchor.z
            );
        }

        /// <summary>
        /// Calculates the anchor-relative AABB for a given grid key.
        /// Useful for intersection tests without needing an actual Chunk instance.
        /// </summary>
        public Bounds GetBoundsForKey(Vector3Int key)
        {
            // Get the center using your existing logic (handles Anchor + GridSize).
            Vector3 center = GridToWorld(key);

            // The size is always the GridSize on active axes. 
            Vector3 size = new Vector3(
                ((_axes & SpatialAxes.X) != 0) ? GridSize : 0,
                ((_axes & SpatialAxes.Y) != 0) ? GridSize : 0,
                ((_axes & SpatialAxes.Z) != 0) ? GridSize : 0
            );

            return new Bounds(center, size);
        }

        /// <summary>
        /// Calculates the anchor-relative AABB for the chunk that contains the given world position.
        /// </summary>
        public Bounds GetBoundsAtWorldPos(Vector3 worldPos)
        {
            Vector3Int key = WorldToGrid(worldPos);
            return GetBoundsForKey(key);
        }

        /// <summary>
        /// Returns all grid keys covered by the given bounds.
        /// This method handles 1D, 2D, and 3D registries automatically via ActiveAxes.
        /// </summary>
        public IEnumerable<Vector3Int> GetKeysInBounds(Bounds relativeBounds)
        {
            Vector3Int minKey = WorldToGrid(relativeBounds.min);
            Vector3Int maxKey = WorldToGrid(relativeBounds.max);

            for (int x = minKey.x; x <= maxKey.x; x++)
            {
                for (int y = minKey.y; y <= maxKey.y; y++)
                {
                    for (int z = minKey.z; z <= maxKey.z; z++)
                    {
                        // Return the key. Masking is already handled by WorldToGrid.
                        yield return new Vector3Int(x, y, z);
                    }
                }
            }
        }

        #endregion

        #region Origin Shift

        /// <summary> 
        /// Synchronizes the anchor and all chunks with a world origin shift. 
        /// The Anchor shifts as a full 3D vector to stay aligned with world geometry.
        /// </summary>
        public void NotifyOriginShift(Vector3 delta)
        {
            // We remove the axis-check for the Anchor itself. 
            // If the world origin moves, our reference point must move identically.
            Anchor += delta;

            foreach (var chunk in AllEntries)
            {
                if (chunk == null) continue;

                // Re-calculate the new world position based on the shifted anchor.
                chunk.transform.position = GridToWorld(chunk.GridKey);
                chunk.SuppressTransformDirtyOnce();
            }
        }

        #endregion

        #region Helpers

        /// <summary> 
        /// Forces inactive axes to 0 for consistent dictionary keys. 
        /// This defines the "dimensionality" of the grid (e.g., 2D XZ vs 3D).
        /// </summary>
        private Vector3Int MaskKey(Vector3Int key)
        {
            return new Vector3Int(
                ((_axes & SpatialAxes.X) != 0) ? key.x : 0,
                ((_axes & SpatialAxes.Y) != 0) ? key.y : 0,
                ((_axes & SpatialAxes.Z) != 0) ? key.z : 0
            );
        }

        #endregion
    }
}
