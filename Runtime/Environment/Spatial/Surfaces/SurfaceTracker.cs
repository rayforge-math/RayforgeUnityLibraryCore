using Rayforge.Core.Collections.Abstractions;
using Rayforge.Core.Collections.Helpers;
using Rayforge.Core.Environment.Abstractions;
using Rayforge.Core.Execution.Abstractions;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Rayforge.Core.Environment.Spatial.Surfaces
{
    /// <summary>
    /// Manages the detection and synchronization of world surfaces.
    /// It maintains an internal serialized list and its own settings, 
    /// syncing everything with an internally owned SpatialComponentRegistry.
    /// </summary>
    [Serializable]
    public class SurfaceTracker
    {
        #region Serialized Fields

        [SerializeField]
        private SurfaceTrackerSettings _settings = SurfaceTrackerSettings.Default;
        private SurfaceTrackerSettings _activeSettings = SurfaceTrackerSettings.Default;

        [SerializeField]
        [Tooltip("Internal list of validated surfaces. Use TrackedSurfaces to access this externally.")]
        private List<GameObject> _surfaces = new List<GameObject>();

        #endregion

        #region Public Members

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
        /// True if the tracker is connected to a valid and initialized registry. 
        /// </summary>
        public bool IsInitialized => _objectRegistry != null && _objectRegistry.IsInitialized;

        #endregion

        #region Public Events

        /// <summary> Event triggered on registry changes. </summary>
        public event Action<SurfaceTracker> OnSurfacesChanged;

        /// <summary> 
        /// Event triggered when tracking settings (filters, scan rules) are modified. 
        /// Useful for refreshing visualizers or forcing a re-scan.
        /// </summary>
        public event Action<SurfaceTracker> OnSettingsChanged;

        #endregion

        #region Public Configuration

        /// <summary>
        /// Returns true if the current Settings differ from the ActiveSettings.
        /// Use this to check if ApplySettings() needs to be called.
        /// </summary>
        public bool SettingsDirty => !_activeSettings.Equals(_settings);

        /// <summary>
        /// Provides access to the currently applied settings. (Read-only)
        /// Use this to see what rules the tracker is actually following right now.
        /// </summary>
        public SurfaceTrackerSettings ActiveSettings => _activeSettings;

        /// <summary>
        /// Gets or sets the current tracker settings. 
        /// Changing this does not automatically trigger a re-validation. 
        /// Call ApplySettings() to synchronize the registry with these new rules.
        /// </summary>
        public SurfaceTrackerSettings Settings
        {
            get => _settings;
            set => _settings = value;
        }

        /// <summary>
        /// Synchronizes the current tracking state with the current settings.
        /// Re-validates existing surfaces and triggers the OnSettingsChanged event.
        /// </summary>
        public void ApplySettings()
        {
            if (!SettingsDirty) return;

            _activeSettings = _settings;
            RebuildRegistry();

            OnSettingsChanged?.Invoke(this);
        }

        #endregion

        #region Init

        /// <summary>
        /// Connects the tracker to an external spatial registry and synchronizes existing data.
        /// </summary>
        /// <param name="externalRegistry">The shared spatial database to populate.</param>
        /// <param name="root">The root transform to start the hierarchy scan from.</param>
        public void Initialize(SurfaceRegistry externalRegistry, Transform root)
        {
            ClearStateInternal();

            _objectRegistry = externalRegistry;
            _root = root;
            _activeSettings = _settings;
        }

        /// <summary>
        /// Performs a complete rebuild of the tracker state by syncing the list and scanning hierarchy.
        /// </summary>
        /// <exception cref="InvalidOperationException">Thrown if the tracker is not initialized.</exception>
        /// <returns>True if any surfaces are currently being tracked after the rebuild.</returns>
        public bool RebuildRegistry()
        {
            if (!IsInitialized)
            {
                throw new InvalidOperationException("Tracker is not initialized. Call Initialize() first.");
            }

            ClearStateInternal();

            SyncListToTrackingInternal();

            if (_activeSettings.scanHierarchy && _root != null)
            {
                ScanHierarchyInternal(_root);
            }
            NotifySurfacesChanged();
            return TotalTrackedCount > 0;
        }

        #endregion

        #region Private Runtime State

        /// <summary> The shared spatial database where validated surfaces are stored. </summary>
        private SurfaceRegistry _objectRegistry;

        /// <summary> Cached root transform. </summary>
        private Transform _root;

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
        private void NotifySurfacesChanged()
        {
            if (_isDirty)
            {
                _isDirty = false;
                OnSurfacesChanged?.Invoke(this);
            }
        }

        #endregion

        #region Runtime

        /// <summary>
        /// Attempts to register a GameObject as a dynamic surface.
        /// Validation is performed based on the current tracker settings.
        /// Fires the OnChanged event if the object was successfully registered.
        /// </summary>
        /// <param name="obj">The GameObject candidate to track.</param>
        /// <returns>True if the object passed validation and was added to the registry.</returns>
        public bool TryAddSurface(GameObject obj)
        {
            if (!IsInitialized)
            {
                throw new InvalidOperationException("Tracker is not initialized. Call Initialize() first.");
            }

            bool added = TryAddSurfaceInternal(obj, false);
            if (added) NotifySurfacesChanged();
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
            if (!IsInitialized)
            {
                throw new InvalidOperationException("Tracker is not initialized. Call Initialize() first.");
            }

            bool removed = RemoveSurfaceInternal(id);
            if (removed) NotifySurfacesChanged();
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
            NotifySurfacesChanged();
        }

        /// <summary>
        /// Wipes ALL data, including the persistent wishlist (_surfaces) and the registry.
        /// WARNING: This permanently clears the user's manual configuration.
        /// </summary>
        public void ClearAll()
        {
            ClearStateInternal();
            _surfaces?.Clear();
            NotifySurfacesChanged();
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
            NotifySurfacesChanged();
        }

        #endregion

        #region Iteration

        /// <summary>
        /// Returns a read-only view of the currently tracked surfaces managed by this tracker.
        /// </summary>
        public IIterator<GameObject> GetTrackedSurfaceIterator()
        {
            if (!IsInitialized)
            {
                throw new InvalidOperationException("Tracker is not initialized. Call Initialize() first.");
            }

            return _surfaces.GetEnumerator().ToIterator();
        }

        /// <summary>
        /// Executes the given action on each tracked surface without allocations.
        /// </summary>
        /// <typeparam name="TAction">The execution handler type.</typeparam>
        /// <param name="action">The action to execute for each surface.</param>
        public void ForEachTrackedSurface<TAction>(ref TAction action)
            where TAction : struct, IExecutionHandler<GameObject>
        {
            if (!IsInitialized)
            {
                throw new InvalidOperationException("Tracker is not initialized. Call Initialize() first.");
            }

            for (int i = 0; i < _surfaces.Count; i++)
            {
                GameObject surface = _surfaces[i];
                if (surface != null)
                {
                    action.Execute(surface);
                }
            }
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
            if (!IsInitialized)
            {
                throw new InvalidOperationException("Tracker is not initialized. Call Initialize() first.");
            }

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
            if (_surfaces != null && _surfaces.Count > 0)
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
            int removed = _surfaces?.RemoveAll(s => s == null) ?? 0;
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
            if (!string.IsNullOrEmpty(_activeSettings.nameFilter) && !t.name.Contains(_activeSettings.nameFilter))
                return false;

            if (_activeSettings.enableAreaCheck)
            {
                return (b.size.x * b.size.z) > _activeSettings.minAreaThreshold;
            }

            return true;
        }

        #endregion
    }
}