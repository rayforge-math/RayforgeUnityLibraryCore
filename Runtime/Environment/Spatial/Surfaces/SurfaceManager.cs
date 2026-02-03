using System.Collections.Generic;
using UnityEngine;

namespace Rayforge.Core.Environment.Spatial.Surfaces
{
    /// <summary>
    /// Scans the scene hierarchy for valid world objects and synchronizes them with the SurfaceRegistry.
    /// Handles filtering by name and physical area to ensure only relevant surfaces are processed.
    /// </summary>
    public class SurfaceManager : MonoBehaviour
    {
        [Header("Workflow Settings")]
        [Tooltip("If true, RebuildRegistry() is called automatically on Start.")]
        public bool autoUpdate = false;

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

        [Header("Surfaces")]
        [Tooltip("Manual list of surfaces. If Auto Detect is enabled, this list is populated automatically.")]
        public List<GameObject> surfaces = new List<GameObject>();

        private readonly HashSet<int> _surfaceIds = new HashSet<int>();
        private readonly List<int> _cleanupBuffer = new List<int>(32);
        private SurfaceRegistry _registry = new SurfaceRegistry();

        private void Start()
        {
            if (autoUpdate)
            {
                RebuildRegistry();
            }
        }

        /// <summary>
        /// Orchestrates the full synchronization process: cleans the manual list and performs a hierarchy scan.
        /// </summary>
        public void RebuildRegistry()
        {
            SyncFromList();

            if (scanHierarchy)
            {
                ScanHierarchyRecursive(transform);
            }
        }

        /// <summary>
        /// Validates existing entries in the surfaces list, removes invalid ones, and cleans up orphans in the registry.
        /// </summary>
        public void SyncFromList()
        {
            _surfaceIds.Clear();

            // Phase A: Clean up current list and register valid candidates
            for (int i = surfaces.Count - 1; i >= 0; i--)
            {
                GameObject obj = surfaces[i];
                if (obj == null || !IsValidCandidate(obj.transform))
                {
                    if (obj != null)
                    {
                        Debug.Log($"[SurfaceManager] Removing '{obj.name}' - Filter/Validation mismatch.");
                        ForceRemoveSurface(obj.GetInstanceID());
                    }
                    else
                    {
                        surfaces.RemoveAt(i);
                    }
                    continue;
                }

                if (TryAddSurface(obj))
                {
                    _surfaceIds.Add(obj.GetInstanceID());
                }
            }

            // Phase B: Clean up Registry Orphans (IDs present in registry but no longer in manager)
            _cleanupBuffer.Clear();
            foreach (int registeredId in _registry.GetAllIds())
            {
                if (!_surfaceIds.Contains(registeredId))
                    _cleanupBuffer.Add(registeredId);
            }

            foreach (int idToRemove in _cleanupBuffer)
            {
                Debug.Log($"[SurfaceManager] Removing ID {idToRemove} (Orphan detected).");
                ForceRemoveSurface(idToRemove);
            }
        }

        /// <summary>
        /// Recursively traverses the hierarchy to find new surface candidates.
        /// </summary>
        /// <param name="parent">The transform to start the scan from.</param>
        private void ScanHierarchyRecursive(Transform parent)
        {
            int childCount = parent.childCount;
            for (int i = 0; i < childCount; i++)
            {
                Transform child = parent.GetChild(i);

                if (!_surfaceIds.Contains(child.gameObject.GetInstanceID()))
                {
                    if (IsValidCandidate(child))
                    {
                        if (TryAddSurface(child.gameObject))
                        {
                            Debug.Log($"[SurfaceManager] New Discovery: Added '{child.name}'.");
                        }
                    }
                }

                if (child.childCount > 0)
                    ScanHierarchyRecursive(child);
            }
        }

        /// <summary>
        /// Completely clears all tracked surfaces from the manager and the registry.
        /// </summary>
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

        /// <summary>
        /// Attempts to register a GameObject with the SurfaceRegistry after validation.
        /// </summary>
        /// <param name="obj">The GameObject to add.</param>
        /// <returns>True if successfully registered.</returns>
        public bool TryAddSurface(GameObject obj)
        {
            if (obj == null) return false;
            int id = obj.GetInstanceID();

            if (!IsValidCandidate(obj.transform))
            {
                if (_surfaceIds.Contains(id) || surfaces.Contains(obj)) ForceRemoveSurface(id);
                return false;
            }

            // In a real scenario, we would create a SpatialObjectState here and pass it
            if (_registry.TryRegisterSurface(obj))
            {
                if (_surfaceIds.Add(id))
                {
                    if (!surfaces.Contains(obj))
                    {
                        surfaces.Add(obj);
                    }
                }
                return true;
            }

            return false;
        }

        /// <summary>
        /// Removes a surface by its unique instance ID from the registry and internal tracking.
        /// </summary>
        /// <param name="id">Unity InstanceID of the object.</param>
        /// <returns>True if the object was found and removed from the registry.</returns>
        public bool ForceRemoveSurface(int id)
        {
            bool wasInRegistry = _registry.UnregisterSurface(id);

            _surfaceIds.Remove(id);

            for (int i = surfaces.Count - 1; i >= 0; i--)
            {
                if (surfaces[i] == null || surfaces[i].GetInstanceID() == id)
                {
                    surfaces.RemoveAt(i);
                }
            }

            return wasInRegistry;
        }

        /// <summary>
        /// Evaluates if a transform meets the naming and area criteria.
        /// </summary>
        /// <param name="t">The transform to check.</param>
        /// <returns>True if the candidate is valid for the registry.</returns>
        private bool IsValidCandidate(Transform t)
        {
            if (!string.IsNullOrEmpty(nameFilter))
            {
                if (!t.name.Contains(nameFilter)) return false;
            }

            if (enableAreaCheck)
            {
                Bounds b;

                if (t.TryGetComponent<Renderer>(out var renderer))
                {
                    b = renderer.bounds;
                }
                else if (t.TryGetComponent<Collider>(out var col))
                {
                    b = col.bounds;
                }
                else
                {
                    return false;
                }

                float area = b.size.x * b.size.z;
                return area > minAreaThreshold;
            }

            return true;
        }
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