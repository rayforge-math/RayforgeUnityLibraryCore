using Rayforge.Core.Diagnostics;
using Rayforge.Core.Environment.Abstractions;
using Rayforge.Core.Environment.Spatial.Helpers;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;

namespace Rayforge.Core.Environment.Spatial
{
    /// <summary>
    /// A specialized registry for fixed-grid WorldChunk3D instances.
    /// Implements spatial indexing, factory logic, and Floating Origin support.
    /// </summary>
    /// <typeparam name="T">The specific chunk type.</typeparam>
    public class ChunkRegistry<T> : SpatialRegistry<Vector3Int, T>, ISpatialGridProvider
        where T : Chunk<T>
    {
        #region Grid Settings
        /// <summary> The physical size of one side of a chunk cell. </summary>
        public GridSize GridSize { get; private set; }

        /// <summary> 
        /// The unique identification string of this registry instance.
        /// Useful for logging and identifying the container in the hierarchy.
        /// </summary>
        public override string RegistryName
        {
            get => base.RegistryName;
            protected set
            {
                _baseName = value;
                base.RegistryName = $"{_baseName}_{GridSize}";
            }
        }

        private string _baseName;

        /// <summary>
        /// English: Updates the grid resolution and destroys all existing chunks
        /// as their spatial keys are no longer valid for the new size.
        /// </summary>
        public void SetGridSize(GridSize newSize)
        {
            if (GridSize == newSize) return;

            LogDebug($"Reformatting Grid: {(int)GridSize}m -> {(int)newSize}m. Destroying all chunks.");

            ClearChunks();
            var oldSize = GridSize;
            GridSize = newSize;
            RegistryName = _baseName;
        }
        
        /// <summary> 
        /// The world-space origin offset for the grid calculation.
        /// Treated as a full 3D point to allow vertical offsets even for 2D grids.
        /// </summary>
        public Vector3 Anchor
        {
            get => _anchor;
            protected set
            {
                _anchor = value;
                if (!ContainerLinkedToAnchor && Container != null)
                    Container.transform.position = _anchor;
            }
        }
        private Vector3 _anchor;

        /// <summary> Cached mask to determine which axes are handled by this registry's indexing. </summary>
        private readonly SpatialAxes _axes;

        public bool IsXActive => (_axes & SpatialAxes.X) != 0;
        public bool IsYActive => (_axes & SpatialAxes.Y) != 0;
        public bool IsZActive => (_axes & SpatialAxes.Z) != 0;

        /// <summary>
        /// Checks if an axis is active by its dimension index (0=X, 1=Y, 2=Z).
        /// Useful for generic loops over dimensions.
        /// </summary>
        public bool IsAxisActive(int axisIndex)
        {
            return axisIndex switch
            {
                0 => (_axes & SpatialAxes.X) != 0,
                1 => (_axes & SpatialAxes.Y) != 0,
                2 => (_axes & SpatialAxes.Z) != 0,
                _ => false
            };
        }

        /// <summary>
        /// Returns the count of currently active axes (1D, 2D, or 3D).
        /// </summary>
        public int ActiveAxisCount()
        {
            int count = 0;
            if (IsXActive) count++;
            if (IsYActive) count++;
            if (IsZActive) count++;
            return count;
        }

        #endregion

        #region Debug Helper
        public bool showDebugLogs = false;

        [Conditional("UNITY_EDITOR")]
        private void LogDebug(string message, string color = "#FFAB91")
        {
            DebugOutput.Log(message, showDebugLogs, color);
        }
        #endregion

        public ChunkRegistry(GridSize gridSize, Vector3 initialAnchor, Transform container = null, string name = "ChunkRegistry")
            : base(container)
        {
            GridSize = gridSize;
            Anchor = initialAnchor;
            _axes = Chunk<T>.ActiveAxes;
            _baseName = name;
            RegistryName = _baseName;

            LogDebug($"Initialized: Size={GridSize}, Anchor={Anchor}, Axes={_axes}");
        }

        #region Factory Implementation

        public virtual T GetOrCreateChunk(Vector3Int key)
        {
            Vector3Int validKey = MaskKey(key);
            return CreateInternal(validKey);
        }

        public virtual T GetOrCreateChunk(Vector2Int key2D)
        {
            Vector3Int key3d = Vector3Int.zero;
            int currentDimension = 0;
            int targetDimensions = ActiveAxisCount();

            for (int i = 0; i < 3; i++)
            {
                if (IsAxisActive(i))
                {
                    key3d[i] = key2D[currentDimension++];
                    if (currentDimension == targetDimensions) break;
                }
            }

            return CreateInternal(MaskKey(key3d));
        }

        public T GetOrCreateChunkAtWorldPos(Vector3 pos)
            => GetOrCreateChunk(WorldToGrid(pos));

        /// <summary>
        /// Attempts to retrieve a chunk at a specific world position.
        /// Automatically converts the position to grid coordinates.
        /// </summary>
        /// <param name="pos">The world position to check.</param>
        /// <param name="chunk">The resulting chunk or null.</param>
        /// <returns>True if a chunk exists at this location, false otherwise.</returns>
        public bool TryGetChunkAtWorldPos(Vector3 pos, out T chunk)
        {
            Vector3Int key = WorldToGrid(pos);
            return TryGetEntry(key, out chunk);
        }

        /// <summary>
        /// Centralized logic to create and initialize a chunk.
        /// </summary>
        private T CreateInternal(Vector3Int validKey)
        {
            LogDebug($"Creating Chunk at {validKey}");

            return GetOrCreate(
                validKey,
                $"Chunk_{validKey.x}_{validKey.y}_{validKey.z}",
                GridToWorld(validKey),
                InitializeChunk
            );
        }

        /// <summary>
        /// Encapsulates the component setup and initial state.
        /// </summary>
        private T InitializeChunk(GameObject go, Vector3Int k)
        {
            T chunk = go.AddComponent<T>();
            chunk.GridKey = k;

            // Configure AABB extents based on the global grid size.
            float half = (int)GridSize * 0.5f;
            chunk.localExtent = new Vector3(
                IsXActive ? half : 0,
                IsYActive ? half : 0,
                IsZActive ? half : 0
            );

            chunk.SuppressTransformDirtyOnce();
            return chunk;
        }

        #endregion

        #region Spatial Mapping

        /// <summary> 
        /// Maps position to key, masking out inactive axes for dictionary storage. 
        /// </summary>
        public Vector3Int WorldToGrid(Vector3 pos)
        {
            Vector3Int rawKey = SpatialUtils.PositionToKey3D(pos, (int)GridSize, Anchor);
            return MaskKey(rawKey);
        }

        /// <summary>
        /// Maps a local position (already relative to Anchor) to a grid key.
        /// Use this when working with internal registry data to avoid double-anchor subtraction.
        /// </summary>
        public Vector3Int LocalToGrid(Vector3 localPos)
        {
            Vector3Int rawKey = SpatialUtils.PositionToKey3D(localPos, (int)GridSize, Vector3.zero);
            return MaskKey(rawKey);
        }

        /// <summary> 
        /// Calculates world-space center. 
        /// Inactive axes default to the Anchor's position, allowing vertical shifts for XZ grids.
        /// </summary>
        public Vector3 GridToWorld(Vector3Int key)
        {
            Vector3 pos = SpatialUtils.KeyToPosition3D(key, (int)GridSize, Anchor, centered: true);
            return MaskWorld(pos);
        }

        /// <summary>
        /// Calculates the anchor-relative AABB for a given grid key.
        /// Useful for intersection tests without needing an actual Chunk instance.
        /// </summary>
        public Bounds GetBoundsForKey(Vector3Int key)
        {
            Vector3 center = GridToWorld(key);

            Vector3 size = new Vector3(
                IsXActive ? (int)GridSize : 0.01f,
                IsYActive ? (int)GridSize : 0.01f,
                IsZActive ? (int)GridSize : 0.01f
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

        /// <summary>
        /// Calculates which grid keys are touched by bounds that are already relative to the Anchor.
        /// Ideal for internal registry updates after an object is already stored.
        /// </summary>
        public IEnumerable<Vector3Int> GetKeysInRelativeBounds(Bounds relativeBounds)
        {
            Vector3Int minKey = LocalToGrid(relativeBounds.min);
            Vector3Int maxKey = LocalToGrid(relativeBounds.max);

            for (int x = minKey.x; x <= maxKey.x; x++)
            {
                for (int y = minKey.y; y <= maxKey.y; y++)
                {
                    for (int z = minKey.z; z <= maxKey.z; z++)
                    {
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
            Anchor += delta;
            LogDebug($"Origin Shift detected: Delta {delta}. New Anchor: {Anchor}");
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
                IsXActive ? key.x : 0,
                IsYActive ? key.y : 0,
                IsZActive ? key.z : 0
            );
        }

        /// <summary> 
        /// Forces inactive world-space axes to the Anchor's position. 
        /// Prevents 2D grids from having 3D floating-point offsets.
        /// </summary>
        private Vector3 MaskWorld(Vector3 pos)
        {
            return new Vector3(
                IsXActive ? pos.x : Anchor.x,
                IsYActive ? pos.y : Anchor.y,
                IsZActive ? pos.z : Anchor.z
            );
        }

        #endregion
    }
}
