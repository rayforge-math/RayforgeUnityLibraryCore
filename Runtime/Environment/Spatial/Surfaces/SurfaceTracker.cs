using Rayforge.Core.Collections.Abstractions;
using Rayforge.Core.Collections.Iterator.Helpers;
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

        #region Public Members

        /// <summary> Event triggered on registry changes. </summary>
        public event Action<SurfaceTracker> OnRegistryChanged;

        /// <summary>
        /// Provides read-only access to the underlying spatial database.
        /// This allows external systems to perform spatial queries (e.g., Raycasts, Bounds checks)
        /// without needing to know about internal sub-registries.
        /// </summary>
        public ISpatialCollection<Vector3Int> Registry => _objectRegistry;

        /// <summary>
        /// The number of surfaces in the persistent wishlist.
        /// </summary>
        public int WishlistCount => _surfaces.Count;

        /// <summary>
        /// The total number of currently tracked surfaces (Manual + Dynamic).
        /// </summary>
        public int TotalTrackedCount => _trackedSurfaces.Count;

        /// <summary>
        /// Returns a read-only view of the currently tracked surfaces managed by this tracker.
        /// </summary>
        public IIterator<GameObject> TrackedSurfaces => _surfaces.GetEnumerator().ToIterator();

        /// <summary> 
        /// True if the tracker is connected to a valid and initialized registry. 
        /// </summary>
        public bool IsInitialized => _objectRegistry != null && _objectRegistry.IsInitialized;

        #endregion

        #region Private Runtime State

        /// <summary> The shared spatial database where validated surfaces are stored. </summary>
        private SpatialSurfaceRegistry _objectRegistry;

        /// <summary> 
        /// Key: InstanceID, Value: IsManual (true if the surface is part of the persistent _surfaces list). 
        /// </summary>
        private readonly Dictionary<int, bool> _trackedSurfaces = new Dictionary<int, bool>();

        /// <summary> 
        /// Internal flag to track changes within a transaction. 
        /// Only set to true when the underlying registry/spatial data actually changes.
        /// </summary>
        private bool _isDirty;

        /// <summary>
        /// Broadcasts changes to listeners and resets the dirty state.
        /// This ensures that even complex batch operations only trigger a single update.
        /// </summary>
        private void TryNotifyOnRegistryChanged()
        {
            if (_isDirty)
            {
                _isDirty = false;
                OnRegistryChanged?.Invoke(this);
            }
        }

        #endregion

        #region Runtime & Init

        /// <summary>
        /// Connects the tracker to an external spatial registry and synchronizes existing data.
        /// </summary>
        /// <param name="externalRegistry">The shared spatial database to populate.</param>
        public void Initialize(SpatialSurfaceRegistry externalRegistry)
        {
            ClearStateInternal();

            _objectRegistry = externalRegistry;

            if (IsInitialized)
            {
                SyncListToTrackingInternal();
                _isDirty = true;
            }

            TryNotifyOnRegistryChanged();
        }

        /// <summary>
        /// Performs a complete rebuild of the tracker state by syncing the list and scanning hierarchy.
        /// </summary>
        /// <param name="root">The root transform to start the hierarchy scan from.</param>
        /// <returns>True if any surfaces are currently being tracked after the rebuild.</returns>
        public bool RebuildRegistry(Transform root)
        {
            if (!IsInitialized) return false;

            ClearStateInternal();

            SyncListToTrackingInternal();

            if (_settings.scanHierarchy && root != null)
            {
                ScanHierarchyInternal(root);
            }
            TryNotifyOnRegistryChanged();
            return TotalTrackedCount > 0;
        }

        /// <summary>
        /// Attempts to register a GameObject as a dynamic surface.
        /// Validation is performed based on the current tracker settings.
        /// Fires the OnChanged event if the object was successfully registered.
        /// </summary>
        /// <param name="obj">The GameObject candidate to track.</param>
        /// <returns>True if the object passed validation and was added to the registry.</returns>
        public bool TryAddSurface(GameObject obj)
        {
            bool added = TryAddSurfaceInternal(obj, false);
            TryNotifyOnRegistryChanged();
            return added;
        }

        /// <summary>
        /// Removes a surface from tracking using its InstanceID.
        /// This unregisters the object from the spatial database and stops local tracking.
        /// Triggers a notification if the ID was found and removed.
        /// </summary>
        /// <param name="id">The unique InstanceID of the GameObject to remove.</param>
        /// <returns>True if the surface was tracked and successfully removed.</returns>
        public bool RemoveSurface(int id)
        {
            bool removed = RemoveSurfaceInternal(id);
            TryNotifyOnRegistryChanged();
            return removed;
        }

        /// <summary>
        /// Clears the current runtime tracking state and the spatial registry.
        /// Does NOT touch the persistent wishlist (_surfaces).
        /// Resets live data while keeping the user's manual configuration intact.
        /// </summary>
        public void ClearState()
        {
            ClearStateInternal();
            TryNotifyOnRegistryChanged();
        }

        /// <summary>
        /// Wipes ALL data, including the persistent wishlist (_surfaces) and the registry.
        /// WARNING: This permanently clears the user's manual configuration.
        /// </summary>
        public void ClearAll()
        {
            ClearStateInternal();
            _surfaces?.Clear();
            TryNotifyOnRegistryChanged();
        }

        /// <summary>
        /// Resets the tracker completely, clearing both local lists and registry entries,
        /// and disconnecting the registry reference.
        /// WARNING: This permanently clears all data and disables the tracker until Re-Initialize.
        /// </summary>
        public void Reset()
        {
            ClearStateInternal();
            _surfaces?.Clear();
            _objectRegistry = null;
            TryNotifyOnRegistryChanged();
        }

        #endregion

        #region Editor Tooling

        /// <summary>
        /// Scans a transform hierarchy for valid surface candidates.
        /// Found objects are registered as dynamic entries and are not added to the persistent wishlist.
        /// Triggers a single notification if any new surfaces were discovered.
        /// </summary>
        /// <param name="root">The root transform to start the recursive scan from.</param>
        public bool ScanHierarchyToTable(Transform root)
        {
            if (root == null) return false;

            _surfaces.Clear();
            _isDirty = true;

            TraverseHierarchy(root, (obj) =>
            {
                _surfaces.Add(obj);
            });

            if (_isDirty)
            {
                CleanupTableNulls();
            }

            return _surfaces.Count > 0;
        }

        /// <summary>
        /// Clears all entries from the persistent wishlist.
        /// Pure Editor operation. Does not affect live tracking until the next Rebuild.
        /// </summary>
        public void ClearTable()
        {
            if (_surfaces.Count > 0)
            {
                _surfaces.Clear();
                _isDirty = true;
            }
        }

        /// <summary>
        /// Optional: Removes only the 'null' entries from the wishlist.
        /// Useful for cleaning up the Inspector without losing valid references.
        /// </summary>
        public void CleanupTableNulls()
        {
            int removed = _surfaces.RemoveAll(s => s == null);
            if (removed > 0) _isDirty = true;
        }

        #endregion

        #region Internal UI List Logic (Runtime)

        /// <summary>
        /// Internal logic for synchronization without triggering events.
        /// </summary>
        private void SyncListToTrackingInternal()
        {
            CleanupLogicalOrphansInternal();
            SyncNewListEntriesInternal();
        }

        /// <summary>
        /// Removes manual entries from tracking that are no longer in the list.
        /// </summary>
        private void CleanupLogicalOrphansInternal()
        {
            if (!IsInitialized) return;

            List<int> toRemove = new List<int>();

            var it = _trackedSurfaces.GetEnumerator();
            while (it.MoveNext())
            {
                var entry = it.Current;
                if (entry.Value && !IsIdInList(entry.Key))
                {
                    toRemove.Add(entry.Key);
                }
            }

            for (int i = 0; i < toRemove.Count; i++)
            {
                RemoveSurfaceInternal(toRemove[i]);
            }
        }

        /// <summary>
        /// Synchronizes the internal wishlist with the registry, adding new entries.
        /// </summary>
        private void SyncNewListEntriesInternal()
        {
            for (int i = _surfaces.Count - 1; i >= 0; i--)
            {
                GameObject obj = _surfaces[i];
                if (obj == null) continue;

                int id = obj.GetInstanceID();

                if (!_trackedSurfaces.TryGetValue(id, out bool isManual) || !isManual)
                {
                    TryAddSurfaceInternal(obj, true);
                }
            }
        }

        #endregion

        #region Private Registry Access

        /// <summary>
        /// Resets the internal tracking state and unregisters all surfaces from the shared database.
        /// Does not modify the persistent serialized wishlist (_surfaces).
        /// Clears the dictionary and registry entries while setting the dirty flag, 
        /// but avoids triggering events.
        /// </summary>
        private void ClearStateInternal()
        {
            List<int> ids = new List<int>(_trackedSurfaces.Keys);
            for (int i = 0; i < ids.Count; i++)
            {
                RemoveSurfaceInternal(ids[i]);
            }

            _trackedSurfaces.Clear();
            _isDirty = true;
        }

        /// <summary>
        /// Validates and attempts to add a GameObject to the shared registry.
        /// Updates the tracking dictionary status. Does not modify the serialized list.
        /// </summary>
        /// <param name="obj">The GameObject candidate.</param>
        /// <returns>True if the object passed validation and is now registered.</returns>
        private bool TryAddSurfaceInternal(GameObject obj, bool isManualEntry)
        {
            if (obj == null || _objectRegistry == null) return false;
            int id = obj.GetInstanceID();

            if (_trackedSurfaces.TryGetValue(id, out bool currentlyManual))
            {
                if (isManualEntry && !currentlyManual)
                {
                    _trackedSurfaces[id] = true;
                    _isDirty = true;
                }
                return true;
            }

            if (!TryGetBounds(obj.transform, out _)) return false;

            if (_objectRegistry.TryRegister(obj))
            {
                _trackedSurfaces[id] = isManualEntry;
                _isDirty = true;
                return true;
            }
            return false;
        }

        /// <summary>
        /// Removes a surface from the local tracking set and unregisters it from the shared spatial database.
        /// </summary>
        /// <param name="id">The InstanceID of the GameObject to remove.</param>
        /// <returns>True if the object was found and successfully removed from the registry.</returns>
        private bool RemoveSurfaceInternal(int id)
        {
            if (!_trackedSurfaces.Remove(id)) return false;

            _isDirty = true;
            if (_objectRegistry != null)
            {
                _objectRegistry.Unregister(id);
            }
            return true;
        }

        /// <summary>
        /// Recursively scans the hierarchy to identify and register surfaces.
        /// Efficiently batches changes by setting _isDirty.
        /// </summary>
        /// <param name="root">The starting transform for the recursion.</param>
        private void ScanHierarchyInternal(Transform root)
        {
            TraverseHierarchy(root, (obj) =>
            {
                if (!_trackedSurfaces.ContainsKey(obj.GetInstanceID()))
                {
                    TryAddSurfaceInternal(obj, false);
                }
            });
        }

        #endregion

        #region Helper & Validation Logic

        /// <summary>
        /// Centrally traverses a hierarchy and executes an action for every valid surface candidate.
        /// Prevents code duplication for recursive scanning.
        /// </summary>
        private void TraverseHierarchy(Transform root, Action<GameObject> onCandidateFound)
        {
            if (root == null) return;

            foreach (Transform child in root)
            {
                if (TryGetBounds(child, out var bounds) && FulfillsFilterCriteria(child, bounds))
                {
                    onCandidateFound?.Invoke(child.gameObject);
                }

                if (child.childCount > 0)
                {
                    TraverseHierarchy(child, onCandidateFound);
                }
            }
        }

        /// <summary>
        /// Checks if a specific InstanceID is present in the serialized wishlist.
        /// Simple linear search, safe for editor-time sync.
        /// </summary>
        private bool IsIdInList(int id)
        {
            for (int i = 0; i < _surfaces.Count; i++)
            {
                if (_surfaces[i] != null && _surfaces[i].GetInstanceID() == id)
                {
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Attempts to extract spatial bounds from a candidate. 
        /// Acts as the primary technical compatibility check.
        /// </summary>
        private bool TryGetBounds(Transform t, out Bounds bounds)
        {
            bounds = new Bounds();
            if (t == null) return false;

            if (t.TryGetComponent<Renderer>(out var r))
            {
                bounds = r.bounds;
                return true;
            }

            if (t.TryGetComponent<Collider>(out var c))
            {
                bounds = c.bounds;
                return true;
            }

            return false;
        }

        /// <summary>
        /// Checks if the candidate meets the designer's filter settings.
        /// Only used for automatic discovery, not for manual wishlist entries.
        /// </summary>
        private bool FulfillsFilterCriteria(Transform t, Bounds b)
        {
            if (!string.IsNullOrEmpty(_settings.nameFilter) && !t.name.Contains(_settings.nameFilter))
                return false;

            if (_settings.enableAreaCheck)
            {
                return (b.size.x * b.size.z) > _settings.minAreaThreshold;
            }

            return true;
        }

        #endregion
    }
}