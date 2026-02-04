using Rayforge.Core.Common.Rendering;
using Rayforge.Core.Common.Rendering.Helpers;
using Rayforge.Core.Environment.Spatial.Surfaces;
using System.Collections.Generic;
using UnityEngine;

namespace Rayforge.Core.Environment.Spatial.Surface
{
    /// <summary>
    /// Scans the scene hierarchy for valid world objects and synchronizes them with the SurfaceRegistry.
    /// Handles filtering by name and physical area to ensure only relevant surfaces are processed.
    /// </summary>
    public class SurfaceManager : MonoBehaviour
    {
        [System.Serializable]
        public struct SurfaceLODLevel
        {
            [Tooltip("Distance threshold for this level.")]
            public float distanceThreshold;

            [Tooltip("Edge resolution for the heightmap.")]
            public PowerOfTwoResolution mapResolution;
        }

        #region Inspector Fields
        [Header("Floating Origin")]
        [Tooltip("The relay that monitors world movement. If null, it will be searched in parents/siblings during Awake.")]
        public OriginShiftRelay shiftRelay;

        [Header("LOD & Culling")]
        [Tooltip("The reference point for LOD calculations (usually Main Camera).")]
        public Transform lodReference;
        [Tooltip("The physical size of a single volumetric chunk in meters.")]
        public ChunkSizeBinary chunkSize = ChunkSizeBinary.Medium;
        [Tooltip("What percentage of the chunk size must the camera move before updating? (e.g., 0.1 = 10%)")]
        [Range(0.01f, 0.5f)]
        public float updateSensitivity = 0.1f;
        [Tooltip("Define LOD levels. Use the +/- buttons. The system auto-validates distances and resolutions.")]
        public SurfaceLODLevel[] lodLevels;

        [Header("Detection Settings")]
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
        private SurfaceRegistry _registry;
        private Vector3 _lastUpdatePos;
        #endregion

        #region Unity Lifecycle
        private void Awake()
        {
            SetupDependencies();
            InitializeRegistries();

            if (shiftRelay != null)
            {
                shiftRelay.OnWorldShiftDetected -= HandleOriginShift;
                shiftRelay.OnWorldShiftDetected += HandleOriginShift;
            }
        }

        private void Start()
        {
            if (autoUpdate) RebuildRegistry();
        }

        private void Update()
        {
            if (lodReference == null || _chunkRegistry == null) return;

            Vector3 curPos = lodReference.position;
            float moveDistSqr = (curPos - _lastUpdatePos).sqrMagnitude;
            float threshold = (float)chunkSize * updateSensitivity;

            if (moveDistSqr > threshold * threshold)
            {
                _lastUpdatePos = curPos;

                _chunkRegistry.UpdateLODs();
                _registry.ApplyChanges();
            }
        }

        private void OnDestroy()
        {
            if (shiftRelay != null)
                shiftRelay.OnWorldShiftDetected -= HandleOriginShift;
        }

        private void OnValidate()
        {
            SetupDependencies();
            SyncLodLevels();

        }
        #endregion

        #region Initialization & Setup
        public void SetupDependencies()
        {
            if (shiftRelay == null)
                shiftRelay = GetComponentInParent<OriginShiftRelay>(true);

            if (lodReference == null && Camera.main != null)
                lodReference = Camera.main.transform;
        }

        private void InitializeRegistries()
        {
            _chunkRegistry = new LODChunkRegistry<SurfaceChunk>(
                (ChunkSize)chunkSize,
                transform.position,
                GetLodDistances(),
                lodReference,
                this.transform
            );

            _registry = new SurfaceRegistry(_chunkRegistry);

            ApplyInspectorSettings();

            _lastUpdatePos = (lodReference != null) ? lodReference.position : Vector3.zero;
        }

        /// <summary>
        /// Pushes the current inspector values into the existing registry instances.
        /// Call this whenever a value in the inspector changes.
        /// </summary>
        public void ApplyInspectorSettings()
        {
            if (_chunkRegistry == null) return;

            _chunkRegistry.SetViewer(lodReference);
            _chunkRegistry.Setup(
                (ChunkSize)chunkSize,
                GetLodDistances(),
                lodReference
            );

            Debug.Log("[SurfaceManager] Inspector settings applied to active Registry.");
        }

        /// <summary>
        /// English: Extracts the raw distance thresholds from the Inspector-friendly LOD list.
        /// </summary>
        private float[] GetLodDistances()
        {
            if (lodLevels == null) return new float[0];

            float[] distances = new float[lodLevels.Length];
            for (int i = 0; i < lodLevels.Length; i++)
                distances[i] = lodLevels[i].distanceThreshold;

            return distances;
        }

        #endregion

        #region Logic Overrides
        private void HandleOriginShift(Vector3 delta)
        {
            _chunkRegistry.NotifyOriginShift(delta);
            _lastUpdatePos += delta; // English: Keep tracking relative to the shifted world
        }

        private void SyncLodLevels()
        {
            if (lodLevels == null || lodLevels.Length == 0) return;
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
                }
                else
                {
                    if (current.distanceThreshold == 0) current.distanceThreshold = 50f;
                }
                lodLevels[i] = current;
            }
        }
        #endregion

        #region Registry Synchronization
        public void RebuildRegistry()
        {
            SyncFromList();
            if (scanHierarchy) ScanHierarchyRecursive(transform);
        }

        public void SyncFromList()
        {
            _surfaceIds.Clear();
            for (int i = surfaces.Count - 1; i >= 0; i--)
            {
                GameObject obj = surfaces[i];
                if (obj == null || !IsValidCandidate(obj.transform))
                {
                    if (obj != null) ForceRemoveSurface(obj.GetInstanceID());
                    else surfaces.RemoveAt(i);
                    continue;
                }
                if (TryAddSurface(obj)) _surfaceIds.Add(obj.GetInstanceID());
            }

            _cleanupBuffer.Clear();
            foreach (int registeredId in _registry.GetAllIds())
                if (!_surfaceIds.Contains(registeredId)) _cleanupBuffer.Add(registeredId);

            foreach (int idToRemove in _cleanupBuffer) ForceRemoveSurface(idToRemove);
        }

        private void ScanHierarchyRecursive(Transform parent)
        {
            foreach (Transform child in parent)
            {
                if (!_surfaceIds.Contains(child.gameObject.GetInstanceID()) && IsValidCandidate(child))
                    TryAddSurface(child.gameObject);

                if (child.childCount > 0) ScanHierarchyRecursive(child);
            }
        }

        public bool TryAddSurface(GameObject obj)
        {
            if (obj == null || !IsValidCandidate(obj.transform)) return false;

            int id = obj.GetInstanceID();
            if (_registry.TryRegisterSurface(obj))
            {
                if (_surfaceIds.Add(id) && !surfaces.Contains(obj)) surfaces.Add(obj);
                return true;
            }
            return false;
        }

        public bool ForceRemoveSurface(int id)
        {
            bool removed = _registry.UnregisterSurface(id);
            _surfaceIds.Remove(id);
            surfaces.RemoveAll(s => s == null || s.GetInstanceID() == id);
            return removed;
        }

        public void ClearAllSurfaces()
        {
            int[] idsToClear = new int[_surfaceIds.Count];
            _surfaceIds.CopyTo(idsToClear);

            foreach (int id in idsToClear)
            {
                ForceRemoveSurface(id);
            }

            surfaces.Clear();
            _surfaceIds.Clear();

            Debug.Log("[SurfaceManager] All surfaces cleared.");
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
                script.ClearAllSurfaces();
            }
        }
    }
#endif
}