using Rayforge.Core.Common.Rendering;
using Rayforge.Core.Common.Rendering.Helpers;
using Rayforge.Core.Environment.Spatial.Surfaces;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using Rayforge.Core.Diagnostics;
using UnityEngine;

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

        #region Configuration: General & Debug
        [Header("Debug & Diagnostics")]
        public bool showDebugLogs = false;

        [Header("Floating Origin")]
        [Tooltip("Monitors world movement to handle coordinate shifts.")]
        public OriginShiftRelay shiftRelay;
        #endregion

        #region Configuration: LOD & Grid
        [Header("LOD & Culling Settings")]
        [Tooltip("The reference point for LOD calculations (usually Main Camera).")]
        public Transform lodReference;

        [Tooltip("The physical size of a single chunk in meters.")]
        public ChunkSizeBinary chunkSize = ChunkSizeBinary.Huge;

        [Tooltip("Movement threshold (% of chunk size) before triggering updates.")]
        [Range(0.01f, 0.5f)]
        public float updateSensitivity = 0.1f;

        [Tooltip("Define LOD levels (Distances and Resolutions).")]
        public SurfaceLODLevel[] lodLevels;
        #endregion

        #region Configuration: Detection
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
        [Tooltip("If true, RebuildRegistry() is called automatically on Start.")]
        public bool autoUpdate = false;

        [Header("Surfaces")]
        [Tooltip("Manual list of surfaces. If Auto Detect is enabled, this list is populated automatically.")]
        public List<GameObject> surfaces = new List<GameObject>();
        #endregion

        #region Private State
        private readonly HashSet<int> _surfaceIds = new HashSet<int>();
        private readonly List<int> _cleanupBuffer = new List<int>(32);

        private LODChunkRegistry<SurfaceChunk> _chunkRegistry;
        private SpatialObjectRegistry _registry;
        private Vector3 _lastUpdatePos;

        private bool IsReady => lodReference != null && _chunkRegistry != null && _registry != null;
        #endregion

        #region Unity Lifecycle
        private void Awake()
        {
            SetupDependencies();
            EnsureSystemsReady(true);

            if (shiftRelay != null)
                shiftRelay.OnWorldShiftDetected += HandleOriginShift;
        }

        private void Start()
        {
            if (autoUpdate) RebuildRegistry();
        }

        private void Update()
        {
            if (!IsReady) return;

            if (CheckMovementThreshold())
            {
                _chunkRegistry.UpdateLODs();
                ProcessBaking();
            }
        }

        private void OnDestroy()
        {
            if (shiftRelay != null)
                shiftRelay.OnWorldShiftDetected -= HandleOriginShift;

            _chunkRegistry?.Dispose();
        }

        private void OnValidate()
        {
            SetupDependencies();
            SanitizeLODLevels();

            EnsureSystemsReady(false);
            UpdateGridSize();
            UpdateLODSettings();
        }
        #endregion

        #region Logic: Initialization & Setup
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
                _registry = new SpatialObjectRegistry { showDebugLogs = this.showDebugLogs };
                LogDebug("Spatial Object Registry initialized.");
            }
        }

        private void CreateChunkRegistry(bool force = false)
        {
            if (_chunkRegistry == null || force)
            {
                _chunkRegistry?.Dispose();

                _chunkRegistry = new LODChunkRegistry<SurfaceChunk>(
                    (ChunkSize)chunkSize,
                    transform.position,
                    GetValidLodDistances(),
                    lodReference,
                    this.transform
                );

                _chunkRegistry.showDebugLogs = this.showDebugLogs;

                _registry.Initialize(_chunkRegistry);
                LogDebug($"Chunk Registry created. GridSize: {(int)chunkSize}m");
            }
        }

        public void ResetTrackingPosition() => _lastUpdatePos = lodReference ? lodReference.position : Vector3.zero;

        public void SetupDependencies()
        {
            if (shiftRelay == null)
                shiftRelay = GetComponentInParent<OriginShiftRelay>(true);

            if (lodReference == null && Camera.main != null)
                lodReference = Camera.main.transform;
        }

        public void UpdateLODSettings()
        {
            if (_chunkRegistry == null)
            {
                LogDebug("<color=orange>UpdateLODSettings skipped:</color> ChunkRegistry is not initialized yet.");
                return;
            }

            float[] distances = GetValidLodDistances();
            _chunkRegistry.SetViewer(lodReference);
            _chunkRegistry.UpdateLodDistances(distances);

            foreach (var c in _chunkRegistry.AllEntries) c?.MarkDirty();

            LogDebug("LOD Settings synchronized.");
        }

        public void UpdateGridSize()
        {
            if (_chunkRegistry == null)
            {
                LogDebug("<color=orange>UpdateGridSize skipped:</color> No existing registry to update.");
                return;
            }

            if (_chunkRegistry.GridSize != (int)chunkSize)
            {
                LogDebug($"Grid size change detected ({(int)_chunkRegistry.GridSize}m -> {(int)chunkSize}m). Recreating...");
                CreateChunkRegistry(true);
            }
        }
        #endregion

        #region Logic: Updates & Baking
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

        private void ProcessBaking()
        {
            foreach (Vector3Int dirtyKey in _registry.GetDirtyBuckets())
            {
                if (_chunkRegistry.TryGetEntry(dirtyKey, out var chunk))
                    chunk.MarkDirty();
            }
            _registry.ClearDirtyBuckets();

            foreach (var chunk in _chunkRegistry.AllEntries)
            {
                if (chunk == null || !chunk.IsDirty) continue;

                PerformChunkBake(chunk);
                chunk.ClearDirty();
            }
        }

        private void PerformChunkBake(SurfaceChunk chunk)
        {
            var relevantObjects = _registry.GetObjectsInCell(chunk.GridKey);
            int resolution = GetResolutionForLod(chunk.CurrentLOD);

            if (showDebugLogs)
                LogDebug($"Baking Chunk {chunk.GridKey} | LOD {chunk.CurrentLOD} | Res {resolution}px | Objects: {relevantObjects.Count}");

            // English: HeightmapBaker.Bake(chunk, relevantObjects, resolution);
        }

        private int GetResolutionForLod(int lodLevel)
        {
            if (lodLevels == null || lodLevels.Length == 0) return 256;
            int index = Mathf.Clamp(lodLevel, 0, lodLevels.Length - 1);
            return (int)lodLevels[index].mapResolution;
        }
        #endregion

        #region Logic: Registry & Hierarchy Scan
        public void RebuildRegistry()
        {
            LogDebug("Rebuilding Registry...");
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
                    ForceRemoveSurface(obj.GetInstanceID());
                    continue;
                }

                if (TryAddSurface(obj))
                    _surfaceIds.Add(obj.GetInstanceID());
            }

            _cleanupBuffer.Clear();
            foreach (int id in _registry.GetAllIds())
            {
                if (!_surfaceIds.Contains(id)) _cleanupBuffer.Add(id);
            }

            foreach (int idToRemove in _cleanupBuffer) ForceRemoveSurface(idToRemove);
        }

        private void ScanHierarchyRecursive(Transform parent)
        {
            foreach (Transform child in parent)
            {
                int id = child.gameObject.GetInstanceID();

                if (!_surfaceIds.Contains(id) && IsValidCandidate(child))
                {
                    if (TryAddSurface(child.gameObject))
                        _surfaceIds.Add(id);
                }

                if (child.childCount > 0) ScanHierarchyRecursive(child);
            }
        }

        public bool TryAddSurface(GameObject obj)
        {
            if (obj == null || !IsInitializedRegistry()) return false;
            if (!IsValidCandidate(obj.transform)) return false;

            if (_registry.TryRegister(obj))
            {
                if (!_surfaceIds.Contains(obj.GetInstanceID())) _surfaceIds.Add(obj.GetInstanceID());
                if (!surfaces.Contains(obj)) surfaces.Add(obj);
                return true;
            }
            return false;
        }

        public bool ForceRemoveSurface(int id)
        {
            _registry.Unregister(id);
            _surfaceIds.Remove(id);
            surfaces.RemoveAll(s => s == null || s.GetInstanceID() == id);
            return true;
        }

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

        #region Logic: Integrity
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

        private void HandleOriginShift(Vector3 delta)
        {
            _chunkRegistry?.NotifyOriginShift(delta);
            _lastUpdatePos += delta;
        }

        private SurfaceLODLevel[] GetValidLodLevels()
        {
            if (lodLevels == null || lodLevels.Length == 0)
            {
                LogDebug("GetValidLodLevels: No LOD levels defined.");
                return new SurfaceLODLevel[0];
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
            return validLevels.ToArray();
        }

        private float[] GetValidLodDistances()
        {
            var lods = GetValidLodLevels();
            var distances = new float[lods.Length];
            for(int i = 0; i < lods.Length; ++i)
            {
                distances[i] = lods[i].distanceThreshold;
            }

            return distances;
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

        private bool IsInitializedRegistry() => _registry != null && _registry.IsInitialized;
        #endregion

        #region Debug Helper
        [Conditional("UNITY_EDITOR")]
        private void LogDebug(string msg) { DebugOutput.Log(msg, showDebugLogs); }
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