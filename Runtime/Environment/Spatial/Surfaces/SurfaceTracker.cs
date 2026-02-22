using Rayforge.Core.Environment.Abstractions;
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

        /// <summary> The shared spatial database where validated surfaces are stored. </summary>
        private SpatialSurfaceRegistry _objectRegistry;

        /// <summary> Cache of InstanceIDs currently owned by this specific tracker. </summary>
        private readonly HashSet<int> _surfaceIds = new HashSet<int>();

        /// <summary>
        /// Provides public access to the internal settings.
        /// </summary>
        public SurfaceTrackerSettings Settings
        {
            get => _settings;
            set => _settings = value;
        }

        /// <summary>
        /// Returns a read-only view of the currently tracked surfaces managed by this tracker.
        /// </summary>
        public IReadOnlyList<GameObject> TrackedSurfaces => _surfaces;

        /// <summary>
        /// True if the tracker's list or registry entries have changed since the last clear.
        /// </summary>
        public bool IsDirty { get; private set; }

        #endregion

        #region Lifecycle & Initialization

        /// <summary>
        /// Connects the tracker to an external spatial registry and synchronizes existing data.
        /// </summary>
        /// <param name="externalRegistry">The shared spatial database to populate.</param>
        public void Initialize(SpatialSurfaceRegistry externalRegistry)
        {
            _objectRegistry = externalRegistry;
            _surfaceIds.Clear();

            if (_objectRegistry != null && _objectRegistry.IsInitialized)
            {
                foreach (var obj in _surfaces)
                {
                    if (obj != null && _objectRegistry.TryRegister(obj))
                    {
                        _surfaceIds.Add(obj.GetInstanceID());
                    }
                }
            }
        }

        /// <summary>
        /// Performs a complete rebuild of the tracker state by syncing the list and scanning hierarchy.
        /// </summary>
        /// <param name="root">The root transform to start the hierarchy scan from.</param>
        public void RebuildRegistry(Transform root)
        {
            if (_objectRegistry == null) return;

            SyncFromList();

            if (_settings.scanHierarchy && root != null)
            {
                ScanHierarchyRecursive(root);
            }
        }

        #endregion

        #region Registry Synchronization

        /// <summary>
        /// Synchronizes the internal surface list with the external registry. 
        /// Removes invalid or null entries.
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
        }

        /// <summary>
        /// Recursively scans a transform hierarchy to find and register new surface candidates.
        /// </summary>
        /// <param name="parent">The starting transform for the recursion.</param>
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

        #endregion

        #region Public API (Add / Remove)

        /// <summary>
        /// Validates and attempts to add a GameObject to the shared registry.
        /// </summary>
        /// <param name="obj">The GameObject candidate.</param>
        /// <returns>True if the object passed validation and was registered.</returns>
        public bool TryAddSurface(GameObject obj)
        {
            if (obj == null || _objectRegistry == null) return false;
            if (!IsValidCandidate(obj.transform)) return false;

            return TryRegisterInternal(obj);
        }

        /// <summary>
        /// Registers an object in the external database and updates local tracking state.
        /// </summary>
        /// <param name="obj">The validated GameObject to register.</param>
        /// <returns>True if registration in the external registry was successful.</returns>
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
        /// Removes a surface from the local tracking set and unregisters it from the shared spatial database.
        /// </summary>
        /// <param name="id">The InstanceID of the GameObject to remove.</param>
        /// <returns>True if the object was found and successfully removed from the registry.</returns>
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

        #endregion

        #region State Management

        /// <summary>
        /// Clears the tracker's ID cache and removes its surfaces from the registry, 
        /// but keeps the internal serialized list.
        /// </summary>
        public void ClearState()
        {
            if (_objectRegistry != null)
            {
                foreach (int id in _surfaceIds)
                {
                    _objectRegistry.Unregister(id);
                }
            }

            _surfaceIds.Clear();
            IsDirty = true;
        }

        /// <summary>
        /// Wipes all local surface data and removes them from the external registry.
        /// </summary>
        public void ClearAll()
        {
            ClearState();
            _surfaces.Clear();
        }

        /// <summary>
        /// Resets the dirty flag after an external system has processed the changes.
        /// </summary>
        public void ClearDirty() => IsDirty = false;

        /// <summary>
        /// Resets the tracker completely, clearing both local lists and registry entries.
        /// </summary>
        public void Reset()
        {
            ClearAll();
            IsDirty = true;
        }

        #endregion

        #region Validation Logic

        /// <summary>
        /// Evaluates a transform against the internal SurfaceTrackerSettings (Name filter, Area threshold).
        /// </summary>
        /// <param name="t">The transform of the candidate.</param>
        /// <returns>True if the transform meets all defined criteria.</returns>
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

        #endregion
    }
}