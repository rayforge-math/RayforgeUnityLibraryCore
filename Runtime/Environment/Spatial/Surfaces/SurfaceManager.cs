using Rayforge.Core.Common.Rendering;
using Rayforge.Core.Common.Rendering.Helpers;
using Rayforge.Core.Diagnostics;
using Rayforge.Core.Environment.Abstractions;
using Rayforge.Core.Environment.Spatial.Surfaces;
using Rayforge.Core.ManagedResources.NativeMemory;
using Rayforge.Core.ManagedResources.Pooling;
using Rayforge.Core.Rendering.Helpers;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using UnityEngine;
using UnityEngine.UIElements;

namespace Rayforge.Core.Environment.Spatial.Surface
{
    /// <summary>
    /// Scans the scene hierarchy for valid world objects and synchronizes them with the SurfaceRegistry.
    /// Handles filtering by name and physical area to ensure only relevant surfaces are processed.
    /// </summary>
    public class SurfaceManager : MonoBehaviour
    {
        #region Nested Types
        [System.Serializable]
        public struct SurfaceLODLevel
        {
            [Tooltip("Distance threshold for this level.")]
            public float distanceThreshold;
            [Tooltip("Edge resolution for the heightmap.")]
            public PowerOfTwoResolution mapResolution;
        }
        #endregion

        #region Serialized Fields: Configuration
        [Header("Debug & Diagnostics")]
        public bool showDebugLogs = false;

        [Header("Floating Origin")]
        [Tooltip("Monitors world movement to handle coordinate shifts.")]
        public OriginShiftRelay shiftRelay;

        [Header("LOD & Culling Settings")]
        [Tooltip("The reference point for LOD calculations (usually Main Camera).")]
        public Transform lodReference;

        [Tooltip("The physical size of a single chunk in meters.")]
        public GridSizeBinary chunkSize = GridSizeBinary.Huge;

        [Tooltip("Movement threshold (% of chunk size) before triggering updates.")]
        [Range(0.01f, 0.5f)]
        public float updateSensitivity = 0.1f;

        [Tooltip("Define LOD levels (Distances and Resolutions).")]
        public SurfaceLODLevel[] lodLevels;

        [Header("Surface Detection Settings")]
        [Tooltip("If enabled, the manager automatically scans all children of this GameObject.")]
        public bool scanHierarchy = true;
        [Tooltip("If not empty, only objects containing this string in their name are considered.")]
        public string nameFilter = "";
        [Space(5)]
        [Tooltip("If enabled, objects must have a minimum physical size to be accepted.")]
        public bool enableAreaCheck = true;
        [Tooltip("Minimum XZ-Area in square meters (e.g., 1.0 for a 1x1m area).")]
        public float minAreaThreshold = 1.0f;
        [Tooltip("If true, RebuildRegistry() is called automatically on Start."), HideInInspector]
        public bool autoUpdate = false;

        [Header("Surfaces")]
        [Tooltip("Manual list of surfaces. If Auto Detect is enabled, this list is populated automatically.")]
        public List<GameObject> surfaces = new List<GameObject>();
        #endregion

        #region Private Runtime State
        private readonly HashSet<int> _surfaceIds = new HashSet<int>();
        private readonly List<int> _cleanupBuffer = new List<int>(32);

        private LODChunkRegistry<SurfaceChunk> _chunkRegistry;
        private SpatialObjectRegistry _registry;
        private Vector3 _lastUpdatePos;
        private bool _needsSpatialSync = false;

        SurfaceLODLevel[] _validLodLevels = Array.Empty<SurfaceLODLevel>();

        private static readonly BufferCreateFunc<RenderTextureDescriptorWrapper, ManagedRenderTexture> s_Create =
            _ => ManagedRenderTexture.Create(_, FilterMode.Point, TextureWrapMode.Clamp);
        private readonly ManagedRenderTexturePool _texturePool = new ManagedRenderTexturePool(s_Create);

        private readonly Dictionary<Vector3Int, LeasedBuffer<ManagedRenderTexture>> _leasedBuffers = new Dictionary<Vector3Int, LeasedBuffer<ManagedRenderTexture>>();
        private readonly HashSet<Vector3Int> _toRelease = new HashSet<Vector3Int>();
        private readonly HashSet<Vector3Int> _toAssign = new HashSet<Vector3Int>();
        private bool _needsBufferSync = false;

