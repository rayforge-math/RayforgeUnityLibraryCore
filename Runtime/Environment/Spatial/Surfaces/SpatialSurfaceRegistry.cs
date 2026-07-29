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
    /// A high-level facade that orchestrates multiple typed registries (MeshRenderer and Terrain).
    /// Provides component-based spatial access while encapsulating internal SpatialState management.
    /// </summary>
    public class SpatialSurfaceRegistry :
        ISpatialRegistry<Vector3Int, MeshRenderer>,
        ISpatialRegistry<Vector3Int, Terrain>
    {
        #region Fields

        /// <summary> Internal registry for MeshRenderer components. </summary>
        private ComponentRegistry<Vector3Int, MeshRenderer> _meshRegistry;

        /// <summary> Internal registry for Terrain components. </summary>
        private ComponentRegistry<Vector3Int, Terrain> _terrainRegistry;

        /// <summary> 
        /// Shared tracker that ensures cell changes in any sub-registry 
        /// mark the same spatial cell as dirty for the orchestrator. 
        /// </summary>
        private readonly HashSet<Vector3Int> _sharedDirtyBuckets = new();

        /// <summary> The provider used for translating world positions to grid coordinates. </summary>
        private ISpatialGridProvider<Vector3Int> _gridProvider;

        /// <summary> 
        /// Gets whether the registry and all its sub-registries are initialized 
        /// and ready for spatial operations. 
        /// </summary>
        public bool IsInitialized =>
            _gridProvider != null &&
            _meshRegistry != null && _meshRegistry.IsInitialized &&
            _terrainRegistry != null && _terrainRegistry.IsInitialized;

        #endregion

        #region Metadata Access (Expert API)

        /// <summary> Provides access to MeshRenderer metadata including precomputed bounds. </summary>
        public ISpatialRegistry<Vector3Int, MeshRenderer> MeshRegistry => _meshRegistry;

        /// <summary> Provides access to Terrain metadata including precomputed bounds. </summary>
        public ISpatialRegistry<Vector3Int, Terrain> TerrainRegistry => _terrainRegistry;

        #endregion

        #region Lifecycle

        /// <summary>
        /// Initializes the orchestrator and sub-registries with the given grid provider.
        /// </summary>
        /// <param name="gridProvider">The master provider for coordinate and bucket mapping.</param>
        public void Initialize(ISpatialGridProvider<Vector3Int> gridProvider)
        {
            try
            {
                Reset();
                _gridProvider = gridProvider ?? throw new ArgumentNullException(nameof(gridProvider));
                _gridProvider.OnGridStructureChanged += HandleGridStructureChanged;

                _meshRegistry = new ComponentRegistry<Vector3Int, MeshRenderer>();
                _terrainRegistry = new ComponentRegistry<Vector3Int, Terrain>();

                _meshRegistry.Initialize(gridProvider, _sharedDirtyBuckets);
                _terrainRegistry.Initialize(gridProvider, _sharedDirtyBuckets);
            }
            catch (Exception e)
            {
                throw new Exception($"SurfaceRegistry initialization failed: {e.Message}", e);
            }
        }

        /// <summary>
        /// Handles structural grid updates (e.g., origin shifts) 
        /// by remapping all registered objects to their new grid coordinates.
        /// </summary>
        /// <param name="provider">The grid provider that triggered the change.</param>
        private void HandleGridStructureChanged(ISpatialGridProvider<Vector3Int> provider)
        {
            _meshRegistry?.FullRemap();
            _terrainRegistry?.FullRemap();
        }

        /// <summary>
        /// Clears all registered objects from the sub-registries and resets the shared dirty state.
        /// </summary>
        public void Clear()
        {
            _meshRegistry?.Clear();
            _terrainRegistry?.Clear();
            _sharedDirtyBuckets.Clear();
        }

        /// <summary>
        /// Shuts down the registry, unhooking from grid events and releasing all internal references.
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

        /// <inheritdoc />
        /// <remarks>
        /// This implementation clears the shared dirty bucket set used by all sub-registries.
        /// </remarks>
        public void ClearDirtyCells() => _sharedDirtyBuckets.Clear();

        #endregion

        #region Registration Logic

        /// <summary>
        /// Attempts to identify and register valid spatial components (MeshRenderer, Terrain) from the provided GameObject.
        /// </summary>
        /// <param name="obj">The GameObject to inspect and register.</param>
        /// <returns>True if at least one component was successfully added or updated in the registry.</returns>
        /// <remarks>
        /// MeshRenderers are only registered if a valid MeshFilter with assigned mesh is present.
        /// Terrains require valid TerrainData to be considered for registration.
        /// </remarks>
        public bool TryRegister(GameObject obj)
        {
            if (obj == null || !IsInitialized) return false;

            int id = obj.GetInstanceID();
            bool changed = false;

            if (obj.TryGetComponent<MeshRenderer>(out var renderer))
            {
                if (obj.TryGetComponent<MeshFilter>(out var filter) && filter.sharedMesh != null)
                {
                    var state = SpatialState<MeshRenderer>.Create(_gridProvider.Anchor, renderer);
                    if (_meshRegistry.TryRegister(id, state)) changed = true;
                }
            }

            if (obj.TryGetComponent<Terrain>(out var terrain) && terrain.terrainData != null)
            {
                var state = SpatialState<Terrain>.Create(_gridProvider.Anchor, terrain);
                if (_terrainRegistry.TryRegister(id, state)) changed = true;
            }

            return changed;
        }

        /// <summary>
        /// Removes an object from all internal sub-registries using its unique Instance ID.
        /// </summary>
        /// <param name="id">The Unity InstanceID of the GameObject to unregister.</param>
        /// <returns>True if the object was found and removed from any sub-registry.</returns>
        public bool Unregister(int id)
        {
            if (!IsInitialized) return false;
            return _meshRegistry.Unregister(id) || _terrainRegistry.Unregister(id);
        }

        #endregion

        #region ISpatialCollection<Vector3Int> Implementation

        /// <inheritdoc />
        public bool IsCellActive(Vector3Int key)
        {
            return (_meshRegistry != null && _meshRegistry.IsCellActive(key)) ||
                   (_terrainRegistry != null && _terrainRegistry.IsCellActive(key));
        }

        /// <inheritdoc />
        public void ForEachCell<TAction>(ref TAction action) 
            where TAction : struct, IExecutionHandler<Vector3Int>
        {
            if (!IsInitialized) return;
            _meshRegistry.ForEachCell(ref action);
            _terrainRegistry.ForEachCell(ref action);
        }

        /// <inheritdoc />
        public IIterator<Vector3Int> GetCellIterator()
        {
            if (!IsInitialized) return IIterator<Vector3Int>.Empty();
            return IteratorExtensions.Combine(_meshRegistry.GetCellIterator(), _terrainRegistry.GetCellIterator());
        }

        /// <inheritdoc />
        public void ForEachDirtyCell<TAction>(ref TAction action) 
            where TAction : struct, IExecutionHandler<Vector3Int>
        {
            foreach (var key in _sharedDirtyBuckets)
                action.Execute(key);
        }

        /// <inheritdoc />
        public IIterator<Vector3Int> GetDirtyCellIterator()
        {
            return _sharedDirtyBuckets.GetEnumerator().ToIterator();
        }

        #endregion

        #region Explicit ISpatialRegistry Implementations

        // --- MeshRenderer Implementation ---

        /// <inheritdoc />
        bool ISpatialRegistry<Vector3Int, MeshRenderer>.TryForEachInCell<TAction>(Vector3Int key, ref TAction action)
        {
            return _meshRegistry != null && _meshRegistry.TryForEachInCell(key, ref action);
        }

        /// <inheritdoc />
        bool ISpatialRegistry<Vector3Int, MeshRenderer>.TryGetEntryIterator(Vector3Int key, out IIterator<MeshRenderer> iterator)
        {
            iterator = null;
            return _meshRegistry != null && _meshRegistry.TryGetEntryIterator(key, out iterator);
        }

        // --- Terrain Implementation ---

        /// <inheritdoc />
        bool ISpatialRegistry<Vector3Int, Terrain>.TryForEachInCell<TAction>(Vector3Int key, ref TAction action)
        {
            return _terrainRegistry != null && _terrainRegistry.TryForEachInCell(key, ref action);
        }

        /// <inheritdoc />
        bool ISpatialRegistry<Vector3Int, Terrain>.TryGetEntryIterator(Vector3Int key, out IIterator<Terrain> iterator)
        {
            iterator = null;
            return _terrainRegistry != null && _terrainRegistry.TryGetEntryIterator(key, out iterator);
        }

        #endregion
    }
}