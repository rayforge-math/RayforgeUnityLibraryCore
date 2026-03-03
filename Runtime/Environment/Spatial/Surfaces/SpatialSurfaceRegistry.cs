using Rayforge.Core.Collections.Abstractions;
using Rayforge.Core.Collections.Helpers;
using Rayforge.Core.Environment.Abstractions;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Rayforge.Core.Environment.Spatial.Surfaces
{
    /// <summary>
    /// Manages registration and spatial partitioning by orchestrating multiple typed registries.
    /// Acts as a facade for MeshRenderer and Terrain spatial data.
    /// Now manages the lifecycle of the grid provider events and a shared dirty tracker.
    /// </summary>
    public class SpatialSurfaceRegistry : ISpatialCollection<Vector3Int>
    {
        private const string Tag = "[SurfaceRegistry]";

        #region Fields

        /// <summary> Specialized registry for MeshRenderers. </summary>
        private SpatialObjectRegistry<Vector3Int, MeshRenderer> _meshRegistry;

        /// <summary> Specialized registry for Terrains. </summary>
        private SpatialObjectRegistry<Vector3Int, Terrain> _terrainRegistry;

        /// <summary> 
        /// Centralized tracker shared between sub-registries. 
        /// Ensures that changes in any sub-registry mark the same spatial cell as dirty.
        /// </summary>
        private readonly HashSet<Vector3Int> _sharedDirtyBuckets = new();

        /// <summary> The current grid provider used for coordinate translation. </summary>
        private ISpatialGridProvider<Vector3Int> _gridProvider;

        /// <summary> Gets whether the registry has been initialized with a grid provider. </summary>
        public bool IsInitialized =>
            _gridProvider != null &&
            _meshRegistry != null && _meshRegistry.IsInitialized &&
            _terrainRegistry != null && _terrainRegistry.IsInitialized;

        #endregion

        #region Lifecycle

        /// <summary>
        /// Initializes the orchestrator and sub-registries with the given grid provider.
        /// The provider serves as the master source for the coordinate system; the registry 
        /// automatically subscribes to structural changes (like GridSize) to keep its internal 
        /// buckets synchronized.
        /// </summary>
        /// <param name="gridProvider">The master provider for coordinate and bucket mapping.</param>
        public void Initialize(ISpatialGridProvider<Vector3Int> gridProvider)
        {
            try
            {
                Reset();

                if (gridProvider == null)
                    throw new ArgumentNullException(nameof(gridProvider), "Cannot initialize SurfaceRegistry with a null grid provider.");

                _gridProvider = gridProvider;
                _gridProvider.OnGridStructureChanged += HandleGridStructureChanged;

                _meshRegistry = new SpatialObjectRegistry<Vector3Int, MeshRenderer>();
                _terrainRegistry = new SpatialObjectRegistry<Vector3Int, Terrain>();

                _meshRegistry.Initialize(gridProvider, _sharedDirtyBuckets);
                _terrainRegistry.Initialize(gridProvider, _sharedDirtyBuckets);
            }
            catch (Exception e)
            {
                throw new Exception($"{Tag} Initialization failed: {e.Message}", e);
            }
        }

        /// <summary>
        /// Handles the grid structure change event by triggering a full remap of all sub-registries.
        /// Central event handler that triggers a rebuild for all sub-registries.
        /// </summary>
        /// <param name="provider">The provider that triggered the change.</param>
        private void HandleGridStructureChanged(ISpatialGridProvider<Vector3Int> provider)
        {
            _meshRegistry?.FullRemap();
            _terrainRegistry?.FullRemap();
        }

        /// <summary>
        /// Clears all managed registries and the shared dirty tracker.
        /// </summary>
        public void Clear()
        {
            _meshRegistry?.Clear();
            _terrainRegistry?.Clear();
            _sharedDirtyBuckets?.Clear();
        }

        /// <summary>
        /// Fully de-initializes the registry, detaches events, and nulls out sub-registries.
        /// Use this for a "hard reset" or during teardown.
        /// </summary>
        public void Reset()
        {
            if (_gridProvider != null)
            {
                _gridProvider.OnGridStructureChanged -= HandleGridStructureChanged;
                _gridProvider = null;
            }

            Clear();

            _meshRegistry = null;
            _terrainRegistry = null;
        }

        /// <summary>
        /// Clears the list of modified spatial cells.
        /// Since it's shared, clearing this set clears it for both registries.
        /// </summary>
        public void ClearDirtyCells()
        {
            _sharedDirtyBuckets.Clear();
        }

        #endregion

        #region Registration Logic

        /// <summary>
        /// Identifies relevant components on a GameObject and registers them.
        /// </summary>
        /// <param name="obj">The GameObject to scan for spatial components.</param>
        /// <returns>True if any registration resulted in a spatial change.</returns>
        public bool TryRegister(GameObject obj)
        {
            if (obj == null) return false;

            if (!IsInitialized)
                throw new InvalidOperationException($"{Tag} Registry not initialized!");

            int id = obj.GetInstanceID();
            bool changed = false;

            if (obj.TryGetComponent<MeshRenderer>(out var renderer))
            {
                if (obj.TryGetComponent<MeshFilter>(out var filter) && filter.sharedMesh != null)
                {
                    var newState = SpatialState<MeshRenderer>.Create(_gridProvider.Anchor, renderer);
                    if (_meshRegistry.TryRegister(id, newState)) changed = true;
                }
            }

            if (obj.TryGetComponent<Terrain>(out var terrain) && terrain.terrainData != null)
            {
                var newState = SpatialState<Terrain>.Create(_gridProvider.Anchor, terrain);
                if (_terrainRegistry.TryRegister(id, newState)) changed = true;
            }

            return changed;
        }

        /// <summary>
        /// Removes an object from all internal registries.
        /// </summary>
        /// <param name="id">The InstanceID of the object.</param>
        /// <returns>True if the object was removed from at least one registry.</returns>
        public bool Unregister(int id)
        {
            if (!IsInitialized) return false;

            bool removedMesh = _meshRegistry.Unregister(id);
            bool removedTerrain = _terrainRegistry.Unregister(id);

            return removedMesh || removedTerrain;
        }

        #endregion

        #region ISpatialCollection Implementation

        /// <summary>
        /// Dispatches the iterator request to the correct sub-registry based on type T.
        /// Direct type dispatching for better performance and clarity.
        /// </summary>
        public bool TryGetIterator<T>(Vector3Int key, out IIterator<T> iterator) where T : Component
        {
            iterator = null;
            if (!IsInitialized) return false;


            if (typeof(T) == typeof(MeshRenderer))
            {
                if (_meshRegistry.TryGetIterator(key, out var meshIter))
                {
                    iterator = meshIter as IIterator<T>;
                    return true;
                }
            }

            else if (typeof(T) == typeof(Terrain))
            {
                if (_terrainRegistry.TryGetIterator(key, out var terrainIter))
                {
                    iterator = terrainIter as IIterator<T>;
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Checks if a cell contains any objects, regardless of their component type.
        /// </summary>
        /// <param name="key">The spatial cell coordinate.</param>
        /// <returns>True if at least one registry has entries in this cell.</returns>
        public bool HasEntriesInCell(Vector3Int key)
        {
            if (!IsInitialized) return false;
            return _meshRegistry.HasEntriesInCell(key) || _terrainRegistry.HasEntriesInCell(key);
        }

        /// <summary>
        /// Returns an iterator over all modified cells.
        /// Returns an iterator over the unified shared dirty tracker.
        /// </summary>
        /// <returns>An iterator of modified spatial keys.</returns>
        public IIterator<Vector3Int> GetDirtyCells()
        {
            return _sharedDirtyBuckets.GetEnumerator().ToIterator();
        }

        /// <summary>
        /// Collects all registered IDs from all sub-registries.
        /// </summary>
        /// <returns>A collection of all unique InstanceIDs currently managed.</returns>
        public IIterator<int> GetAllIds()
        {
            if (!IsInitialized)
                return IIterator<int>.Empty();

            return IteratorExtensions.Combine(
                _meshRegistry.AllIds,
                _terrainRegistry.AllIds
            );
        }

        #endregion
    }
}