        private readonly HashSet<Vector3Int> _chunksPendingBake = new HashSet<Vector3Int>();

        private bool IsReady => lodReference != null && _chunkRegistry != null && _registry != null;
        #endregion

        #region Unity Lifecycle
        private void Awake()
        {
            SetupDependencies();
            SanitizeLODLevels();
            CacheValidLodLevels();

            EnsureSystemsReady(true);

            if (shiftRelay != null)
            {
                shiftRelay.OnWorldShiftDetected -= HandleOriginShift;
                shiftRelay.OnWorldShiftDetected += HandleOriginShift;
            }

            //_texturePool.showDebugLogs = true;
        }

        private void Start()
        {
            RebuildRegistry();
        }

        private void Update()
        {
            if (!IsReady) return;

            if (_needsSpatialSync)
            {
                LogDebug("Update: Spatial sync triggered by registry changes.");
                SynchronizeChunksWithRegistry();
                _needsSpatialSync = false;
            }

            if (CheckMovementThreshold())
            {
                LogDebug($"Update: LOD refresh triggered by movement.");
                _chunkRegistry.UpdateLODs();
            }

            if (_needsBufferSync)
            {
                LogDebug("Update: Heightmap updates triggered by registry changes.");
                UdpateChunkHandles();
                _needsBufferSync = false;
            }

            if (_chunksPendingBake.Count > 0)
            {
                ProcessBaking();
            }
        }

        private void OnDestroy()
        {
            if (shiftRelay != null)
                shiftRelay.OnWorldShiftDetected -= HandleOriginShift;

            _chunkRegistry?.Dispose();
            _registry?.Clear();

            _chunkRegistry = null;
            _registry = null;

            _texturePool?.Dispose();
        }

        private void OnValidate()
        {
            if (!enabled || !gameObject.activeInHierarchy) return;

            SetupDependencies();
            SanitizeLODLevels();
            CacheValidLodLevels();

            UpdateGridSize();
            UpdateLODSettings();
        }
        #endregion

        #region Initialization & Dependencies
        private void EnsureSystemsReady(bool force = false)
        {
            CreateObjectRegistry(force);
            CreateChunkRegistry(force);
            ResetTrackingPosition();
        }

        private void CreateObjectRegistry(bool force = false)
        {
            if (_registry == null || force)
            {
                _registry = new SpatialObjectRegistry();
                //_registry.showDebugLogs = showDebugLogs;
                LogDebug("Spatial Object Registry initialized.");
            }
        }

        private void CreateChunkRegistry(bool force = false)
        {
            if (_chunkRegistry == null || force)
            {
                _chunkRegistry?.Dispose();

                _chunkRegistry = new LODChunkRegistry<SurfaceChunk>(
                    (GridSize)chunkSize,
                    transform.position,
                    GetValidLodDistances(),
                    true,
                    lodReference,
                    this.transform
                );
                //_chunkRegistry.showDebugLogs = showDebugLogs;

                _registry?.Initialize(_chunkRegistry);
                LogDebug($"Chunk Registry created. GridSize: {(int)chunkSize}m");
            }
        }

        public void SetupDependencies()
        {
            if (shiftRelay == null)
                shiftRelay = GetComponentInParent<OriginShiftRelay>(true);

            if (lodReference == null && Camera.main != null)
                lodReference = Camera.main.transform;
        }

        #endregion

        #region Configuration & Validation

        private void UpdateGridSize()
        {
            if (_chunkRegistry == null) return;

            if (_chunkRegistry.GridSize != (GridSize)chunkSize)
            {
                _chunkRegistry.SetGridSize((GridSize)chunkSize);
                _registry?.Initialize(_chunkRegistry);
                _needsSpatialSync = true;

                LogDebug("Grid Settings synchronized.");
            }
            else
            {
                LogDebug("Grid size update skipped: Size is already identical.");
            }
        }

