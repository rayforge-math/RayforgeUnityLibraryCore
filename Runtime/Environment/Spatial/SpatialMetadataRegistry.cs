using Rayforge.Core.Environment.Abstractions;
using Rayforge.Core.Rendering.Abstractions;
using Rayforge.Core.Rendering.Collections.Buffered;
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
        private readonly MetadataStore<TSpatial> m_SpatialStore;
        private readonly MetadataStore<TVisual> m_VisualStore;

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
        /// Initializes a new spatial registry and automatically registers the mandatory spatial and visual stores.
        /// </summary>
        /// <param name="capacity">Maximum number of slots available in the registry.</param>
        /// <param name="batchSize">Size of a single dirty-tracking batch for optimized GPU uploads.</param>
        protected SpatialMetadataRegistry(int capacity, int batchSize) : base(capacity, batchSize)
        {
            m_SpatialStore = AddStore<TSpatial>();
            m_VisualStore = AddStore<TVisual>();
        }

        /// <summary>
        /// Updates both spatial and visual data for a specific key using a single index lookup.
        /// If the key does not exist, a new slot is automatically allocated.
        /// </summary>
        /// <param name="key">The unique identifier of the entity.</param>
        /// <param name="spatial">The new spatial/culling data.</param>
        /// <param name="visual">The new visual/rendering data.</param>
        public void SetMetadata(TKey key, TSpatial spatial, TVisual visual)
        {
            int idx = GetOrAllocateIndex(key);
            m_SpatialStore.Set(idx, spatial);
            m_VisualStore.Set(idx, visual);
        }

        /// <summary>
        /// Updates only the spatial/culling data for a key.
        /// Useful for moving objects that don't change their appearance.
        /// </summary>
        /// <param name="key">The unique identifier of the entity.</param>
        /// <param name="spatial">The new spatial/culling data.</param>
        public void SetSpatial(TKey key, TSpatial spatial)
        {
            int idx = GetOrAllocateIndex(key);
            m_SpatialStore.Set(idx, spatial);
        }

        /// <summary>
        /// Updates only the visual/atlas data for a key.
        /// Useful for LOD swaps or texture updates where the position stays fixed.
        /// </summary>
        /// <param name="key">The unique identifier of the entity.</param>
        /// <param name="visual">The new visual/rendering data.</param>
        public void SetVisual(TKey key, TVisual visual)
        {
            int idx = GetOrAllocateIndex(key);
            m_VisualStore.Set(idx, visual);
        }

        /// <summary>
        /// Tries to retrieve the current spatial and visual data for a given key.
        /// </summary>
        /// <param name="key">The unique identifier of the entity.</param>
        /// <param name="spatial">The current spatial data (out).</param>
        /// <param name="visual">The current visual data (out).</param>
        /// <returns>True if the key was found and data was retrieved.</returns>
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
        /// <param name="key">The unique identifier of the entity.</param>
        /// <param name="spatial">The current spatial data (out).</param>
        /// <returns>True if the key was found and data was retrieved.</returns>
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
        /// <param name="key">The unique identifier of the entity.</param>
        /// <param name="visual">The current visual data (out).</param>
        /// <returns>True if the key was found and data was retrieved.</returns>
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
        /// Iterates through all dirty segments of the internal stores and invokes the provided callbacks.
        /// This allows external systems to synchronize GPU buffers without the registry knowing about hardware resources.
        /// </summary>
        /// <param name="onSpatialChanged">Callback invoked for modified spatial data ranges (sourceArray, offset, count).</param>
        /// <param name="onVisualChanged">Callback invoked for modified visual data ranges (sourceArray, offset, count).</param>
        public void ExtractChanges(Action<Array, int, int> onSpatialChanged, Action<Array, int, int> onVisualChanged)
        {
            m_SpatialStore.ProcessDirtyBatches(onSpatialChanged);
            m_VisualStore.ProcessDirtyBatches(onVisualChanged);
        }

        /// <summary>
        /// Checks if a specific key is currently registered and has an allocated slot.
        /// </summary>
        /// <param name="key">The key to check.</param>
        /// <returns>True if the key exists in the registry.</returns>
        public bool Contains(TKey key) => TryGetIndex(key, out _);

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
    }
}