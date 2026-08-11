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
    public class SurfaceRegistry : ISpatialCollection<Vector3Int>
    {
        #region Fields

        /// <summary> Internal registry for MeshRenderer components. </summary>
        private SpatialComponentRegistry<Vector3Int, MeshRenderer> m_MeshRegistry;

        /// <summary> Internal registry for Terrain components. </summary>
        private SpatialComponentRegistry<Vector3Int, Terrain> m_TerrainRegistry;

        /// <summary> The provider used for translating world positions to grid coordinates. </summary>
        private ISpatialGridConfiguration<Vector3Int> m_GridProvider;

        /// <summary> 
        /// Gets whether the registry and all its sub-registries are initialized 
        /// and ready for spatial operations. 
        /// </summary>
        public bool IsInitialized =>
            m_GridProvider != null &&
            m_MeshRegistry != null && m_MeshRegistry.IsInitialized &&
            m_TerrainRegistry != null && m_TerrainRegistry.IsInitialized;

        #endregion

        #region Metadata Access

        /// <summary> Provides access to MeshRenderer metadata including precomputed bounds. </summary>
        public ISpatialRegistry<Vector3Int, MeshRenderer> MeshRegistry => m_MeshRegistry;

        /// <summary> Provides access to Terrain metadata including precomputed bounds. </summary>
        public ISpatialRegistry<Vector3Int, Terrain> TerrainRegistry => m_TerrainRegistry;

        #endregion

        #region Lifecycle

        /// <summary>
        /// Initializes the orchestrator and sub-registries with the given grid provider.
        /// </summary>
        /// <param name="gridProvider">The master provider for coordinate and bucket mapping.</param>
        public void Initialize(ISpatialGridProvider<Vector3Int> gridProvider)
        {
            if (gridProvider == null)
            {
                throw new ArgumentNullException(
                    nameof(gridProvider),
                    "A valid spatial grid provider must be provided to initialize the surface registry."
                );
            }

            Reset();
            m_GridProvider = gridProvider;
            m_GridProvider.OnGridStructureChanged += HandleGridStructureChanged;

            m_MeshRegistry = new SpatialComponentRegistry<Vector3Int, MeshRenderer>();
            m_TerrainRegistry = new SpatialComponentRegistry<Vector3Int, Terrain>();

            m_MeshRegistry.Initialize(gridProvider);
            m_TerrainRegistry.Initialize(gridProvider);
        }

        /// <summary>
        /// Handles structural grid updates (e.g., origin shifts) 
        /// by remapping all registered objects to their new grid coordinates.
        /// </summary>
        /// <param name="provider">The grid provider that triggered the change.</param>
        private void HandleGridStructureChanged(ISpatialGridConfiguration<Vector3Int> provider)
        {
            m_MeshRegistry?.FullRemap();
            m_TerrainRegistry?.FullRemap();
        }

        /// <summary>
        /// Clears all registered objects from the sub-registries.
        /// </summary>
        public void Clear()
        {
            m_MeshRegistry?.Clear();
            m_TerrainRegistry?.Clear();
        }

        /// <summary>
        /// Shuts down the registry, unhooking from grid events and releasing all internal references.
        /// </summary>
        public void Reset()
        {
            if (m_GridProvider != null)
            {
                m_GridProvider.OnGridStructureChanged -= HandleGridStructureChanged;
                m_GridProvider = null;
            }
            Clear();
            m_MeshRegistry = null;
            m_TerrainRegistry = null;
        }

        /// <inheritdoc />
        public void ClearDirtyCells()
        {
            m_MeshRegistry?.ClearDirtyCells();
            m_TerrainRegistry?.ClearDirtyCells();
        }

        #endregion

        #region Registration Logic

        /// <summary>
        /// Attempts to identify and register valid spatial components (MeshRenderer, Terrain) from the provided GameObject.
        /// </summary>
        /// <param name="obj">The GameObject to inspect and register.</param>
        /// <returns>True if at least one component was successfully added or updated in the registry.</returns>
        /// <exception cref="InvalidOperationException">Thrown if the registry is not initialized.</exception>
        public bool TryRegister(GameObject obj)
        {
            if (!IsInitialized)
            {
                throw new InvalidOperationException("Registry is not initialized. Call Initialize() first.");
            }

            if (obj == null) return false;

            int id = obj.GetInstanceID();
            bool changed = false;

            if (obj.TryGetComponent<MeshRenderer>(out var renderer))
            {
                if (obj.TryGetComponent<MeshFilter>(out var filter) && filter.sharedMesh != null)
                {
                    var state = ComponentState<MeshRenderer>.Create(m_GridProvider.Anchor, renderer);
                    if (m_MeshRegistry.TryRegister(id, state)) changed = true;
                }
            }

            if (obj.TryGetComponent<Terrain>(out var terrain) && terrain.terrainData != null)
            {
                var state = ComponentState<Terrain>.Create(m_GridProvider.Anchor, terrain);
                if (m_TerrainRegistry.TryRegister(id, state)) changed = true;
            }

            return changed;
        }

        /// <summary>
        /// Removes an object from all internal sub-registries using its unique Instance ID.
        /// </summary>
        /// <param name="id">The Unity InstanceID of the GameObject to unregister.</param>
        /// <returns>True if the object was found and removed from any sub-registry.</returns>
        /// <exception cref="InvalidOperationException">Thrown if the registry is not initialized.</exception>
        public bool Unregister(int id)
        {
            if (!IsInitialized)
            {
                throw new InvalidOperationException("Registry is not initialized. Call Initialize() first.");
            }

            return m_MeshRegistry.Unregister(id) || m_TerrainRegistry.Unregister(id);
        }

        #endregion

        #region ISpatialCollection<Vector3Int> Implementation

        /// <inheritdoc />
        public bool IsCellActive(Vector3Int key)
        {
            if (!IsInitialized)
            {
                throw new InvalidOperationException("Registry is not initialized. Call Initialize() first.");
            }

            return (m_MeshRegistry != null && m_MeshRegistry.IsCellActive(key)) ||
                   (m_TerrainRegistry != null && m_TerrainRegistry.IsCellActive(key));
        }

        /// <inheritdoc />
        public void ForEachCell<TAction>(ref TAction action)
            where TAction : struct, IExecutionHandler<Vector3Int>
        {
            if (!IsInitialized)
            {
                throw new InvalidOperationException("Registry is not initialized. Call Initialize() first.");
            }

            m_MeshRegistry.ForEachCell(ref action);
            m_TerrainRegistry.ForEachCell(ref action);
        }

        /// <inheritdoc />
        public IIterator<Vector3Int> GetCellIterator()
        {
            if (!IsInitialized)
            {
                throw new InvalidOperationException("Registry is not initialized. Call Initialize() first.");
            }

            return IteratorExtensions.Combine(m_MeshRegistry.GetCellIterator(), m_TerrainRegistry.GetCellIterator());
        }

        /// <inheritdoc />
        public void ForEachDirtyCell<TAction>(ref TAction action)
            where TAction : struct, IExecutionHandler<Vector3Int>
        {
            if (!IsInitialized)
            {
                throw new InvalidOperationException("Registry is not initialized. Call Initialize() first.");
            }

            var processedCells = new HashSet<Vector3Int>();

            var meshAction = new DirtyCellAction<TAction> { Action = action, Processed = processedCells };
            m_MeshRegistry.ForEachDirtyCell(ref meshAction);

            var terrainAction = new DirtyCellAction<TAction> { Action = action, Processed = processedCells };
            m_TerrainRegistry.ForEachDirtyCell(ref terrainAction);

            action = meshAction.Action;
        }

        /// <inheritdoc />
        public IIterator<Vector3Int> GetDirtyCellIterator()
        {
            if (!IsInitialized)
            {
                throw new InvalidOperationException("Registry is not initialized. Call Initialize() first.");
            }

            return IteratorExtensions.Combine(m_MeshRegistry.GetDirtyCellIterator(), m_TerrainRegistry.GetDirtyCellIterator());
        }

        #endregion

        #region IReadOnlySpatialCollection<Vector3Int> Implementation

        /// <inheritdoc />
        public int StateCount
        {
            get
            {
                if (!IsInitialized)
                {
                    throw new InvalidOperationException("Registry is not initialized. Call Initialize() first.");
                }
                return (m_MeshRegistry != null ? m_MeshRegistry.StateCount : 0) +
                       (m_TerrainRegistry != null ? m_TerrainRegistry.StateCount : 0);
            }
        }

        /// <inheritdoc />
        public int CellCount
        {
            get
            {
                if (!IsInitialized)
                {
                    throw new InvalidOperationException("Registry is not initialized. Call Initialize() first.");
                }
                return (m_MeshRegistry != null ? m_MeshRegistry.CellCount : 0) +
                       (m_TerrainRegistry != null ? m_TerrainRegistry.CellCount : 0);
            }
        }

        /// <inheritdoc />
        public int DirtyCellCount
        {
            get
            {
                if (!IsInitialized)
                {
                    throw new InvalidOperationException("Registry is not initialized. Call Initialize() first.");
                }
                return (m_MeshRegistry != null ? m_MeshRegistry.DirtyCellCount : 0) +
                       (m_TerrainRegistry != null ? m_TerrainRegistry.DirtyCellCount : 0);
            }
        }

        /// <inheritdoc />
        public int GetCellStateCount(Vector3Int key)
        {
            if (!IsInitialized)
            {
                throw new InvalidOperationException("Registry is not initialized. Call Initialize() first.");
            }
            return (m_MeshRegistry != null ? m_MeshRegistry.GetCellStateCount(key) : 0) +
                   (m_TerrainRegistry != null ? m_TerrainRegistry.GetCellStateCount(key) : 0);
        }

        #endregion

        #region Helper Structs

        /// <summary>
        /// Internal helper struct to filter duplicate dirty cell callbacks across sub-registries.
        /// </summary>
        private struct DirtyCellAction<TOuterAction> : IExecutionHandler<Vector3Int>
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
    }
}