        private void UpdateLODSettings()
        {
            if (_chunkRegistry == null) return;

            float[] distances = GetValidLodDistances();

            bool updateLod = _chunkRegistry.SetViewer(lodReference);
            updateLod |= _chunkRegistry.UpdateLodDistances(distances);

            if (updateLod)
            {
                _chunkRegistry.UpdateLODs();
                LogDebug("LOD Settings synchronized and LODs recalculated.");
            }
            else
            {
                LogDebug("LOD update skipped: Viewer and distances have not changed.");
            }
        }

        private void SanitizeLODLevels()
        {
            if (lodLevels == null || lodLevels.Length == 0) return;

            bool invalid = false;

            for (int i = 0; i < lodLevels.Length; i++)
            {
                var current = lodLevels[i];

                if (i > 0)
                {
                    var prev = lodLevels[i - 1];

                    if (current.distanceThreshold <= prev.distanceThreshold)
                        current.distanceThreshold = prev.distanceThreshold + 10.0f;

                    if (!current.mapResolution.IsLowerThan(prev.mapResolution))
                        current.mapResolution = prev.mapResolution.Downscale();

                    if (current.mapResolution == prev.mapResolution)
                    {
                        if (!invalid)
                        {
                            LogDebug($"LOD Chain reached resolution limit at index {i}. " +
                                     "Subsequent levels will be clamped and filtered out during bake.");
                            invalid = true;
                        }

                        current.mapResolution = prev.mapResolution;
                        current.distanceThreshold = prev.distanceThreshold;
                    }
                }
                else
                {
                    if (current.distanceThreshold == 0) current.distanceThreshold = 50f;
                    if (current.mapResolution.IsLowerThan(PowerOfTwoResolution.Resolution32))
                        current.mapResolution = PowerOfTwoResolution.Resolution256;
                }

                lodLevels[i] = current;
            }
        }

        private float[] GetValidLodDistances()
        {
            var distances = new float[_validLodLevels.Length];
            for (int i = 0; i < _validLodLevels.Length; ++i)
            {
                distances[i] = _validLodLevels[i].distanceThreshold;
            }

            return distances;
        }

        private void CacheValidLodLevels()
        {
            if (lodLevels == null || lodLevels.Length == 0)
            {
                LogDebug("GetValidLodLevels: No LOD levels defined.");
                _validLodLevels = new SurfaceLODLevel[0];
                return;
            }

            List<SurfaceLODLevel> validLevels = new List<SurfaceLODLevel>(capacity: lodLevels.Length);
            SurfaceLODLevel lastAccepted = default;

            for (int i = 0; i < lodLevels.Length; i++)
            {
                var cur = lodLevels[i];

                if (i == 0)
                {
                    validLevels.Add(cur);
                    lastAccepted = cur;
                    continue;
                }

                bool resDrops = lastAccepted.mapResolution.IsHigherThan(cur.mapResolution);
                bool distGrows = cur.distanceThreshold > lastAccepted.distanceThreshold;

                if (resDrops && distGrows)
                {
                    validLevels.Add(cur);
                    lastAccepted = cur;
                }
                else
                {
                    string reason = "";
                    if (!resDrops) reason += $"Resolution {cur.mapResolution} is not lower than {lastAccepted.mapResolution}. ";
                    if (!distGrows) reason += $"Distance {cur.distanceThreshold}m is not further than {lastAccepted.distanceThreshold}m.";

                    LogDebug($"LOD Level [{i}] ignored: {reason}");
                }
            }

            LogDebug($"LOD Validation complete: {validLevels.Count} valid levels extracted from {lodLevels.Length} entries.");
            _validLodLevels = validLevels.ToArray();
        }

        #endregion

        #region Surface Tracking & Hierarchy Scanning

        public void RebuildRegistry()
        {
            LogDebug("Rebuilding Registry...");
            _registry.Clear();
            SyncFromList();
            if (scanHierarchy) ScanHierarchyRecursive(transform);
        }

        public void SyncFromList()
        {
            _surfaceIds.Clear();

            for (int i = surfaces.Count - 1; i >= 0; i--)
            {
                GameObject obj = surfaces[i];
                if (obj == null) { surfaces.RemoveAt(i); continue; }

                if (!IsValidCandidate(obj.transform))
                {
                    RemoveSurface(obj.GetInstanceID());
                    continue;
                }

                TryAddSurface(obj);
            }

            _cleanupBuffer.Clear();
            foreach (int id in _registry.GetAllIds())
            {
                if (!_surfaceIds.Contains(id)) _cleanupBuffer.Add(id);
            }

            foreach (int idToRemove in _cleanupBuffer) RemoveSurface(idToRemove);
        }

