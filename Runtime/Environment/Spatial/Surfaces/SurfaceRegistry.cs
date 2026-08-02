using Rayforge.Core.Collections.Abstractions;
using Rayforge.Core.Collections.Helpers;
using Rayforge.Core.Environment.Abstractions;
using Rayforge.Core.Execution.Abstractions;
using Rayforge.Core.Environment.Spatial.Components;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Rayforge.Core.Environment.Spatial.Surfaces
{
    /// <summary>
    /// A high-level facade that orchestrates multiple typed registries (MeshRenderer and Terrain).
    /// Provides component-based spatial access while encapsulating internal ComponentState management.
    /// </summary>
    public class SurfaceRegistry :
        ISpatialRegistry<Vector3Int, MeshRenderer>,
        ISpatialRegistry<Vector3Int, Terrain>,
        ISpatialCollection<Vector3Int>
    {
        #region Fields

        /// <summary> Internal registry for MeshRenderer components. </summary>
        private SpatialComponentRegistry<Vector3Int, MeshRenderer> _meshRegistry;

        /// <summary> Internal registry for Terrain components. </summary>
        private SpatialComponentRegistry<Vector3Int, Terrain> _terrainRegistry;

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

                _meshRegistry = new SpatialComponentRegistry<Vector3Int, MeshRenderer>();
                _terrainRegistry = new SpatialComponentRegistry<Vector3Int, Terrain>();

                _meshRegistry.Initialize(gridProvider);
                _terrainRegistry.Initialize(gridProvider);
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
        private void HandleGridStructureChanged(ISpatialGridConfiguration<Vector3Int> provider)
        {
            _meshRegistry?.FullRemap();
            _terrainRegistry?.FullRemap();
        }

        /// <summary>
        /// Clears all registered objects from the sub-registries.
        /// </summary>
        public void Clear()
        {
            _meshRegistry?.Clear();
            _terrainRegistry?.Clear();
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
        public void ClearDirtyCells()
        {
            _meshRegistry?.ClearDirtyCells();
            _terrainRegistry?.ClearDirtyCells();
        }

        #endregion

        #region Registration Logic

        /// <summary>
        /// Attempts to identify and register valid spatial components (MeshRenderer, Terrain) from the provided GameObject.
        /// </summary>
        /// <param name="obj">The GameObject to inspect and register.</param>
        /// <returns>True if at least one component was successfully added or updated in the registry.</returns>
        public bool TryRegister(GameObject obj)
        {
            if (obj == null || !IsInitialized) return false;

            int id = obj.GetInstanceID();
            bool changed = false;

            if (obj.TryGetComponent<MeshRenderer>(out var renderer))
            {
                if (obj.TryGetComponent<MeshFilter>(out var filter) && filter.sharedMesh != null)
                {
                    var state = ComponentState<MeshRenderer>.Create(_gridProvider.Anchor, renderer);
                    if (_meshRegistry.TryRegister(id, state)) changed = true;
                }
            }

            if (obj.TryGetComponent<Terrain>(out var terrain) && terrain.terrainData != null)
            {
                var state = ComponentState<Terrain>.Create(_gridProvider.Anchor, terrain);
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
            if (!IsInitialized) return;

            var processedCells = new HashSet<Vector3Int>();

            var meshAction = new InternalDirtyCellAction<TAction> { Action = action, Processed = processedCells };
            _meshRegistry.ForEachDirtyCell(ref meshAction);

            var terrainAction = new InternalDirtyCellAction<TAction> { Action = action, Processed = processedCells };
            _terrainRegistry.ForEachDirtyCell(ref terrainAction);

            action = meshAction.Action;
        }

        /// <inheritdoc />
        public IIterator<Vector3Int> GetDirtyCellIterator()
        {
            if (!IsInitialized) return IIterator<Vector3Int>.Empty();
            return IteratorExtensions.Combine(_meshRegistry.GetDirtyCellIterator(), _terrainRegistry.GetDirtyCellIterator());
        }

        #endregion

        #region Helper Structs

        /// <summary>
        /// Internal helper struct to filter duplicate dirty cell callbacks across sub-registries.
        /// </summary>
        private struct InternalDirtyCellAction<TOuterAction> : IExecutionHandler<Vector3Int>
            where TOuterAction : struct, IExecutionHandler<Vector3Int>
        {
            public TOuterAction Action;
            public HashSet<Vector3Int> Processed;

            public void Execute(Vector3Int key)
            {
                if (Processed.Add(key))
                {
                    Action.Execute(key);
                }
            }
        }

        #endregion

        #region Explicit ISpatialRegistry Implementations

        // --- MeshRenderer Implementation ---

        /// <inheritdoc />
        bool ISpatialRegistry<Vector3Int, MeshRenderer>.Contains(int id) =>
            _meshRegistry != null && _meshRegistry.Contains(id);

        /// <inheritdoc />
        bool ISpatialRegistry<Vector3Int, MeshRenderer>.TryGetState(int id, out ComponentState<MeshRenderer> state)
        {
            state = default;
            return _meshRegistry != null && _meshRegistry.TryGetState(id, out state);
        }

        /// <inheritdoc />
        bool ISpatialRegistry<Vector3Int, MeshRenderer>.TryForEachInCell<TAction>(Vector3Int key, ref TAction action) =>
            _meshRegistry != null && _meshRegistry.TryForEachInCell(key, ref action);

        /// <inheritdoc />
        void ISpatialRegistry<Vector3Int, MeshRenderer>.ForEachId<TAction>(ref TAction action) =>
            _meshRegistry?.ForEachId(ref action);

        /// <inheritdoc />
        void ISpatialRegistry<Vector3Int, MeshRenderer>.ForEachKey<TAction>(ref TAction action) =>
            _meshRegistry?.ForEachKey(ref action);

        /// <inheritdoc />
        bool ISpatialRegistry<Vector3Int, MeshRenderer>.TryForEachCellId<TAction>(Vector3Int key, ref TAction action) =>
            _meshRegistry != null && _meshRegistry.TryForEachCellId(key, ref action);

        /// <inheritdoc />
        void ISpatialRegistry<Vector3Int, MeshRenderer>.ForEachState<TAction>(ref TAction action) =>
            _meshRegistry?.ForEachState(ref action);

        /// <inheritdoc />
        void ISpatialRegistry<Vector3Int, MeshRenderer>.ForEachDirtyCell<TAction>(ref TAction action) =>
            _meshRegistry?.ForEachDirtyCell(ref action);

        /// <inheritdoc />
        bool ISpatialRegistry<Vector3Int, MeshRenderer>.TryGetEntryIterator(Vector3Int key, out IIterator<MeshRenderer> iterator)
        {
            iterator = null;
            return _meshRegistry != null && _meshRegistry.TryGetEntryIterator(key, out iterator);
        }

        /// <inheritdoc />
        IIterator<int> ISpatialRegistry<Vector3Int, MeshRenderer>.AllIds =>
            _meshRegistry != null ? _meshRegistry.AllIds : IIterator<int>.Empty();

        /// <inheritdoc />
        IIterator<Vector3Int> ISpatialRegistry<Vector3Int, MeshRenderer>.AllKeys =>
            _meshRegistry != null ? _meshRegistry.AllKeys : IIterator<Vector3Int>.Empty();

        /// <inheritdoc />
        IIterator<ComponentState<MeshRenderer>> ISpatialRegistry<Vector3Int, MeshRenderer>.AllStates =>
            _meshRegistry != null ? _meshRegistry.AllStates : IIterator<ComponentState<MeshRenderer>>.Empty();

        /// <inheritdoc />
        IIterator<int> ISpatialRegistry<Vector3Int, MeshRenderer>.CellIds(Vector3Int key) =>
            _meshRegistry != null ? _meshRegistry.CellIds(key) : IIterator<int>.Empty();

        /// <inheritdoc />
        IIterator<Vector3Int> ISpatialRegistry<Vector3Int, MeshRenderer>.GetDirtyCellIterator() =>
            _meshRegistry != null ? _meshRegistry.GetDirtyCellIterator() : IIterator<Vector3Int>.Empty();


        // --- Terrain Implementation ---

        /// <inheritdoc />
        bool ISpatialRegistry<Vector3Int, Terrain>.Contains(int id) =>
            _terrainRegistry != null && _terrainRegistry.Contains(id);

        /// <inheritdoc />
        bool ISpatialRegistry<Vector3Int, Terrain>.TryGetState(int id, out ComponentState<Terrain> state)
        {
            state = default;
            return _terrainRegistry != null && _terrainRegistry.TryGetState(id, out state);
        }

        /// <inheritdoc />
        bool ISpatialRegistry<Vector3Int, Terrain>.TryForEachInCell<TAction>(Vector3Int key, ref TAction action) =>
            _terrainRegistry != null && _terrainRegistry.TryForEachInCell(key, ref action);

        /// <inheritdoc />
        void ISpatialRegistry<Vector3Int, Terrain>.ForEachId<TAction>(ref TAction action) =>
            _terrainRegistry?.ForEachId(ref action);

        /// <inheritdoc />
        void ISpatialRegistry<Vector3Int, Terrain>.ForEachKey<TAction>(ref TAction action) =>
            _terrainRegistry?.ForEachKey(ref action);

        /// <inheritdoc />
        bool ISpatialRegistry<Vector3Int, Terrain>.TryForEachCellId<TAction>(Vector3Int key, ref TAction action) =>
            _terrainRegistry != null && _terrainRegistry.TryForEachCellId(key, ref action);

        /// <inheritdoc />
        void ISpatialRegistry<Vector3Int, Terrain>.ForEachState<TAction>(ref TAction action) =>
            _terrainRegistry?.ForEachState(ref action);

        /// <inheritdoc />
        void ISpatialRegistry<Vector3Int, Terrain>.ForEachDirtyCell<TAction>(ref TAction action) =>
            _terrainRegistry?.ForEachDirtyCell(ref action);

        /// <inheritdoc />
        bool ISpatialRegistry<Vector3Int, Terrain>.TryGetEntryIterator(Vector3Int key, out IIterator<Terrain> iterator)
        {
            iterator = null;
            return _terrainRegistry != null && _terrainRegistry.TryGetEntryIterator(key, out iterator);
        }

        /// <inheritdoc />
        IIterator<int> ISpatialRegistry<Vector3Int, Terrain>.AllIds =>
            _terrainRegistry != null ? _terrainRegistry.AllIds : IIterator<int>.Empty();

        /// <inheritdoc />
        IIterator<Vector3Int> ISpatialRegistry<Vector3Int, Terrain>.AllKeys =>
            _terrainRegistry != null ? _terrainRegistry.AllKeys : IIterator<Vector3Int>.Empty();

        /// <inheritdoc />
        IIterator<ComponentState<Terrain>> ISpatialRegistry<Vector3Int, Terrain>.AllStates =>
            _terrainRegistry != null ? _terrainRegistry.AllStates : IIterator<ComponentState<Terrain>>.Empty();

        /// <inheritdoc />
        IIterator<int> ISpatialRegistry<Vector3Int, Terrain>.CellIds(Vector3Int key) =>
            _terrainRegistry != null ? _terrainRegistry.CellIds(key) : IIterator<int>.Empty();

        /// <inheritdoc />
        IIterator<Vector3Int> ISpatialRegistry<Vector3Int, Terrain>.GetDirtyCellIterator() =>
            _terrainRegistry != null ? _terrainRegistry.GetDirtyCellIterator() : IIterator<Vector3Int>.Empty();

        #endregion

        #region IReadOnlySpatialCollection<Vector3Int> Implementation

        /// <inheritdoc />
        public int StateCount =>
            (_meshRegistry != null ? _meshRegistry.StateCount : 0) +
            (_terrainRegistry != null ? _terrainRegistry.StateCount : 0);

        /// <inheritdoc />
        public int CellCount =>
            (_meshRegistry != null ? _meshRegistry.CellCount : 0) +
            (_terrainRegistry != null ? _terrainRegistry.CellCount : 0);

        /// <inheritdoc />
        public int DirtyCellCount =>
            (_meshRegistry != null ? _meshRegistry.DirtyCellCount : 0) +
            (_terrainRegistry != null ? _terrainRegistry.DirtyCellCount : 0);

        /// <inheritdoc />
        public int GetCellStateCount(Vector3Int key) =>
            (_meshRegistry != null ? _meshRegistry.GetCellStateCount(key) : 0) +
            (_terrainRegistry != null ? _terrainRegistry.GetCellStateCount(key) : 0);

        #endregion
    }
}