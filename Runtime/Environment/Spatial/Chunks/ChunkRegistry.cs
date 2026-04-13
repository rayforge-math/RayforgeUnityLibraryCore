using Rayforge.Core.Collections.Abstractions;
using Rayforge.Core.Collections.Iterator;
using Rayforge.Core.Environment.Abstractions;
using Rayforge.Core.Environment.Spatial.Helpers;
using Rayforge.Core.Execution.Abstractions;
using Rayforge.Core.Execution.Handler;
using System;
using System.Runtime.CompilerServices;
using UnityEngine;

namespace Rayforge.Core.Environment.Spatial.Chunks
{
    /// <summary>
    /// A specialized registry for fixed-grid Chunk instances.
    /// Implements spatial indexing, factory logic, and Floating Origin support.
    /// </summary>
    /// <typeparam name="T">The specific chunk type.</typeparam>
    public class ChunkRegistry<T> : SpatialRegistry<Vector3Int, T>, ISpatialGridProvider<Vector3Int>
        where T : Chunk<T>
    {
        #region Internal Structures

        private struct CreateMeta
        {
            public GridSize gridSize;
            public bool isXActive;
            public bool isYActive;
            public bool isZActive;
        }

        #endregion

        #region Events & State

        /// <summary>
        /// Triggered when the grid's scale or fundamental structure changes 
        /// (e.g., GridSize change). Requires a full rebuild of dependent systems.
        /// </summary>
        public event Action<ISpatialGridProvider<Vector3Int>> OnGridStructureChanged;

        /// <summary> 
        /// Triggered when the grid origin shifts. 
        /// Passes the provider and the delta movement.
        /// </summary>
        public event Action<ISpatialGridProvider<Vector3Int>, Vector3> OnAnchorChanged;

        /// <summary> Gets the total number of cells currently tracked in the registry. </summary>
        public int TotalCellCount => Count;

        #endregion

        #region Configuration Fields

        private GridSize _gridSize;
        private string _baseName;
        private Vector3 _anchor;
        private SpatialAxes _axes;

        /// <summary> The physical size of one side of a grid cell. </summary>
        public GridSize GridSize
        {
            get => _gridSize;
            private set
            {
                if (_gridSize == value) return;
                if ((int)value <= 0) throw new ArgumentException($"{Tag} GridSize must be positive.");

                _gridSize = value;
                Clear();
                RegistryName = _baseName;
                OnGridStructureChanged?.Invoke(this);
            }
        }

        /// <summary> The world-space origin offset for the grid calculation. </summary>
        public Vector3 Anchor
        {
            get => _anchor;
            protected set
            {
                if (_anchor == value) return;
                if (float.IsNaN(value.x) || float.IsNaN(value.y) || float.IsNaN(value.z))
                    throw new ArgumentException($"{Tag} Anchor cannot contain NaN values.");

                Vector3 delta = value - _anchor;
                _anchor = value;

                if (!ContainerLinkedToAnchor && Container != null)
                    Container.transform.position = _anchor;

                OnAnchorChanged?.Invoke(this, delta);
            }
        }

        public override string RegistryName
        {
            get => base.RegistryName;
            protected set
            {
                _baseName = value;
                base.RegistryName = $"{_baseName}_{GridSize}";
            }
        }

        #endregion

        #region Public Configuration API

        /// <summary>
        /// Updates the grid resolution and returns whether a change occurred.
        /// <para><b>Warning:</b> Changing the size invalidates all existing spatial keys. 
        /// This will trigger a destruction of all active chunks to prevent coordinate misalignment.</para>
        /// </summary>
        /// <param name="newSize">The new binary grid size to apply.</param>
        /// <returns>True if the grid size was different from the current value, signaling a mandatory reset.</returns>
        public bool SetGridSize(GridSize newSize)
        {
            if (GridSize == newSize) return false;

            GridSize = newSize;
            return true;
        }

        /// <summary>
        /// Updates the grid origin and returns whether the anchor has shifted significantly.
        /// Useful for Floating Origin systems to avoid redundant LOD recalculations.
        /// </summary>
        /// <param name="newAnchor">The new world-space position to act as the coordinate root.</param>
        /// <returns>True if the anchor position was updated (exceeds epsilon threshold).</returns>
        public bool SetAnchor(Vector3 newAnchor)
        {
            if (Vector3.SqrMagnitude(Anchor - newAnchor) < 0.0001f) return false;

            Anchor = newAnchor;
            return true;
        }

        #endregion

        #region Axis Management

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

        #region Lifecycle

        /// <summary>
        /// Default constructor. English: Initialize() must be called to setup the grid parameters.
        /// </summary>
        public ChunkRegistry() : base() { }

        /// <summary>
        /// Initializes the grid registry with spatial settings.
        /// Sets up axes based on the generic type T and configures the spatial anchor.
        /// </summary>
        /// <param name="settings">The spatial configuration (GridSize, Anchor).</param>
        /// <param name="parent">Optional parent transform in the Unity hierarchy.</param>
        /// <param name="name">Base name for the container GameObject.</param>
        public virtual void Initialize(SpatialSettings settings, Transform parent = null, string name = "ChunkRegistry")
        {
            try
            {
                base.Initialize(parent, name);

                _axes = Chunk<T>.ActiveAxes;
                if (_axes == 0)
                {
                    throw new InvalidOperationException($"No active axes defined for chunk type {typeof(T).Name}. Check the static constructor of your chunk class.");
                }

                _baseName = name;

                if ((int)settings.GridSize <= 0)
                    throw new ArgumentException($"Invalid GridSize: {settings.GridSize}. Size must be a positive power of two.");

                _gridSize = settings.GridSize;
                var delta = settings.Anchor - _anchor;
                _anchor = settings.Anchor;

                RegistryName = _baseName;

                OnGridStructureChanged?.Invoke(this);
                OnAnchorChanged?.Invoke(this, delta);
            }
            catch (Exception e)
            {
                throw new Exception($"{Tag} Initialization failed: {e.Message}", e);
            }
        }

        #endregion

        #region Factory & Public Access API

        /// <summary>
        /// Retrieves an existing chunk or creates a new one. 
        /// The provided handler is used to configure the chunk without heap allocations.
        /// </summary>
        /// <typeparam name="THandler">The struct handler used to configure the chunk.</typeparam>
        /// <param name="key">The 3D grid coordinate.</param>
        /// <param name="onConfigure">A struct handler executed to set up the chunk.</param>
        /// <param name="chunk">The retrieved or newly created chunk.</param>
        /// <returns>True if a new chunk was created; false if an existing one was returned.</returns>
        public virtual bool GetOrCreateChunk<THandler>(Vector3Int key, ref THandler onConfigure, out T chunk)
            where THandler : struct, IExecutionHandler<T>
        {
            Vector3Int validKey = MaskKey(key);
            return CreateInternal(validKey, ref onConfigure, out chunk);
        }

        /// <summary>
        /// Converts a world-space position to a grid coordinate and ensures a chunk exists at that location.
        /// </summary>
        /// <typeparam name="THandler">The struct handler used to configure the chunk.</typeparam>
        /// <param name="pos">The world-space position.</param>
        /// <param name="onConfigure">A struct handler executed to set up the chunk if it's newly created or needs refresh.</param>
        /// <param name="chunk">When this method returns, contains the chunk corresponding to the calculated grid position.</param>
        /// <returns>True if the chunk was successfully retrieved or created; otherwise, false.</returns>
        public bool GetOrCreateChunkAtWorldPos<THandler>(Vector3 pos, ref THandler onConfigure, out T chunk)
            where THandler : struct, IExecutionHandler<T>
            => GetOrCreateChunk(WorldToGrid(pos), ref onConfigure, out chunk);

        /// <summary>
        /// Maps a 2D grid coordinate to the active 3D axes of the volume and retrieves or creates the corresponding chunk.
        /// </summary>
        /// <typeparam name="THandler">The struct handler used to configure the chunk.</typeparam>
        /// <param name="key2D">The 2D coordinate to be projected into 3D space.</param>
        /// <param name="onConfigure">A struct handler executed to set up the chunk if it's newly created or needs refresh.</param>
        /// <param name="chunk">When this method returns, contains the chunk at the projected 3D location.</param>
        /// <returns>True if the chunk was successfully retrieved or created; otherwise, false.</returns>
        public bool GetOrCreateChunk<THandler>(Vector2Int key2D, ref THandler onConfigure, out T chunk)
            where THandler : struct, IExecutionHandler<T>
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

            return GetOrCreateChunk(key3d, ref onConfigure, out chunk);
        }

