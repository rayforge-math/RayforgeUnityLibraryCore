using Rayforge.Core.Environment.Abstractions;
using Rayforge.Core.Environment.Spatial.Chunks;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Rayforge.Core.Environment.Spatial.Surfaces
{
    /// <summary>
    /// Manages the detection and synchronization of world surfaces.
    /// It maintains an internal serialized list and its own settings, 
    /// syncing everything with an internally owned SpatialObjectRegistry.
    /// </summary>
    [Serializable]
    public class SurfaceTracker
    {
        #region Serialized Fields

        [SerializeField]
        private SurfaceTrackerSettings _settings = SurfaceTrackerSettings.Default;

        [SerializeField]
        [Tooltip("Internal list of validated surfaces. Use TrackedSurfaces to access this externally.")]
        private List<GameObject> _surfaces = new List<GameObject>();

        #endregion

        #region Private Runtime State

        private SpatialObjectRegistry _objectRegistry;

        private readonly HashSet<int> _surfaceIds = new HashSet<int>();
        private readonly List<int> _cleanupBuffer = new List<int>(32);

        /// <summary>
        /// Provides public access to the internal settings.
        /// </summary>
        public SurfaceTrackerSettings Settings
        {
            get => _settings;
            set => _settings = value;
        }

        /// <summary>
        /// Returns a read-only view of the currently tracked surfaces.
        /// </summary>
        public IReadOnlyList<GameObject> TrackedSurfaces => _surfaces;

        /// <summary>
        /// Provides read-only access to the registry for spatial queries.
        /// </summary>
        public ISpatialCollection Registry => _objectRegistry;

        /// <summary>
        /// True if the registry or list has changed and requires an external spatial update.
        /// </summary>
        public bool IsDirty { get; private set; }

        #endregion

        /// <summary>
        /// Initializes the internal registry using the provided chunk infrastructure.
        /// </summary>
        /// <param name="chunkRegistry">The grid/chunk system required by the spatial registry.</param>
        public void Initialize(ISpatialGridProvider<Vector3Int> chunkRegistry)
        {
            if(_objectRegistry == null)
            {
                _objectRegistry = new SpatialObjectRegistry();
            }
            else
            {
                _objectRegistry.Clear();
            }

            _objectRegistry.Initialize(chunkRegistry);

            _surfaceIds.Clear();
            foreach (var obj in _surfaces)
            {
                if (obj != null)
                {
                    _objectRegistry.TryRegister(obj);
                    _surfaceIds.Add(obj.GetInstanceID());
                }
            }
        }

        /// <summary>
        /// Performs a complete rebuild of the registry using internal settings and hierarchy scanning.
        /// </summary>
        /// <param name="root">The root transform to start the hierarchy scan from.</param>
        public void RebuildRegistry(Transform root)
        {
            if (_objectRegistry == null) return;

            _objectRegistry.Clear();
            SyncFromList();

            if (_settings.scanHierarchy && root != null)
            {
                ScanHierarchyRecursive(root);
            }
        }

        /// <summary>
        /// Clears all runtime data including the registry and ID cache, but keeps the serialized surface list.
        /// </summary>
        public void ClearState()
        {
            _objectRegistry?.Clear();
            _surfaceIds.Clear();
            _cleanupBuffer.Clear();
            IsDirty = true;
        }

        /// <summary>
        /// Wipes everything including the serialized list and the registry.
        /// </summary>
        public void ClearAll()
        {
            _surfaces.Clear();
            ClearState();
        }

        /// <summary>
        /// Synchronizes the internal list with the registry and removes invalid entries.
        /// </summary>
        private void SyncFromList()
        {
            if (_objectRegistry == null) return;

            _surfaceIds.Clear();

            for (int i = _surfaces.Count - 1; i >= 0; i--)
            {
                GameObject obj = _surfaces[i];
                if (obj == null)
                {
                    _surfaces.RemoveAt(i);
                    IsDirty = true;
                    continue;
                }

                if (!IsValidCandidate(obj.transform))
                {
                    RemoveSurface(obj.GetInstanceID());
                    continue;
                }

                TryRegisterInternal(obj);
            }

            _cleanupBuffer.Clear();
            foreach (int id in _objectRegistry.GetAllIds())
            {
                if (!_surfaceIds.Contains(id))
                {
                    _cleanupBuffer.Add(id);
                }
            }

            foreach (int idToRemove in _cleanupBuffer)
            {
                RemoveSurface(idToRemove);
            }
        }

        /// <summary>
        /// Recursively scans a hierarchy and adds new valid candidates.
        /// </summary>
        /// <param name="parent">The current parent transform in the recursion.</param>
        private void ScanHierarchyRecursive(Transform parent)
        {
            foreach (Transform child in parent)
            {
                int id = child.gameObject.GetInstanceID();

                if (!_surfaceIds.Contains(id) && IsValidCandidate(child))
                {
                    TryAddSurface(child.gameObject);
                }

                if (child.childCount > 0)
                {
                    ScanHierarchyRecursive(child);
                }
            }
        }

        /// <summary>
        /// Attempts to add a specific surface. Validates it against internal settings first.
        /// </summary>
        /// <param name="obj">The GameObject to be tracked as a surface.</param>
        /// <returns>True if the object was valid and successfully registered.</returns>
        public bool TryAddSurface(GameObject obj)
        {
            if (obj == null || _objectRegistry == null) return false;
            if (!IsValidCandidate(obj.transform)) return false;

            return TryRegisterInternal(obj);
        }

        /// <summary>
        /// Internal helper to update registry, list, and ID cache simultaneously.
        /// </summary>
        /// <param name="obj">The validated GameObject to register.</param>
        /// <returns>True if registration was successful.</returns>
        private bool TryRegisterInternal(GameObject obj)
        {
            int id = obj.GetInstanceID();

            if (_objectRegistry.TryRegister(obj))
            {
                if (!_surfaceIds.Contains(id)) _surfaceIds.Add(id);
                if (!_surfaces.Contains(obj)) _surfaces.Add(obj);

                IsDirty = true;
                return true;
            }
            return false;
        }

        /// <summary>
        /// Removes a surface from the tracking set and the spatial registry.
        /// </summary>
        /// <param name="id">The InstanceID of the GameObject to remove.</param>
        /// <returns>True if the object was found and successfully removed.</returns>
        public bool RemoveSurface(int id)
        {
            if (_objectRegistry == null) return false;

            if (_objectRegistry.Unregister(id))
            {
                _surfaceIds.Remove(id);
                _surfaces.RemoveAll(s => s == null || s.GetInstanceID() == id);

                IsDirty = true;
                return true;
            }

            return false;
        }

        /// <summary>
        /// Resets the spatial sync flag after the manager has handled the update.
        /// </summary>
        public void ClearDirty() => IsDirty = false;

        /// <summary>
        /// Evaluates a transform against internal name and area filtering rules.
        /// </summary>
        /// <param name="t">The transform of the candidate object.</param>
        /// <returns>True if the object matches all criteria in SurfaceTrackerSettings.</returns>
        private bool IsValidCandidate(Transform t)
        {
            if (!string.IsNullOrEmpty(_settings.nameFilter) && !t.name.Contains(_settings.nameFilter))
            {
                return false;
            }

            if (_settings.enableAreaCheck)
            {
                Bounds b;
                if (t.TryGetComponent<Renderer>(out var r)) b = r.bounds;
                else if (t.TryGetComponent<Collider>(out var c)) b = c.bounds;
                else return false;

                return (b.size.x * b.size.z) > _settings.minAreaThreshold;
            }

            return true;
        }

        /// <summary>
        /// Completely clears all tracking data and resets the spatial sync flag.
        /// </summary>
        public void Reset()
        {
            _surfaces.Clear();
            _surfaceIds.Clear();
            _objectRegistry?.Clear();
            IsDirty = true;
        }
    }
}