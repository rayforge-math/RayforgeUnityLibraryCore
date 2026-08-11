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
        #region Events & State

        /// <summary>
        /// Triggered when the grid's scale or fundamental structure changes 
        /// (e.g., GridSize change). Requires a full rebuild of dependent systems.
        /// </summary>
        public event Action<ISpatialGridConfiguration<Vector3Int>> OnGridStructureChanged;

        /// <summary> 
        /// Triggered when the grid origin shifts. 
        /// Passes the provider and the delta movement.
        /// </summary>
        public event Action<ISpatialGridConfiguration<Vector3Int>, Vector3> OnAnchorChanged;

        #endregion

        #region Configuration Fields

        private GridSize m_GridSize;
        private string m_BaseName;
        private Vector3 m_Anchor;
        private SpatialAxes m_ActiveAxes;

        /// <summary> The physical size of one side of a grid cell. </summary>
        public GridSize GridSize
        {
            get => m_GridSize;
            set
            {
                if (m_GridSize == value) return;
                if ((int)value <= 0) throw new ArgumentException("GridSize must be positive.");

                m_GridSize = value;
                Clear();
                RegistryName = m_BaseName;
                OnGridStructureChanged?.Invoke(this);
            }
        }

        /// <summary> The world-space origin offset for the grid calculation. </summary>
        public Vector3 Anchor
        {
            get => m_Anchor;
            set
            {
                // Epsilon check to prevent micro-shifts from triggering full recalculations
                if (Vector3.SqrMagnitude(m_Anchor - value) < 0.0001f) return;

                if (float.IsNaN(value.x) || float.IsNaN(value.y) || float.IsNaN(value.z))
                    throw new ArgumentException("Anchor cannot contain NaN values.");

                Vector3 delta = value - m_Anchor;
                m_Anchor = value;

                if (!ContainerLinkedToAnchor && Container != null)
                    Container.transform.position = m_Anchor;

                OnAnchorChanged?.Invoke(this, delta);
            }
        }

        public override string RegistryName
        {
            get => base.RegistryName;
            set
            {
                m_BaseName = value;
                base.RegistryName = $"{m_BaseName}_{GridSize}";
            }
        }

        #endregion

        #region Lifecycle

        /// <summary>
        /// Default constructor. Initialize must be called to setup the grid parameters.
        /// </summary>
        public ChunkRegistry() : base() { }

        /// <summary>
        /// Initializes the grid registry with spatial settings.
        /// Sets up axes based on the generic type T and configures the spatial anchor.
        /// </summary>
        /// <param name="gridSize">The size of the spatial grid.</param>
        /// <param name="anchor">The spatial anchor position.</param>
        /// <param name="parent">Optional parent transform in the Unity hierarchy.</param>
        /// <param name="name">Base name for the container GameObject.</param>
        public virtual void Initialize(GridSize gridSize, Vector3 anchor, Transform parent = null, string name = "ChunkRegistry")
        {
            if ((int)gridSize <= 0)
                throw new ArgumentException($"Invalid GridSize: {gridSize}. Size must be a positive number.", nameof(gridSize));

            m_ActiveAxes = Chunk<T>.ActiveAxes;
            if (m_ActiveAxes == 0)
            {
                throw new InvalidOperationException($"No active axes defined for chunk type {typeof(T).Name}. Check the [ChunkConfig] attribute on your chunk class.");
            }

            Reset();
            base.Initialize(parent, name);

            m_BaseName = string.IsNullOrEmpty(name) ? "ChunkRegistry" : name;
            m_GridSize = gridSize;

            var delta = anchor - m_Anchor;
            m_Anchor = anchor;

            RegistryName = m_BaseName;

            OnGridStructureChanged?.Invoke(this);

            if (delta != Vector3.zero)
            {
                OnAnchorChanged?.Invoke(this, delta);
            }
        }

        #endregion

        #region Axis Management

        public SpatialAxes ActiveAxes => m_ActiveAxes;

        public bool IsXActive => (m_ActiveAxes & SpatialAxes.X) != 0;
        public bool IsYActive => (m_ActiveAxes & SpatialAxes.Y) != 0;
        public bool IsZActive => (m_ActiveAxes & SpatialAxes.Z) != 0;

        /// <summary>
        /// Checks if an axis is active by its dimension index (0=X, 1=Y, 2=Z).
        /// Useful for generic loops over dimensions.
        /// </summary>
        public bool IsAxisActive(int axisIndex)
        {
            return axisIndex switch
            {
                0 => IsXActive,
                1 => IsYActive,
                2 => IsZActive,
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

        #region ISpatialGridProvider Implementation

        /// <inheritdoc />
        public Vector3Int WorldToGrid(Vector3 worldPos)
        {
            if (float.IsNaN(worldPos.x) || float.IsNaN(worldPos.y) || float.IsNaN(worldPos.z))
                throw new ArgumentException("World position cannot contain NaN values.", nameof(worldPos));

            Vector3Int rawKey = SpatialUtils.PositionToKey3D(worldPos, (int)GridSize, Anchor);
            return MaskKey(rawKey);
        }

        /// <inheritdoc />
        public Vector3 GridToWorld(Vector3Int key)
        {
            Vector3 pos = SpatialUtils.KeyToPosition3D(key, (int)GridSize, Anchor, centered: true);
            return MaskWorld(pos);
        }

        /// <inheritdoc />
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

        /// <inheritdoc />
        public IIterator<Vector3Int> GetKeysInBounds(Bounds worldBounds)
        {
            if (float.IsNaN(worldBounds.center.x) || float.IsNaN(worldBounds.size.x))
                throw new ArgumentException("World bounds contain NaN values.", nameof(worldBounds));

            Vector3Int minKey = WorldToGrid(worldBounds.min);
            Vector3Int maxKey = WorldToGrid(worldBounds.max);
            return new Iterator<Vector3Int, GridRangeState>(new GridRangeState(minKey, maxKey));
        }

        /// <inheritdoc />
        public IIterator<Vector3Int> GetKeysInRelativeBounds(Bounds relativeBounds)
        {
            if (float.IsNaN(relativeBounds.center.x) || float.IsNaN(relativeBounds.size.x))
                throw new ArgumentException("Relative bounds contain NaN values.", nameof(relativeBounds));

            Vector3Int minKey = LocalToGrid(relativeBounds.min);
            Vector3Int maxKey = LocalToGrid(relativeBounds.max);
            return new Iterator<Vector3Int, GridRangeState>(new GridRangeState(minKey, maxKey));
        }

        /// <inheritdoc />
        public IIterator<Vector3Int> GetKeysInRadius(Vector3 worldCenter, float radius, bool useEdgeDistance = true)
        {
            if (float.IsNaN(worldCenter.x) || float.IsNaN(worldCenter.y) || float.IsNaN(worldCenter.z))
                throw new ArgumentException("World center cannot contain NaN values.", nameof(worldCenter));
            if (radius < 0f)
                throw new ArgumentException("Radius must be non-negative.", nameof(radius));

            Bounds searchBounds = new Bounds(worldCenter, Vector3.one * radius * 2f);
            Vector3Int minKey = WorldToGrid(searchBounds.min);
            Vector3Int maxKey = WorldToGrid(searchBounds.max);
            float gridSize = (float)GridSize;
            Vector3 localCenter = worldCenter - Anchor;

            if (useEdgeDistance)
            {
                var edgeState = new GridRadiusEdgeState(minKey, maxKey, localCenter, radius, gridSize, m_ActiveAxes);
                return new Iterator<Vector3Int, GridRadiusEdgeState>(edgeState);
            }
            else
            {
                var centreState = new GridRadiusCentreState(minKey, maxKey, localCenter, radius, gridSize, m_ActiveAxes);
                return new Iterator<Vector3Int, GridRadiusCentreState>(centreState);
            }
        }

        /// <inheritdoc />
        public IIterator<Vector3Int> GetKeysInRelativeRadius(Vector3 relativeCenter, float radius, bool useEdgeDistance = true)
        {
            if (float.IsNaN(relativeCenter.x) || float.IsNaN(relativeCenter.y) || float.IsNaN(relativeCenter.z))
                throw new ArgumentException("Relative center cannot contain NaN values.", nameof(relativeCenter));
            if (radius < 0f)
                throw new ArgumentException("Radius must be non-negative.", nameof(radius));

            return GetKeysInRadius(relativeCenter + Anchor, radius, useEdgeDistance);
        }

        /// <inheritdoc />
        public void ForEachKeyInBounds<TAction>(Bounds worldBounds, ref TAction action)
            where TAction : struct, IExecutionHandler<Vector3Int>
        {
            if (float.IsNaN(worldBounds.min.x) || float.IsNaN(worldBounds.min.y) || float.IsNaN(worldBounds.min.z) ||
                float.IsNaN(worldBounds.max.x) || float.IsNaN(worldBounds.max.y) || float.IsNaN(worldBounds.max.z))
            {
                throw new ArgumentException("World bounds cannot contain NaN values.", nameof(worldBounds));
            }

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
            if (float.IsNaN(relativeBounds.min.x) || float.IsNaN(relativeBounds.min.y) || float.IsNaN(relativeBounds.min.z) ||
                float.IsNaN(relativeBounds.max.x) || float.IsNaN(relativeBounds.max.y) || float.IsNaN(relativeBounds.max.z))
            {
                throw new ArgumentException("Relative bounds cannot contain NaN values.", nameof(relativeBounds));
            }

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
            if (float.IsNaN(worldCenter.x) || float.IsNaN(worldCenter.y) || float.IsNaN(worldCenter.z))
                throw new ArgumentException("World center cannot contain NaN values.", nameof(worldCenter));
            if (radius < 0f)
                throw new ArgumentException("Radius must be non-negative.", nameof(radius));

            Bounds searchBounds = new Bounds(worldCenter, Vector3.one * radius * 2f);
            Vector3Int minKey = WorldToGrid(searchBounds.min);
            Vector3Int maxKey = WorldToGrid(searchBounds.max);
            float gridSize = (float)GridSize;
            Vector3 localCenter = worldCenter - Anchor;

            if (useEdgeDistance)
            {
                var state = new GridRadiusEdgeState(minKey, maxKey, localCenter, radius, gridSize, m_ActiveAxes);
                var iterator = new Iterator<Vector3Int, GridRadiusEdgeState>(state);
                while (iterator.MoveNext()) action.Execute(iterator.Current);
            }
            else
            {
                var state = new GridRadiusCentreState(minKey, maxKey, localCenter, radius, gridSize, m_ActiveAxes);
                var iterator = new Iterator<Vector3Int, GridRadiusCentreState>(state);
                while (iterator.MoveNext()) action.Execute(iterator.Current);
            }
        }

        /// <inheritdoc />
        public void ForEachKeyInRelativeRadius<TAction>(Vector3 relativeCenter, float radius, ref TAction action, bool useEdgeDistance = true)
            where TAction : struct, IExecutionHandler<Vector3Int>
        {
            if (float.IsNaN(relativeCenter.x) || float.IsNaN(relativeCenter.y) || float.IsNaN(relativeCenter.z))
                throw new ArgumentException("Relative center cannot contain NaN values.", nameof(relativeCenter));
            if (radius < 0f)
                throw new ArgumentException("Radius must be non-negative.", nameof(radius));

            ForEachKeyInRadius(relativeCenter + Anchor, radius, ref action, useEdgeDistance);
        }

        #endregion

        #region Factory & Public Access API

        /// <summary>
        /// Retrieves an existing chunk or creates a new one. 
        /// The provided handler is used to configure the chunk without heap allocations.
        /// </summary>
        public virtual bool GetOrCreateChunk<THandler>(Vector3Int key, ref THandler onConfigure, out T chunk)
            where THandler : struct, IExecutionHandler<T>
        {
            if (!IsInitialized)
                throw new InvalidOperationException("Registry is not initialized. Call Initialize() first.");

            Vector3Int validKey = MaskKey(key);

            var factory = new StatefulFuncHandler<EntryCreateData<Vector3Int>, ChunkRegistry<T>, T>(
                this,
                static (data, coord) =>
                {
                    T newChunk = data.gameObject.AddComponent<T>();
                    float half = (int)coord.GridSize * 0.5f;

                    ((IChunkControl)newChunk).Initialize(
                        data.key,
                        new Vector3(
                            coord.IsXActive ? half : 0,
                            coord.IsYActive ? half : 0,
                            coord.IsZActive ? half : 0)
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

            if (isNew)
            {
                onConfigure.Execute(chunk);
            }

            return isNew;
        }

        /// <summary>
        /// Converts a world-space position to a grid coordinate and ensures a chunk exists at that location.
        /// </summary>
        public bool GetOrCreateChunkAtWorldPos<THandler>(Vector3 pos, ref THandler onConfigure, out T chunk)
            where THandler : struct, IExecutionHandler<T>
            => GetOrCreateChunk(WorldToGrid(pos), ref onConfigure, out chunk);

        /// <summary>
        /// Maps a 2D grid coordinate to the active 3D axes of the volume and retrieves or creates the corresponding chunk.
        /// </summary>
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
        public bool TryGetChunk(Vector3Int key, out T chunk)
        {
            return TryGetEntry(key, out chunk);
        }

        /// <summary>
        /// Attempts to retrieve a chunk at a specific world position.
        /// </summary>
        public bool TryGetChunkAtWorldPos(Vector3 pos, out T chunk)
        {
            Vector3Int key = WorldToGrid(pos);
            return TryGetEntry(key, out chunk);
        }

        #endregion

        #region Coordinate Mapping Helpers

        /// <summary>
        /// Gets the world-space center of the cell for a specific grid key.
        /// </summary>
        public Vector3 GetCellCenter(Vector3Int key)
        {
            if (!IsInitialized)
                throw new InvalidOperationException("Registry is not initialized. Call Initialize() first.");

            return GridToWorld(key);
        }

        /// <summary>
        /// Gets the world-space center of the cell containing the specified world position.
        /// </summary>
        public Vector3 GetCellCenter(Vector3 worldPos)
        {
            if (!IsInitialized)
                throw new InvalidOperationException("Registry is not initialized. Call Initialize() first.");

            return GridToWorld(WorldToGrid(worldPos));
        }

        /// <summary>
        /// Gets the exact bounds (center and size) for a specific grid key.
        /// </summary>
        public Bounds GetCellBounds(Vector3Int key)
        {
            if (!IsInitialized)
                throw new InvalidOperationException("Registry is not initialized. Call Initialize() first.");

            return GetBoundsForKey(key);
        }

        /// <summary>
        /// Gets the exact bounds (center and size) of the cell containing the specified world position.
        /// </summary>
        public Bounds GetCellBounds(Vector3 worldPos)
        {
            if (!IsInitialized)
                throw new InvalidOperationException("Registry is not initialized. Call Initialize() first.");

            return GetBoundsForKey(WorldToGrid(worldPos));
        }

        /// <summary>
        /// Maps a local position (relative to Anchor) to a grid key.
        /// </summary>
        public Vector3Int LocalToGrid(Vector3 localPos)
        {
            if (!IsInitialized)
                throw new InvalidOperationException("Registry is not initialized. Call Initialize() first.");

            Vector3Int rawKey = SpatialUtils.PositionToKey3D(localPos, (int)GridSize, Vector3.zero);
            return MaskKey(rawKey);
        }

        #endregion

        #region Distance Metrics

        /// <summary>
        /// Calculates the squared distance from a world position to the closest edge of a grid cell defined by its key.
        /// </summary>
        /// <exception cref="InvalidOperationException">Thrown if the registry is not initialized.</exception>
        public float GetSqrDistanceToClosestEdge(Vector3Int key, Vector3 worldPos)
        {
            if (!IsInitialized)
                throw new InvalidOperationException("Registry is not initialized. Call Initialize() first.");

            return GetSqrDistanceToClosestEdge_Internal(worldPos, GridToWorld(key), (float)GridSize, IsXActive, IsYActive, IsZActive);
        }

        /// <summary>
        /// Calculates the squared distance from a world position to the closest edge of the cell containing targetPos.
        /// </summary>
        /// <exception cref="InvalidOperationException">Thrown if the registry is not initialized.</exception>
        public float GetSqrDistanceToClosestEdge(Vector3 targetPos, Vector3 worldPos)
        {
            if (!IsInitialized)
                throw new InvalidOperationException("Registry is not initialized. Call Initialize() first.");

            return GetSqrDistanceToClosestEdge(WorldToGrid(targetPos), worldPos);
        }

        /// <summary>
        /// Calculates the squared distance from a world position to the center of a grid cell defined by its key.
        /// </summary>
        /// <exception cref="InvalidOperationException">Thrown if the registry is not initialized.</exception>
        public float GetSqrDistanceToCenter(Vector3Int key, Vector3 worldPos)
        {
            if (!IsInitialized)
                throw new InvalidOperationException("Registry is not initialized. Call Initialize() first.");

            return GetSqrDistanceToCenter_Internal(worldPos, GridToWorld(key), IsXActive, IsYActive, IsZActive);
        }

        /// <summary>
        /// Calculates the squared distance from a world position to the center of the cell containing targetPos.
        /// </summary>
        /// <exception cref="InvalidOperationException">Thrown if the registry is not initialized.</exception>
        public float GetSqrDistanceToCenter(Vector3 targetPos, Vector3 worldPos)
        {
            if (!IsInitialized)
                throw new InvalidOperationException("Registry is not initialized. Call Initialize() first.");

            return GetSqrDistanceToCenter(WorldToGrid(targetPos), worldPos);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static float GetSqrDistanceToClosestEdge_Internal(Vector3 worldPos, Vector3 cellCenter, float gridSize, bool xActive, bool yActive, bool zActive)
        {
            float halfSize = gridSize * 0.5f;
            float sqrDist = 0;

            if (xActive) sqrDist += SpatialUtils.GetSqrDistanceToClosestEdge1D(worldPos.x, cellCenter.x, halfSize);
            if (yActive) sqrDist += SpatialUtils.GetSqrDistanceToClosestEdge1D(worldPos.y, cellCenter.y, halfSize);
            if (zActive) sqrDist += SpatialUtils.GetSqrDistanceToClosestEdge1D(worldPos.z, cellCenter.z, halfSize);

            return sqrDist;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static float GetSqrDistanceToCenter_Internal(Vector3 worldPos, Vector3 cellCenter, bool xActive, bool yActive, bool zActive)
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
        /// </summary>
        public void NotifyOriginShift(Vector3 delta)
        {
            if (!IsInitialized)
                throw new InvalidOperationException("Registry is not initialized. Call Initialize() first.");

            Anchor += delta;
        }

        #endregion

        #region Internal Mask Helpers

        private Vector3Int MaskKey(Vector3Int key)
        {
            if (m_ActiveAxes == 0) return key;

            return new Vector3Int(
                IsXActive ? key.x : 0,
                IsYActive ? key.y : 0,
                IsZActive ? key.z : 0
            );
        }

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