        /// <summary>
        /// Attempts to retrieve a chunk for a specific key.
        /// </summary>
        /// <param name="key">The key used by the registry.</param>
        /// <param name="chunk">The resulting chunk or null.</param>
        /// <returns>True if a chunk exists at this location, false otherwise.</returns>
        public bool TryGetChunk(Vector3Int key, out T chunk)
        {
            return TryGetEntry(key, out chunk);
        }

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
        /// Orchestrates the internal creation, naming, and configuration of a chunk.
        /// </summary>
        /// <typeparam name="THandler">The struct handler used to configure the chunk.</typeparam>
        /// <param name="validKey">The already masked and validated 3D grid coordinate.</param>
        /// <param name="onConfigure">A struct handler executed to finalize the chunk's setup after creation or retrieval.</param>
        /// <param name="chunk">When this method returns, contains the fully initialized chunk instance.</param>
        /// <returns>True if a brand new chunk was created; false if an existing one was retrieved.</returns>
        private bool CreateInternal<THandler>(Vector3Int validKey, ref THandler onConfigure, out T chunk)
            where THandler : struct, IExecutionHandler<T>
        {
            var factory = new LambdaFunction<ChunkCreateData, ChunkRegistry<T>, T>(
                this,
                static (data, coord) =>
                {
                    T newChunk = data.gameObject.AddComponent<T>();
                    newChunk.GridKey = data.key;

                    float half = (int)coord.GridSize * 0.5f;
                    newChunk.localExtent = new Vector3(
                        coord.IsXActive ? half : 0,
                        coord.IsYActive ? half : 0,
                        coord.IsZActive ? half : 0
                    );

                    newChunk.SuppressTransformDirtyOnce();
                    return newChunk;
                }
            );

            bool isNew = GetOrCreate(
                validKey,
                $"Chunk_{validKey.x}_{validKey.y}_{validKey.z}",
                GridToWorld(validKey),
                ref factory,
                out chunk
            );

            onConfigure.Execute(chunk);

            return isNew;
        }