        private void ScanHierarchyRecursive(Transform parent)
        {
            foreach (Transform child in parent)
            {
                int id = child.gameObject.GetInstanceID();

                if (!_surfaceIds.Contains(id) && IsValidCandidate(child))
                {
                    TryAddSurface(child.gameObject);
                }

                if (child.childCount > 0) ScanHierarchyRecursive(child);
            }
        }

        public bool TryAddSurface(GameObject obj)
        {
            if (obj == null) return false;
            if (!IsValidCandidate(obj.transform)) return false;

            if (_registry.TryRegister(obj))
            {
                if (!_surfaceIds.Contains(obj.GetInstanceID())) _surfaceIds.Add(obj.GetInstanceID());
                if (!surfaces.Contains(obj)) surfaces.Add(obj);

                _needsSpatialSync = true;
                return true;
            }
            return false;
        }

        public bool RemoveSurface(int id)
        {
            if (_registry.Unregister(id))
            {
                _surfaceIds.Remove(id);
                surfaces.RemoveAll(s => s.GetInstanceID() == id);

                _needsSpatialSync = true;
                return true;
            }
            
            return false;
        }

        private bool IsValidCandidate(Transform t)
        {
            if (!string.IsNullOrEmpty(nameFilter) && !t.name.Contains(nameFilter)) return false;

            if (enableAreaCheck)
            {
                Bounds b;
                if (t.TryGetComponent<Renderer>(out var r)) b = r.bounds;
                else if (t.TryGetComponent<Collider>(out var c)) b = c.bounds;
                else return false;

                return (b.size.x * b.size.z) > minAreaThreshold;
            }
            return true;
        }

        #endregion

        #region Runtime Processing & Baking

        private void SynchronizeChunksWithRegistry()
        {
            int createdCount = 0;
            int updatedCount = 0;
            int removedCount = 0;

            foreach (var key in _registry.GetDirtyBuckets())
            {
                bool hasData = _registry.HasDataInBucket(key);
                bool exists = _chunkRegistry.TryGetEntry(key, out var chunk);

                if (hasData)
                {
                    _chunksPendingBake.Add(key);

                    if (exists)
                    {
                        updatedCount++;
                        LogDebug($"Sync: Existing chunk {key} data changed");
                    }
                    else
                    {
                        Action<SurfaceChunk> configChunk = _ =>
                        {
                            _.OnLODChanged += HandleChunkLODChanged;
                            _.OnCleanup += HandleChunkCleanup;
                        };

                        if(_chunkRegistry.GetOrCreateChunk(key, configChunk, out chunk))
                        {
                            createdCount++;
                        }
                        LogDebug($"Sync: Created new chunk shell at {key}");
                    }
                }
                else if (exists)
                {
                    _chunkRegistry.RemoveAndDestroy(key);
                    removedCount++;
                    LogDebug($"Sync: Removed empty chunk shell at {key}");
                }
            }
            _registry.ClearDirtyBuckets();

            LogDebug($"Spatial Sync Summary: {createdCount} created, {updatedCount} updated, {removedCount} removed.");
        }

        private void HandleChunkLODChanged(ILODState sender, int oldLod, int newLod)
        {
            if (oldLod != newLod)
            {
                if (oldLod >= 0)
                {
                    _toRelease.Add(sender.GridKey);
                }

                if (newLod >= 0)
                {
                    _toAssign.Add(sender.GridKey);
                }
                else
                {
                    _toAssign.Remove(sender.GridKey);
                }

                _needsBufferSync = true;
            }
        }

        private void HandleChunkCleanup(SurfaceChunk sender)
        {
            _toRelease.Add(sender.GridKey);
        }

