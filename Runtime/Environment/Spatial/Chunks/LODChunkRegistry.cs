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
        #region Fields & Config

        private float[] _lodSqrDistances;
        private float[] _lodDistances;
        private bool _deactivateOnCulled;

        public Transform Viewer { get; private set; }

        /// <summary>
        /// If true, chunks exceeding the maximum LOD distance threshold are automatically deactivated.
        /// Use this to save performance by disabling GameObjects that are too far away to be visible.
        /// </summary>
        public bool DeactivateOnCulled => _deactivateOnCulled;

        /// <summary> 
        /// High-performance access to the squared thresholds. 
        /// Avoids array copying and heap allocations.
        /// </summary>
        public ReadOnlySpan<float> LodSqrDistances => _lodSqrDistances;

        /// <summary>
        /// High-performance access to the thresholds. 
        /// Avoids array copying and heap allocations.
        /// </summary>
        public ReadOnlySpan<float> LodDistances => _lodDistances;

        /// <summary> Implementation of ILODGridProvider. Returns current viewer position. </summary>
        public Vector3 ViewerPos => (Viewer != null) ? Viewer.position : Vector3.zero;

        /// <summary> Implementation of ILODGridProvider. Returns number of LOD levels. </summary>
        public int LodCount => _lodSqrDistances?.Length ?? 0;

        /// <summary> Implementation of ILODGridProvider. </summary>
        public event Action<ILODGridProvider<Vector3Int>> OnLODSettingsChanged;

        /// <summary> 
        /// Returns the number of chunks that are currently within a valid LOD range (LOD >= 0).
        /// </summary>
        public int ActiveCellCount => _activeCellCountCache;
        private int _activeCellCountCache = 0;

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
        public void Initialize(SpatialSettings spatialSettings, LodSettings lodSettings, Transform viewer = null, Transform container = null)
        {
            try
            {
                base.Initialize(spatialSettings, container, "LODChunkRegistry");

                if (lodSettings.LodDistances == null || lodSettings.LodDistances.Length == 0)
                    throw new ArgumentException("LOD distances are missing in configuration.");

                _deactivateOnCulled = lodSettings.DeactivateOnCulled;
                Viewer = viewer;

                ApplyLodConfiguration(lodSettings.LodDistances);
            }
            catch (Exception e)
            {
                throw new Exception($"{Tag} Initialization failed: {e.Message}", e);
            }
        }

        /// <summary>
        /// Updates the internal squared distance thresholds.
        /// Re-calculates squared values to keep the Update loop math simple and fast.
        /// </summary>
        public bool UpdateLodDistances(ReadOnlySpan<float> newDistances)
            => ApplyLodConfiguration(newDistances);

        /// <summary> Updates the viewer reference (e.g., when switching cameras). </summary>
        public bool SetViewer(Transform viewer)
        {
            if (Viewer != viewer)
            {
                Viewer = viewer;
                return true;
            }
            return false;
        }

        /// <summary>
        /// The single source of truth for changing LOD arrays.
        /// Handles validation, allocation, and notification.
        /// </summary>
        private bool ApplyLodConfiguration(ReadOnlySpan<float> newDistances)
        {
            if (newDistances.Length == 0)
                throw new ArgumentException("Cannot apply an empty LOD configuration.");

            if (_lodDistances != null && _lodDistances.Length == newDistances.Length)
            {
                bool changed = false;
                for (int i = 0; i < newDistances.Length; i++)
                {
                    if (!Mathf.Approximately(_lodDistances[i], newDistances[i]))
                    {
                        changed = true;
                        break;
                    }
                }
                if (!changed) return false;
            }

            int count = newDistances.Length;
            _lodDistances = new float[count];
            _lodSqrDistances = new float[count];

            for (int i = 0; i < count; i++)
            {
                float d = newDistances[i];
                if (i > 0 && d <= _lodDistances[i - 1])
                    throw new InvalidOperationException($"LOD Distance at index {i} ({d}) must be greater than index {i - 1} ({_lodDistances[i - 1]}).");

                _lodDistances[i] = d;
                _lodSqrDistances[i] = d * d;
            }

            OnLODSettingsChanged?.Invoke(this);

            return true;
        }

        #endregion

        #region Factory Overrides

        /// <summary>
        /// Overrides the base factory using the <see cref="IExecutionHandler{T}"/> pattern.
        /// </summary>
        /// <typeparam name="THandler">The struct handler used to configure the chunk.</typeparam>
        /// <param name="key">The 3D grid coordinate for the chunk.</param>
        /// <param name="onConfigure">A struct handler containing the state and logic for chunk setup.</param>
        /// <param name="chunk">When this method returns, contains the initialized and LOD-configured chunk instance.</param>
        /// <returns>True if a brand new chunk was created; otherwise, false.</returns>
        public override bool GetOrCreateChunk<THandler>(Vector3Int key, ref THandler onConfigure, out T chunk)
        {
            bool isNew = base.GetOrCreateChunk(key, ref onConfigure, out chunk);

            if (isNew)
            {
                ((ILODReceiver)chunk).ConfigureLODRange(_lodDistances.Length - 1);

                chunk.OnLODChanged += HandleChunkLODChanged;
                chunk.OnCleanup += HandleChunkDestroyed;

                UpdateChunkLOD(chunk, ViewerPos);
            }

            return isNew;
        }

        /// <summary>
        /// Reacts to individual chunk LOD changes to keep the global ActiveCellCount in sync.
        /// </summary>
        private void HandleChunkLODChanged(ILODState sender, int oldLod, int newLod)
        {
            bool wasActive = oldLod >= 0;
            bool isActive = newLod >= 0;

            if (!wasActive && isActive) _activeCellCountCache++;
            else if (wasActive && !isActive) _activeCellCountCache--;
        }

        /// <summary>
        /// Ensures the active count is decremented if an active chunk is destroyed.
        /// </summary>
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

        #region ILODGridProvider Implementation

        /// <inheritdoc />
        public int CalculateTargetLOD(float sqrDistance)
        {
            ReadOnlySpan<float> thresholds = LodSqrDistances;
            for (int i = 0; i < thresholds.Length; i++)
            {
                if (sqrDistance < thresholds[i]) return i;
            }
            return -1;
        }

        /// <inheritdoc />
        public IIterator<Vector3Int> GetKeysInLODLevel(int lodIndex, Vector3 center)
        {
            if (lodIndex < 0 || lodIndex >= LodCount)
                return IIterator<Vector3Int>.Empty();

            float outerRadius = LodDistances[lodIndex];

            Bounds searchBounds = new Bounds(center, Vector3.one * outerRadius * 2f);
            Vector3Int minKey = WorldToGrid(searchBounds.min);
            Vector3Int maxKey = WorldToGrid(searchBounds.max);

            var rangeState = new GridRangeState(minKey, maxKey);

            var lodState = new GridLodState(
                rangeState,
                lodIndex,
                center,
                outerRadius,
                _lodSqrDistances,
                new Vector3((int)GridSize, (int)GridSize, (int)GridSize),
                ActiveAxes
            );

            return new Iterator<Vector3Int, GridLodState>(lodState);
        }

        /// <inheritdoc />
        public int GetKeyCountInLODLevel(int lodIndex, Vector3 center)
        {
            if (lodIndex < 0 || lodIndex >= LodCount) return 0;

            float outerRadius = LodDistances[lodIndex];
            Bounds searchBounds = new Bounds(center, Vector3.one * outerRadius * 2f);
            int count = 0;

            foreach (var key in GetKeysInBounds(searchBounds))
            {
                float sqrDist = GetSqrDistanceToClosestEdge(key, center);
                if (CalculateTargetLOD(sqrDist) == lodIndex)
                {
                    count++;
                }
            }
            return count;
        }

        /// <inheritdoc />
        public void ForEachKeyInLOD<TAction>(int lodIndex, Vector3 center, ref TAction action)
            where TAction : struct, IExecutionHandler<Vector3Int>
        {
            if (lodIndex < 0 || lodIndex >= LodCount) return;

            float outerRadius = LodDistances[lodIndex];
            Bounds searchBounds = new Bounds(center, Vector3.one * outerRadius * 2f);

            foreach (var key in GetKeysInBounds(searchBounds))
            {
                float sqrDist = GetSqrDistanceToClosestEdge(key, center);

                if (CalculateTargetLOD(sqrDist) == lodIndex)
                {
                    action.Execute(key);
                }
            }
        }

        /// <inheritdoc />
        public IIterator<Vector3Int> GetKeysInFullRange(Vector3 center)
        {
            if (LodCount == 0)
                return IIterator<Vector3Int>.Empty();

            float maxRadius = LodDistances[LodCount - 1];
            return GetKeysInRadius(center, maxRadius, useEdgeDistance: true);
        }

        /// <inheritdoc />
        public void ForEachKeyInRange<TAction>(Vector3 center, ref TAction action)
            where TAction : struct, IExecutionHandler<Vector3Int>
        {
            if (LodCount == 0) return;

            float maxRadius = LodDistances[LodCount - 1];

            foreach (var key in GetKeysInRadius(center, maxRadius, useEdgeDistance: true))
            {
                action.Execute(key);
            }
        }

        /// <inheritdoc />
        public int GetKeyCountInFullRange(Vector3 center)
        {
            if (LodCount == 0) return 0;

            float maxRadius = LodDistances[LodCount - 1];
            int count = 0;

            foreach (var _ in GetKeysInRadius(center, maxRadius, useEdgeDistance: true))
            {
                count++;
            }

            return count;
        }

        /// <inheritdoc />
        public int GetMaxCapacityForLODLevel(int lodIndex)
        {
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

        /// <summary>
        /// Helper to calculate cell counts based on which axes are currently active.
        /// Handles 1D, 2D, and 3D configurations automatically.
        /// </summary>
        private int CalculateCountForActiveAxes(int axisCount)
        {
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
            int targetLod = CalculateTargetLOD(sqrDist);
            return ((ILODReceiver)chunk).UpdateLOD(targetLod, _deactivateOnCulled);
        }

        #endregion
    }
}
