using Rayforge.Core.Diagnostics;
using Rayforge.Core.Environment.Abstractions;
using Rayforge.Core.Environment.Spatial.Helpers;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;

namespace Rayforge.Core.Environment.Spatial.Chunks
{
    /// <summary>
    /// A specialized registry for fixed-grid WorldChunk3D instances.
    /// Implements spatial indexing, factory logic, and Floating Origin support.
    /// </summary>
    /// <typeparam name="T">The specific chunk type.</typeparam>
    public class ChunkRegistry<T> : SpatialRegistry<Vector3Int, T>, ISpatialGridProvider
        where T : Chunk<T>
    {
        #region Fields

#if UNITY_EDITOR
        public bool showDebugLogs = false;
#endif

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
        /// Updates the grid resolution and destroys all existing chunks
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

        public virtual bool GetOrCreateChunk(Vector3Int key, Action<T> onConfigure, out T chunk)
        {
            Vector3Int validKey = MaskKey(key);
            return CreateInternal(validKey, onConfigure, out chunk);
        }

        public bool GetOrCreateChunk(Vector2Int key2D, Action<T> onConfigure, out T chunk)
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

            return GetOrCreateChunk(key3d, onConfigure, out chunk);
        }

        public bool GetOrCreateChunkAtWorldPos(Vector3 pos, Action<T> onConfigure, out T chunk)
            => GetOrCreateChunk(WorldToGrid(pos), onConfigure, out chunk);

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
        private bool CreateInternal(Vector3Int validKey, Action<T> onConfigure, out T chunk)
        {
            LogDebug($"Creating Chunk at {validKey}");

            bool isNew = GetOrCreate(
                validKey,
                $"Chunk_{validKey.x}_{validKey.y}_{validKey.z}",
                GridToWorld(validKey),
                InitializeChunk,
                out chunk
            );
            onConfigure?.Invoke(chunk);
            return isNew;
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

        #region Spatial Mapping & ISpatialGridProvider

        /// <summary>
        /// Maps a world position to a grid key, masking out inactive axes.
        /// Implements ISpatialGridProvider.WorldToGrid.
        /// </summary>
        /// <param name="worldPos">The world-space position to convert.</param>
        /// <returns>The masked grid key.</returns>
        public Vector3Int WorldToGrid(Vector3 worldPos)
        {
            Vector3Int rawKey = SpatialUtils.PositionToKey3D(worldPos, (int)GridSize, Anchor);
            return MaskKey(rawKey);
        }

        /// <summary>
        /// Maps a local position (relative to Anchor) to a grid key.
        /// </summary>
        public Vector3Int LocalToGrid(Vector3 localPos)
        {
            Vector3Int rawKey = SpatialUtils.PositionToKey3D(localPos, (int)GridSize, Vector3.zero);
            return MaskKey(rawKey);
        }

        /// <summary>
        /// Calculates the world-space center of a cell.
        /// Inactive axes default to the Anchor's position.
        /// </summary>
        public Vector3 GridToWorld(Vector3Int key)
        {
            Vector3 pos = SpatialUtils.KeyToPosition3D(key, (int)GridSize, Anchor, centered: true);
            return MaskWorld(pos);
        }

        /// <summary>
        /// Calculates the world-space AABB for a given grid key.
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

        // --- Key Discovery (Bounds) ---

        /// <summary>
        /// Returns all grid coordinates (keys) that are touched by the given world-space bounds.
        /// </summary>
        /// <param name="worldBounds">The bounding box in world space to check against.</param>
        /// <returns>An enumerable of grid keys intersected by the bounds.</returns>
        public IEnumerable<Vector3Int> GetKeysInBounds(Bounds worldBounds)
        {
            Vector3Int minKey = WorldToGrid(worldBounds.min);
            Vector3Int maxKey = WorldToGrid(worldBounds.max);

            for (int x = minKey.x; x <= maxKey.x; x++)
                for (int y = minKey.y; y <= maxKey.y; y++)
                    for (int z = minKey.z; z <= maxKey.z; z++)
                        yield return new Vector3Int(x, y, z);
        }

        /// <summary>
        /// Returns all grid coordinates (keys) that are touched by the given local-space bounds (relative to Anchor).
        /// </summary>
        /// <param name="relativeBounds">The bounding box relative to the grid anchor.</param>
        /// <returns>An enumerable of grid keys intersected by the relative bounds.</returns>
        public IEnumerable<Vector3Int> GetKeysInRelativeBounds(Bounds relativeBounds)
        {
            Vector3Int minKey = LocalToGrid(relativeBounds.min);
            Vector3Int maxKey = LocalToGrid(relativeBounds.max);

            for (int x = minKey.x; x <= maxKey.x; x++)
                for (int y = minKey.y; y <= maxKey.y; y++)
                    for (int z = minKey.z; z <= maxKey.z; z++)
                        yield return new Vector3Int(x, y, z);
        }

        // --- Key Discovery (Radius) ---

        /// <summary>
        /// Returns all grid keys within a specified world-space radius.
        /// </summary>
        /// <param name="worldCenter">The center point of the search in world space.</param>
        /// <param name="radius">The search radius in meters.</param>
        /// <param name="useEdgeDistance">If true, calculates distance to the cell's closest edge. If false, uses cell center.</param>
        /// <returns>An enumerable of keys within the given range.</returns>
        public IEnumerable<Vector3Int> GetKeysInRadius(Vector3 worldCenter, float radius, bool useEdgeDistance = true)
        {
            float sqrRadius = radius * radius;
            Bounds searchBounds = new Bounds(worldCenter, Vector3.one * radius * 2f);

            foreach (var key in GetKeysInBounds(searchBounds))
            {
                float sqrDist = useEdgeDistance
                    ? GetSqrDistanceToClosestEdge(key, worldCenter)
                    : GetSqrDistanceToCenter(key, worldCenter);

                if (sqrDist <= sqrRadius)
                    yield return key;
            }
        }

        /// <summary>
        /// Returns all grid keys within a specified radius relative to the Anchor.
        /// </summary>
        /// <param name="relativeCenter">The center point relative to the grid anchor.</param>
        /// <param name="radius">The search radius in meters.</param>
        /// <param name="useEdgeDistance">If true, calculates distance to the cell's closest edge. If false, uses cell center.</param>
        /// <returns>An enumerable of keys within the given range.</returns>
        public IEnumerable<Vector3Int> GetKeysInRelativeRadius(Vector3 relativeCenter, float radius, bool useEdgeDistance = true)
        {
            return GetKeysInRadius(relativeCenter + Anchor, radius, useEdgeDistance);
        }

        // --- Distance Metrics ---

        /// <summary>
        /// Calculates the squared distance from a world position to the closest edge/point of a grid cell (AABB distance).
        /// </summary>
        /// <param name="key">The grid coordinate of the cell.</param>
        /// <param name="worldPos">The reference position in world space.</param>
        /// <returns>The squared distance to the cell edge. Returns 0 if the position is inside the cell.</returns>
        public float GetSqrDistanceToClosestEdge(Vector3Int key, Vector3 worldPos)
        {
            Vector3 center = GridToWorld(key);
            float halfSize = (int)GridSize * 0.5f;
            float sqrDist = 0;

            // X-Axis
            if (IsXActive)
            {
                float delta = Mathf.Max(0, Mathf.Abs(worldPos.x - center.x) - halfSize);
                sqrDist += delta * delta;
            }
            // Y-Axis
            if (IsYActive)
            {
                float delta = Mathf.Max(0, Mathf.Abs(worldPos.y - center.y) - halfSize);
                sqrDist += delta * delta;
            }
            // Z-Axis
            if (IsZActive)
            {
                float delta = Mathf.Max(0, Mathf.Abs(worldPos.z - center.z) - halfSize);
                sqrDist += delta * delta;
            }

            return sqrDist;
        }

        /// <summary>
        /// Calculates the squared distance from a world position to the closest edge of the cell containing the target position.
        /// </summary>
        /// <param name="targetPos">World position used to identify the target cell.</param>
        /// <param name="worldPos">The reference position in world space.</param>
        /// <returns>The squared distance to the identifying cell's edge.</returns>
        public float GetSqrDistanceToClosestEdge(Vector3 targetPos, Vector3 worldPos)
        {
            return GetSqrDistanceToClosestEdge(WorldToGrid(targetPos), worldPos);
        }

        /// <summary>
        /// Calculates the squared distance from a world position to the exact center of a grid cell.
        /// </summary>
        /// <param name="key">The grid coordinate of the cell.</param>
        /// <param name="worldPos">The reference position in world space.</param>
        /// <returns>The squared Euclidean distance to the cell center.</returns>
        public float GetSqrDistanceToCenter(Vector3Int key, Vector3 worldPos)
        {
            return Vector3.SqrMagnitude(worldPos - GridToWorld(key));
        }

        // --- Transformation & Bounds ---

        /// <summary>
        /// Returns the world-space center of a specific grid cell key.
        /// </summary>
        /// <param name="key">The grid coordinate.</param>
        /// <returns>The world-space center position.</returns>
        public Vector3 GetCellCenter(Vector3Int key) => GridToWorld(key);

        /// <summary>
        /// Returns the world-space center of the grid cell that contains the given world position.
        /// </summary>
        /// <param name="worldPos">A position within the desired cell.</param>
        /// <returns>The world-space center of the identified cell.</returns>
        public Vector3 GetCellCenter(Vector3 worldPos) => GridToWorld(WorldToGrid(worldPos));

        /// <summary>
        /// Returns the world-space axis-aligned bounding box (AABB) of a specific grid cell.
        /// </summary>
        /// <param name="key">The grid coordinate.</param>
        /// <returns>The world-space Bounds of the cell.</returns>
        public Bounds GetCellBounds(Vector3Int key) => GetBoundsForKey(key);

        /// <summary>
        /// Returns the world-space axis-aligned bounding box (AABB) of the cell containing the given world position.
        /// </summary>
        /// <param name="worldPos">A position within the desired cell.</param>
        /// <returns>The world-space Bounds of the identified cell.</returns>
        public Bounds GetCellBounds(Vector3 worldPos) => GetBoundsForKey(WorldToGrid(worldPos));

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

        #region Debug Helper

        [Conditional("UNITY_EDITOR")]
        private void LogDebug(string message, string color = "#FFAB91")
        {
            DebugOutput.Log(message, showDebugLogs, color);
        }

        #endregion
    }
}