        private void UdpateChunkHandles()
        {
            foreach (var key in _toRelease)
            {
                if (_leasedBuffers.TryGetValue(key, out var lease))
                {
                    lease.Return();
                    _leasedBuffers.Remove(key);

                    LogDebug($"Returned buffer for chunk {key}");
                }
            }
            _toRelease.Clear();


            foreach (var key in _toAssign)
            { 
                if (_chunkRegistry.TryGetEntry(key, out SurfaceChunk chunk))
                {
                    int currentLod = chunk.CurrentLOD;

                    var settings = _validLodLevels[currentLod];
                    var res = (int)settings.mapResolution;

                    var descriptor = new RenderTextureDescriptorWrapper { Descriptor = DefaultDescriptors.HeightmapDefault(res, res) };
                    var newLease = _texturePool.Rent(descriptor);

                    _leasedBuffers[key] = newLease;
                    chunk.SetHeightmap(newLease.BufferHandle.Buffer);

                    _chunksPendingBake.Add(key);
                    LogDebug($"Rented buffer for chunk {key}");
                }
            }
            _toAssign.Clear();
        }
        
        private void ProcessBaking()
        {
            LogDebug($"Beginning Baking Cycle...");

            foreach (var key in _chunksPendingBake)
            {
                if (_chunkRegistry.TryGetEntry(key, out SurfaceChunk chunk))
                {
                    //chunk.Heightmap;
                }
            }

            _chunksPendingBake.Clear();
        }

        private void PerformChunkBake(SurfaceChunk chunk)
        {
            var relevantObjects = _registry.GetObjectsInCell(chunk.GridKey);
            int resolution = GetResolutionForLod(chunk.CurrentLOD);

            LogDebug($"Baking Chunk {chunk.GridKey} | LOD {chunk.CurrentLOD} | Res {resolution}px | Objects: {relevantObjects.Count}");

            // English: HeightmapBaker.Bake(chunk, relevantObjects, resolution);
        }
        
        private bool CheckMovementThreshold()
        {
            float distSqr = (lodReference.position - _lastUpdatePos).sqrMagnitude;
            float threshold = (float)chunkSize * updateSensitivity;

            if (distSqr > threshold * threshold)
            {
                _lastUpdatePos = lodReference.position;
                return true;
            }
            return false;
        }

        private int GetResolutionForLod(int lodLevel)
        {
            if (lodLevels == null || lodLevels.Length == 0) return 64;
            int index = Mathf.Clamp(lodLevel, 0, lodLevels.Length - 1);
            return (int)lodLevels[index].mapResolution;
        }

        #endregion

        #region System Events & Cleanup

        private void HandleOriginShift(Vector3 delta)
        {
            _chunkRegistry?.NotifyOriginShift(delta);
            _lastUpdatePos += delta;
        }

        public void ResetTrackingPosition() => _lastUpdatePos = lodReference ? lodReference.position : Vector3.zero;

        /// <summary>
        /// English: Completely wipes all registered surfaces, destroys all chunks, 
        /// and resets the registries to a clean state.
        /// </summary>
        public void ClearAll()
        {
            LogDebug("Performing full system cleanup...");

            _surfaceIds.Clear();
            surfaces.Clear();
            _cleanupBuffer.Clear();

            if (_registry != null)
            {
                _registry = new SpatialObjectRegistry { showDebugLogs = this.showDebugLogs };
                _registry.Initialize(_chunkRegistry);
            }

            if (_chunkRegistry != null)
            {
                _chunkRegistry.Dispose();
                CreateChunkRegistry(true);
            }

            ResetTrackingPosition();

            LogDebug("System cleared. All chunks destroyed and registries reset.");
        }
        #endregion

        #region Debug Helper
        [Conditional("UNITY_EDITOR")]
        private void LogDebug(string msg, [CallerLineNumber] int line = 0) { DebugOutput.Log(msg, showDebugLogs, lineNumber: line); }
        #endregion
    }

#if UNITY_EDITOR
    /// <summary>
    /// Custom Editor to provide convenient buttons for surface management in the Inspector.
    /// </summary>
    [UnityEditor.CustomEditor(typeof(SurfaceManager))]
    public class SurfaceManagerEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            SurfaceManager script = (SurfaceManager)target;

            GUILayout.Space(10);
            if (GUILayout.Button("Refresh Surfaces", GUILayout.Height(30)))
            {
                script.RebuildRegistry();
            }

            GUILayout.Space(10);
            if (GUILayout.Button("Clear Surfaces", GUILayout.Height(30)))
            {
                script.ClearAll();
            }
        }
    }
#endif
}