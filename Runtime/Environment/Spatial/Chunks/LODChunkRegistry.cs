using Rayforge.Core.Collections.Abstractions;
using Rayforge.Core.Collections.Iterator;
using Rayforge.Core.Environment.Abstractions;
using Rayforge.Core.Execution.Abstractions;
using System;
using UnityEngine;

namespace Rayforge.Core.Environment.Spatial.Chunks
{
    /// <summary>
    /// A high-performance registry that centralizes LOD logic for chunks.
    /// Chunks stay "dumb" while the registry dictates state changes based on distance, 
    /// automatically respecting ActiveAxes for 2D or 3D distance checks.
    /// </summary>
    /// <typeparam name="T">The chunk type implementing both spatial and LOD interfaces.</typeparam>
    public class LODChunkRegistry<T> : ChunkRegistry<T>, ILODGridProvider<Vector3Int>
        where T : LODChunk<T>
    {
        #region Private Structs

        private struct CountHandler : IExecutionHandler<Vector3Int>
        {
            public int Count;

            public void Execute(Vector3Int value)
            {
                Count++;
            }
        }

        #endregion

        #region Fields & Config

        private float[] m_LodSqrDistances;
        private float[] m_LodDistances;
        private bool m_DeactivateOnCulled;

        private Transform m_Viewer;

        /// <summary>
        /// Gets or sets the current world-space position reference of the player or camera focus.
        /// </summary>
        public Transform Viewer
        {
            get => m_Viewer;
            set
            {
                if (value == null)
                    throw new ArgumentNullException(nameof(value), "Viewer cannot be null.");

                if (m_Viewer != value)
                {
                    m_Viewer = value;
                }
            }
        }

        /// <summary> Implementation of ILODGridProvider. Returns current viewer position. </summary>
        public Vector3 ViewerPos => (Viewer != null) ? Viewer.position : Vector3.zero;

        /// <summary>
        /// If true, chunks exceeding the maximum LOD distance threshold are automatically deactivated.
        /// Use this to save performance by disabling GameObjects that are too far away to be visible.
        /// </summary>
        public bool DeactivateOnCulled => m_DeactivateOnCulled;

        /// <summary> 
        /// High-performance access to the squared thresholds. 
        /// Avoids array copying and heap allocations.
        /// </summary>
        public ReadOnlySpan<float> LodSqrDistances => m_LodSqrDistances;

        /// <summary>
        /// High-performance access to the thresholds. 
        /// Avoids array copying and heap allocations.
        /// </summary>
        public ReadOnlySpan<float> LodDistances => m_LodDistances;

        /// <summary> Implementation of ILODGridProvider. Returns number of LOD levels. </summary>
        public int LodCount => m_LodSqrDistances?.Length ?? 0;

        /// <summary> 
        /// Returns the number of chunks that are currently within a valid LOD range (LOD >= 0).
        /// </summary>
        public int ActiveCellCount => _activeCellCountCache;
        private int _activeCellCountCache = 0;

        #endregion

        #region Events

        /// <summary> Implementation of ILODGridProvider. </summary>
        public event Action<ILODGridConfiguration<Vector3Int>> OnLODSettingsChanged;

        #endregion

        #region Lifecycle

        /// <summary>
        /// Default constructor. Does not allocate LOD arrays yet.
        /// </summary>
        public LODChunkRegistry() : base() { }

        /// <summary>
        /// Initializes the LOD registry.
        /// Combines spatial setup with LOD threshold configuration.
        /// </summary>
        public void Initialize(
            GridSize gridSize,
            Vector3 anchor,
            ReadOnlySpan<float> lodDistances,
            Transform viewer,
            bool deactivateOnCulled = true,
            Transform parent = null,
            string name = "LODChunkRegistry")
        {
            if (viewer == null)
            {
                throw new ArgumentNullException(nameof(viewer), "Viewer transform is required for LOD calculations and cannot be null.");
            }

            if (lodDistances.IsEmpty)
            {
                throw new ArgumentException("LOD distances cannot be empty. At least one LOD level must be defined.", nameof(lodDistances));
            }

            if (lodDistances[0] <= 0f)
            {
                throw new ArgumentException("The first LOD distance must be greater than zero.", nameof(lodDistances));
            }

            base.Initialize(gridSize, anchor, parent, name);

            m_DeactivateOnCulled = deactivateOnCulled;
            m_Viewer = viewer;

            UpdateLodDistances(lodDistances);
        }

        /// <summary>
        /// Updates the internal squared distance thresholds.
        /// </summary>
        public bool UpdateLodDistances(ReadOnlySpan<float> newDistances)
        {
            if (!IsInitialized)
                throw new InvalidOperationException("Cannot update LOD distances on an uninitialized registry.");

            if (newDistances.IsEmpty)
                throw new ArgumentException("Cannot apply an empty LOD configuration.", nameof(newDistances));

            if (m_LodDistances != null && m_LodDistances.Length == newDistances.Length)
            {
                bool changed = false;
                for (int i = 0; i < newDistances.Length; i++)
                {
                    if (!Mathf.Approximately(m_LodDistances[i], newDistances[i]))
                    {
                        changed = true;
                        break;
                    }
                }
                if (!changed) return false;
            }

            int count = newDistances.Length;

            float[] newLinearDistances = new float[count];
            float[] newSqrDistances = new float[count];

            for (int i = 0; i < count; i++)
            {
                float d = newDistances[i];

                if (d <= 0f)
                    throw new ArgumentException($"LOD distance at index {i} ({d}) must be greater than zero.", nameof(newDistances));

                if (i > 0 && d <= newLinearDistances[i - 1])
                    throw new ArgumentException($"LOD distance at index {i} ({d}) must be strictly greater than the previous index ({newLinearDistances[i - 1]}).", nameof(newDistances));

                newLinearDistances[i] = d;
                newSqrDistances[i] = d * d;
            }

            m_LodDistances = newLinearDistances;
            m_LodSqrDistances = newSqrDistances;

            OnLODSettingsChanged?.Invoke(this);

            return true;
        }

        #endregion

        #region Helpers

        /// <summary>
        /// Calculates a radius extent vector that zeroes out inactive axes to prevent Bounding Box expansion on unused axes.
        /// </summary>
        private Vector3 GetActiveRadiusExtent(float radius)
        {
            return new Vector3(
                IsXActive ? radius : 0f,
                IsYActive ? radius : 0f,
                IsZActive ? radius : 0f
            );
        }

        #endregion

        #region Factory Overrides

        public override bool GetOrCreateChunk<THandler>(Vector3Int key, ref THandler onConfigure, out T chunk)
        {
            if (!IsInitialized)
                throw new InvalidOperationException("Cannot create or retrieve chunks on an uninitialized registry.");

            bool isNew = base.GetOrCreateChunk(key, ref onConfigure, out chunk);

            if (isNew)
            {
                ((ILODReceiver)chunk).ConfigureLODRange(m_LodDistances.Length - 1);

                chunk.OnLODChanged += HandleChunkLODChanged;
                chunk.OnCleanup += HandleChunkDestroyed;

                UpdateChunkLOD(chunk, ViewerPos);
            }

            return isNew;
        }

        private void HandleChunkLODChanged(ILODState sender, int oldLod, int newLod)
        {
            bool wasActive = oldLod >= 0;
            bool isActive = newLod >= 0;

            if (!wasActive && isActive) _activeCellCountCache++;
            else if (wasActive && !isActive) _activeCellCountCache--;
        }

        private void HandleChunkDestroyed(T chunk)
        {
            chunk.OnLODChanged -= HandleChunkLODChanged;
            chunk.OnCleanup -= HandleChunkDestroyed;

            if (chunk.CurrentLOD >= 0)
            {
                _activeCellCountCache--;
            }
        }

        #endregion

        #region ILODGridQuery Implementation

        /// <inheritdoc />
        public int CalculateTargetLODSqr(float sqrDistance)
        {
            if (!IsInitialized)
                throw new InvalidOperationException("Call Initialize() first.");

            ReadOnlySpan<float> thresholds = LodSqrDistances;
            for (int i = 0; i < thresholds.Length; i++)
            {
                if (sqrDistance < thresholds[i]) return i;
            }
            return -1;
        }

        /// <inheritdoc />
        public int CalculateTargetLOD(float distance)
        {
            if (!IsInitialized)
                throw new InvalidOperationException("Call Initialize() first.");

            ReadOnlySpan<float> thresholds = LodDistances;
            for (int i = 0; i < thresholds.Length; i++)
            {
                if (distance < thresholds[i]) return i;
            }
            return -1;
        }

        /// <inheritdoc />
        public IIterator<Vector3Int> GetKeysInLOD(int lodIndex, Vector3 center)
        {
            if (!IsInitialized)
                throw new InvalidOperationException("Call Initialize() first.");

            if (lodIndex < 0 || lodIndex >= LodCount)
                return IIterator<Vector3Int>.Empty();

            float minSqrRadius = lodIndex == 0 ? 0f : LodSqrDistances[lodIndex - 1];
            float maxSqrRadius = LodSqrDistances[lodIndex];

            float outerRadius = LodDistances[lodIndex];
            Vector3 radiusExtent = GetActiveRadiusExtent(outerRadius);
            Vector3Int minKey = WorldToGrid(center - radiusExtent);
            Vector3Int maxKey = WorldToGrid(center + radiusExtent);
            float gridSize = (float)GridSize;

            var state = new GridLODEdgeState(
                minKey,
                maxKey,
                center,
                minSqrRadius,
                maxSqrRadius,
                new Vector3(gridSize, gridSize, gridSize),
                ActiveAxes
            );

            return new Iterator<Vector3Int, GridLODEdgeState>(state);
        }

        /// <inheritdoc />
        public IIterator<Vector3Int> GetKeysInFullRange(Vector3 center)
        {
            if (!IsInitialized)
                throw new InvalidOperationException("Call Initialize() first.");

            if (LodCount == 0)
                return IIterator<Vector3Int>.Empty();

            float maxRadius = LodDistances[LodCount - 1];
            Vector3 radiusExtent = GetActiveRadiusExtent(maxRadius);
            Vector3Int minKey = WorldToGrid(center - radiusExtent);
            Vector3Int maxKey = WorldToGrid(center + radiusExtent);
            float gridSize = (float)GridSize;

            var state = new GridLODEdgeState(
                minKey,
                maxKey,
                center,
                0f,
                LodSqrDistances[LodCount - 1],
                new Vector3(gridSize, gridSize, gridSize),
                ActiveAxes
            );

            return new Iterator<Vector3Int, GridLODEdgeState>(state);
        }

        /// <inheritdoc />
        public void ForEachKeyInLOD<TAction>(int lodIndex, Vector3 center, ref TAction action)
            where TAction : struct, IExecutionHandler<Vector3Int>
        {
            if (!IsInitialized)
                throw new InvalidOperationException("Call Initialize() first.");

            if (lodIndex < 0 || lodIndex >= LodCount)
                return;

            float minSqrRadius = lodIndex == 0 ? 0f : LodSqrDistances[lodIndex - 1];
            float maxSqrRadius = LodSqrDistances[lodIndex];

            float outerRadius = LodDistances[lodIndex];
            Vector3 radiusExtent = GetActiveRadiusExtent(outerRadius);
            Vector3Int minKey = WorldToGrid(center - radiusExtent);
            Vector3Int maxKey = WorldToGrid(center + radiusExtent);
            float gridSize = (float)GridSize;

            var state = new GridLODEdgeState(
                minKey,
                maxKey,
                center,
                minSqrRadius,
                maxSqrRadius,
                new Vector3(gridSize, gridSize, gridSize),
                ActiveAxes
            );

            while (state.MoveNext(ref state, out Vector3Int key))
            {
                action.Execute(key);
            }
        }

        /// <inheritdoc />
        public void ForEachKeyInFullRange<TAction>(Vector3 center, ref TAction action)
            where TAction : struct, IExecutionHandler<Vector3Int>
        {
            if (!IsInitialized)
                throw new InvalidOperationException("Call Initialize() first.");

            if (LodCount == 0) return;

            float maxRadius = LodDistances[LodCount - 1];
            Vector3 radiusExtent = GetActiveRadiusExtent(maxRadius);
            Vector3Int minKey = WorldToGrid(center - radiusExtent);
            Vector3Int maxKey = WorldToGrid(center + radiusExtent);
            float gridSize = (float)GridSize;

            var state = new GridLODEdgeState(
                minKey,
                maxKey,
                center,
                0f,
                LodSqrDistances[LodCount - 1],
                new Vector3(gridSize, gridSize, gridSize),
                ActiveAxes
            );

            while (state.MoveNext(ref state, out Vector3Int key))
            {
                action.Execute(key);
            }
        }

        #endregion

        #region ILODGridMetrics Implementation

        /// <inheritdoc />
        public int GetKeyCountInLODLevel(int lodIndex, Vector3 center)
        {
            if (!IsInitialized)
                throw new InvalidOperationException("Call Initialize() first.");

            if (lodIndex < 0 || lodIndex >= LodCount) return 0;

            var handler = new CountHandler();
            ForEachKeyInLOD(lodIndex, center, ref handler);
            return handler.Count;
        }

        /// <inheritdoc />
        public int GetKeyCountInFullRange(Vector3 center)
        {
            if (!IsInitialized)
                throw new InvalidOperationException("Call Initialize() first.");

            if (LodCount == 0) return 0;

            var handler = new CountHandler();
            ForEachKeyInFullRange(center, ref handler);
            return handler.Count;
        }

        /// <inheritdoc />
        public int GetMaxCapacityForLODLevel(int lodIndex)
        {
            if (!IsInitialized)
                throw new InvalidOperationException("Call Initialize() first.");

            if (lodIndex < 0 || lodIndex >= LodCount) return 0;

            float outerRadius = LodDistances[lodIndex];
            float size = (float)GridSize;

            int outerMaxAxis = Mathf.CeilToInt((outerRadius * 2f) / size) + 1;
            int outerMaxCount = CalculateCountForActiveAxes(outerMaxAxis);

            int innerMinCount = 0;
            if (lodIndex > 0)
            {
                float innerRadius = LodDistances[lodIndex - 1];
                int innerMinAxis = Mathf.Max(0, Mathf.FloorToInt((innerRadius * 2f) / size) - 1);
                innerMinCount = CalculateCountForActiveAxes(innerMinAxis);
            }

            return Mathf.Max(0, outerMaxCount - innerMinCount);
        }

        /// <inheritdoc />
        private int CalculateCountForActiveAxes(int axisCount)
        {
            if (!IsInitialized)
                throw new InvalidOperationException("Call Initialize() first.");

            if (axisCount <= 0) return 0;

            int total = 1;
            bool anyActive = false;

            if (IsXActive) { total *= axisCount; anyActive = true; }
            if (IsYActive) { total *= axisCount; anyActive = true; }
            if (IsZActive) { total *= axisCount; anyActive = true; }

            return anyActive ? total : 0;
        }

        #endregion

        #region Core LOD Logic

        public int UpdateLODs() => UpdateLODs(ViewerPos);

        public int UpdateLODs(Vector3 focusPos)
        {
            if (!IsInitialized)
                throw new InvalidOperationException("Call Initialize() first.");

            int changeCount = 0;
            foreach (T chunk in AllEntries)
            {
                if (chunk != null && UpdateChunkLOD(chunk, focusPos))
                {
                    changeCount++;
                }
            }
            return changeCount;
        }

        private bool UpdateChunkLOD(T chunk, Vector3 pos)
        {
            float sqrDist = GetSqrDistanceToClosestEdge(chunk.GridKey, pos);
            int targetLod = CalculateTargetLODSqr(sqrDist);
            return ((ILODReceiver)chunk).UpdateLOD(targetLod, m_DeactivateOnCulled);
        }

        #endregion
    }
}