        #endregion

        #region Coordinate Mapping

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

        #endregion

        #region Distance Metrics

        /// <summary>
        /// Calculates the squared distance from a world position to the closest edge/point of a grid cell (AABB distance).
        /// Respects the active axes of this registry.
        /// </summary>
        /// <param name="key">The grid coordinate of the cell.</param>
        /// <param name="worldPos">The reference position in world space.</param>
        /// <returns>The squared distance to the cell edge. Returns 0 if the position is inside the cell.</returns>
        public float GetSqrDistanceToClosestEdge(Vector3Int key, Vector3 worldPos)
        {
            return GetSqrDistanceToClosestEdgeStatic(worldPos, GridToWorld(key), (float)GridSize, IsXActive, IsYActive, IsZActive);
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
        /// Respects the active axes of this registry.
        /// </summary>
        /// <param name="key">The grid coordinate of the cell.</param>
        /// <param name="worldPos">The reference position in world space.</param>
        /// <returns>The squared Euclidean distance to the cell center.</returns>
        public float GetSqrDistanceToCenter(Vector3Int key, Vector3 worldPos)
        {
            return GetSqrDistanceToCenterStatic(worldPos, GridToWorld(key), IsXActive, IsYActive, IsZActive);
        }

        /// <summary>
        /// Calculates the squared distance from a world position to the center of the cell containing the target position.
        /// </summary>
        /// <param name="targetPos">World position used to identify the target cell.</param>
        /// <param name="worldPos">The reference position in world space.</param>
        /// <returns>The squared distance to the identified cell's center.</returns>
        public float GetSqrDistanceToCenter(Vector3 targetPos, Vector3 worldPos)
        {
            return GetSqrDistanceToCenter(WorldToGrid(targetPos), worldPos);
        }

        #endregion

        #region Static Distance Logic

        /// <summary>
        /// Static implementation to calculate squared distance to a cell's closest edge. 
        /// Useful for passing as a delegate or lambda to avoid closure allocations.
        /// </summary>
        /// <param name="worldPos">The reference position in world space.</param>
        /// <param name="cellCenter">The calculated world space center of the cell.</param>
        /// <param name="gridSize">The physical size of the grid cell.</param>
        /// <param name="xActive">Whether the X-axis distance should be included.</param>
        /// <param name="yActive">Whether the Y-axis distance should be included.</param>
        /// <param name="zActive">Whether the Z-axis distance should be included.</param>
        /// <returns>The squared distance to the AABB edge.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float GetSqrDistanceToClosestEdgeStatic(Vector3 worldPos, Vector3 cellCenter, float gridSize, bool xActive, bool yActive, bool zActive)
        {
            float halfSize = gridSize * 0.5f;
            float sqrDist = 0;

            if (xActive) sqrDist += SpatialUtils.GetSqrDistanceToClosestEdge1D(worldPos.x, cellCenter.x, halfSize);
            if (yActive) sqrDist += SpatialUtils.GetSqrDistanceToClosestEdge1D(worldPos.y, cellCenter.y, halfSize);
            if (zActive) sqrDist += SpatialUtils.GetSqrDistanceToClosestEdge1D(worldPos.z, cellCenter.z, halfSize);

            return sqrDist;
        }

        /// <summary>
        /// Static implementation to calculate squared distance to a cell's center.
        /// Useful for high-performance callbacks and multithreaded contexts.
        /// </summary>
        /// <param name="worldPos">The reference position in world space.</param>
        /// <param name="cellCenter">The calculated world space center of the cell.</param>
        /// <param name="xActive">Whether the X-axis distance should be included.</param>
        /// <param name="yActive">Whether the Y-axis distance should be included.</param>
        /// <param name="zActive">Whether the Z-axis distance should be included.</param>
        /// <returns>The squared Euclidean distance to the center.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float GetSqrDistanceToCenterStatic(Vector3 worldPos, Vector3 cellCenter, bool xActive, bool yActive, bool zActive)
        {
            float sqrDist = 0;

            if (xActive) sqrDist += SpatialUtils.GetSqrDistance1D(worldPos.x, cellCenter.x);
            if (yActive) sqrDist += SpatialUtils.GetSqrDistance1D(worldPos.y, cellCenter.y);
            if (zActive) sqrDist += SpatialUtils.GetSqrDistance1D(worldPos.z, cellCenter.z);

            return sqrDist;
        }

        #endregion

        #region Floating Origin Support

        /// <summary> 
        /// Synchronizes the anchor and all chunks with a world origin shift. 
        /// The Anchor shifts as a full 3D vector to stay aligned with world geometry.
        /// </summary>
        public void NotifyOriginShift(Vector3 delta)
        {
            Anchor += delta;
        }

        #endregion

        #region Flexible Iteration (Boxing)

        /// <inheritdoc />
        public IIterator<Vector3Int> GetKeysInBounds(Bounds worldBounds)
        {
            Vector3Int minKey = WorldToGrid(worldBounds.min);
            Vector3Int maxKey = WorldToGrid(worldBounds.max);
            return new Iterator<Vector3Int, GridRangeState>(new GridRangeState(minKey, maxKey));
        }

        /// <inheritdoc />
        public IIterator<Vector3Int> GetKeysInRelativeBounds(Bounds relativeBounds)
        {
            Vector3Int minKey = LocalToGrid(relativeBounds.min);
            Vector3Int maxKey = LocalToGrid(relativeBounds.max);
            return new Iterator<Vector3Int, GridRangeState>(new GridRangeState(minKey, maxKey));
        }

        /// <inheritdoc />
        public IIterator<Vector3Int> GetKeysInRadius(Vector3 worldCenter, float radius, bool useEdgeDistance = true)
        {
            Bounds searchBounds = new Bounds(worldCenter, Vector3.one * radius * 2f);
            Vector3Int minKey = WorldToGrid(searchBounds.min);
            Vector3Int maxKey = WorldToGrid(searchBounds.max);
            float gridSize = (float)GridSize;
            var radiusState = new GridRadiusState(minKey, maxKey, Anchor, worldCenter, radius, useEdgeDistance, gridSize, IsXActive, IsYActive, IsZActive);
            return new Iterator<Vector3Int, GridRadiusState>(radiusState);
        }

        /// <inheritdoc />
        public IIterator<Vector3Int> GetKeysInRelativeRadius(Vector3 relativeCenter, float radius, bool useEdgeDistance = true)
        {
            return GetKeysInRadius(relativeCenter + Anchor, radius, useEdgeDistance);
        }

        #endregion

        #region High-Performance Iteration (Zero-Allocation)

        /// <inheritdoc />
        public void ForEachKeyInBounds<TAction>(Bounds worldBounds, ref TAction action)
            where TAction : struct, IExecutionHandler<Vector3Int>
        {
            var state = new GridRangeState(WorldToGrid(worldBounds.min), WorldToGrid(worldBounds.max));
            var iterator = new Iterator<Vector3Int, GridRangeState>(state);

            while (iterator.MoveNext())
            {
                action.Execute(iterator.Current);
            }
        }

        /// <inheritdoc />
        public void ForEachKeyInRelativeBounds<TAction>(Bounds relativeBounds, ref TAction action)
            where TAction : struct, IExecutionHandler<Vector3Int>
        {
            var state = new GridRangeState(LocalToGrid(relativeBounds.min), LocalToGrid(relativeBounds.max));
            var iterator = new Iterator<Vector3Int, GridRangeState>(state);

            while (iterator.MoveNext())
            {
                action.Execute(iterator.Current);
            }
        }

        /// <inheritdoc />
        public void ForEachKeyInRadius<TAction>(Vector3 worldCenter, float radius, ref TAction action, bool useEdgeDistance = true)
            where TAction : struct, IExecutionHandler<Vector3Int>
        {
            Bounds searchBounds = new Bounds(worldCenter, Vector3.one * radius * 2f);
            Vector3Int minKey = WorldToGrid(searchBounds.min);
            Vector3Int maxKey = WorldToGrid(searchBounds.max);
            float gridSize = (float)GridSize;
            var radiusState = new GridRadiusState(minKey, maxKey, Anchor, worldCenter, radius, useEdgeDistance, gridSize, IsXActive, IsYActive, IsZActive);
            var iterator = new Iterator<Vector3Int, GridRadiusState>(radiusState);

            while (iterator.MoveNext())
            {
                action.Execute(iterator.Current);
            }
        }

        /// <inheritdoc />
        public void ForEachKeyInRelativeRadius<TAction>(Vector3 relativeCenter, float radius, ref TAction action, bool useEdgeDistance = true)
            where TAction : struct, IExecutionHandler<Vector3Int>
        {
            ForEachKeyInRadius(relativeCenter + Anchor, radius, ref action, useEdgeDistance);
        }

        #endregion

        #region Helpers

        /// <summary> 
        /// Forces inactive axes to 0 for consistent dictionary keys. 
        /// This defines the "dimensionality" of the grid (e.g., 2D XZ vs 3D).
        /// </summary>
        private Vector3Int MaskKey(Vector3Int key)
        {
            if (_axes == 0) return key;

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
