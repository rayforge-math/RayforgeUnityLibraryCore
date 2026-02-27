using Rayforge.Core.Collections.Abstractions;
using Rayforge.Core.Environment.Abstractions;
using Rayforge.Core.Rendering.Abstractions;
using Rayforge.Core.Rendering.Collections.Buffered;
using Rayforge.Core.Rendering.Collections.Iterator;
using System;

namespace Rayforge.Core.Environment.Spatial
{
    /// <summary>
    /// Generic bridge for registries that participate in a spatial culling and rendering pipeline.
    /// This class enforces the presence of spatial (culling) and visual (rendering) data stores.
    /// </summary>
    /// <typeparam name="TKey">The unique identifier type for the entities (e.g., Vector3Int for Chunks).</typeparam>
    /// <typeparam name="TSpatial">The struct type used for GPU culling (e.g., SphereSpatialData).</typeparam>
    /// <typeparam name="TVisual">The struct type used for GPU rendering (e.g., MatrixSpatialData).</typeparam>
    public abstract class SpatialMetadataRegistry<TKey, TSpatial, TVisual> : MetadataRegistry<TKey>, ISpatialMetadataRegistry
        where TKey : struct, IEquatable<TKey>
        where TSpatial : unmanaged
        where TVisual : unmanaged
    {
        private MetadataStore<TSpatial> m_SpatialStore;
        private MetadataStore<TVisual> m_VisualStore;

        #region Lifecycle & Configuration

        /// <summary>
        /// Initializes a new spatial registry and automatically registers the mandatory spatial and visual stores.
        /// </summary>
        /// <param name="capacity">Maximum number of slots available in the registry.</param>
        /// <param name="batchSize">Size of a single dirty-tracking batch for optimized GPU uploads.</param>
        protected SpatialMetadataRegistry(int capacity, int batchSize) : base(capacity, batchSize)
        {
            SetupStores();
        }

        /// <summary>
        /// Reconfigures the entire registry hierarchy and re-binds the spatial and visual stores.
        /// This ensures that local references point to the new, resized data stores.
        /// </summary>
        /// <returns>True if a structural resize/re-allocation happened; false if only a Clear was performed.</returns>
        public new bool Reconfigure(int newCapacity, int newBatchSize)
        {
            bool changed = base.Reconfigure(newCapacity, newBatchSize);
            SetupStores();
            return changed;
        }

        /// <summary>
        /// Internal helper to bind or re-bind the mandatory stores.
        /// </summary>
        private void SetupStores()
        {
            m_SpatialStore = AddStore<TSpatial>();
            m_VisualStore = AddStore<TVisual>();
        }

        #endregion

        #region ISpatialMetadataRegistry Implementation

        /// <summary>
        /// Grants read-only access to the spatial store via the non-generic interface.
        /// English comment: Useful for external buffer management or debugging.
        /// </summary>
        public IMetadataStore SpatialMetadata => m_SpatialStore;

        /// <summary>
        /// Grants read-only access to the visual store via the non-generic interface.
        /// </summary>
        public IMetadataStore VisualMetadata => m_VisualStore;

        /// <summary>
        /// Gets the specialized iterator for modified spatial data.
        /// English comment: Use this to update the GPU culling buffer.
        /// </summary>
        public IIterator<BufferSegmentMeta> SpatialDirtyIterator => m_SpatialStore.GetDirtyBatchIterator();

        /// <summary>
        /// Gets the specialized iterator for modified visual data.
        /// English comment: Use this to update the GPU rendering/atlas buffer.
        /// </summary>
        public IIterator<BufferSegmentMeta> VisualDirtyIterator => m_VisualStore.GetDirtyBatchIterator();

        /// <summary>
        /// Clears the dirty tracking state for both spatial and visual stores.
        /// English comment: Call this after a full synchronization of both buffers.
        /// </summary>
        public void ClearAllDirty()
        {
            m_SpatialStore.ClearDirty();
            m_VisualStore.ClearDirty();
        }

        /// <summary>
        /// Clears the dirty tracking state only for the spatial store.
        /// English comment: Useful if you sync buffers at different frequencies.
        /// </summary>
        public void ClearSpatialDirty()
        {
            m_SpatialStore.ClearDirty();
        }

        /// <summary>
        /// Clears the dirty tracking state only for the visual store.
        /// </summary>
        public void ClearVisualDirty()
        {
            m_VisualStore.ClearDirty();
        }

        #endregion

        #region Data Access (Setters)

        /// <summary>
        /// Updates both spatial and visual data for a specific key using a single index lookup.
        /// If the key does not exist, a new slot is automatically allocated.
        /// </summary>
        public void SetMetadata(TKey key, TSpatial spatial, TVisual visual)
        {
            int idx = GetOrAllocateIndex(key);
            m_SpatialStore.Set(idx, spatial);
            m_VisualStore.Set(idx, visual);
        }

        /// <summary>
        /// Updates only the spatial/culling data for a key.
        /// </summary>
        public void SetSpatial(TKey key, TSpatial spatial)
        {
            int idx = GetOrAllocateIndex(key);
            m_SpatialStore.Set(idx, spatial);
        }

        /// <summary>
        /// Updates only the visual/atlas data for a key.
        /// </summary>
        public void SetVisual(TKey key, TVisual visual)
        {
            int idx = GetOrAllocateIndex(key);
            m_VisualStore.Set(idx, visual);
        }

        #endregion

        #region Data Access (Getters & State)

        /// <summary>
        /// Tries to retrieve the current spatial and visual data for a given key.
        /// </summary>
        public bool TryGetMetadata(TKey key, out TSpatial spatial, out TVisual visual)
        {
            if (TryGetIndex(key, out int index))
            {
                spatial = m_SpatialStore.Get(index);
                visual = m_VisualStore.Get(index);
                return true;
            }

            spatial = default;
            visual = default;
            return false;
        }

        /// <summary>
        /// Tries to retrieve only the spatial data.
        /// </summary>
        public bool TryGetSpatial(TKey key, out TSpatial spatial)
        {
            if (TryGetIndex(key, out int index))
            {
                spatial = m_SpatialStore.Get(index);
                return true;
            }
            spatial = default;
            return false;
        }

        /// <summary>
        /// Tries to retrieve only the visual data.
        /// </summary>
        public bool TryGetVisual(TKey key, out TVisual visual)
        {
            if (TryGetIndex(key, out int index))
            {
                visual = m_VisualStore.Get(index);
                return true;
            }
            visual = default;
            return false;
        }

        /// <summary>
        /// Checks if a specific key is currently registered and has an allocated slot.
        /// </summary>
        public bool Contains(TKey key) => TryGetIndex(key, out _);

        #endregion

        #region Mass Operations & Template Methods

        /// <summary>
        /// Marks all data in all registered stores as dirty, forcing a full GPU re-upload.
        /// Useful after a graphics context loss or buffer recreation.
        /// </summary>
        public void MarkAllDirty()
        {
            m_SpatialStore.MarkAllDirty();
            m_VisualStore.MarkAllDirty();
        }

        /// <summary>
        /// Fully releases the key and ensures the GPU data is invalidated.
        /// This is the "Template Method" that provides a unified API.
        /// </summary>
        public void ReleaseAndKill(TKey key)
        {
            if (TryGetIndex(key, out int index))
            {
                m_SpatialStore.Set(index, GetInvalidSpatialData());
                Release(key);
            }
        }

        /// <summary>
        /// Must be implemented by child classes to define what "inactive" means for TSpatial.
        /// </summary>
        protected abstract TSpatial GetInvalidSpatialData();

        #endregion
    }
}