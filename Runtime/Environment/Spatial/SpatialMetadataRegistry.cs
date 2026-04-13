using Rayforge.Core.Collections.Abstractions;
using Rayforge.Core.Collections.Buffered;
using Rayforge.Core.Collections.Iterator;
using Rayforge.Core.Environment.Abstractions;
using Rayforge.Core.Execution.Abstractions;
using System;

namespace Rayforge.Core.Environment.Spatial
{
    /// <summary>
    /// Generic bridge for registries that participate in a spatial culling and rendering pipeline.
    /// This class enforces the presence of spatial (culling) and visual (rendering) data stores.
    /// </summary>
    /// <typeparam name="TKey">The unique identifier type for the entities (e.g., Vector3Int for Chunks).</typeparam>
    /// <typeparam name="TCulling">The struct type used for GPU culling (e.g., SphereSpatialData).</typeparam>
    /// <typeparam name="TRender">The struct type used for GPU rendering (e.g., MatrixSpatialData).</typeparam>
    public abstract class SpatialMetadataRegistry<TKey, TCulling, TRender> : MetadataRegistry<TKey>, ISpatialMetadataRegistry
        where TKey : struct, IEquatable<TKey>
        where TCulling : unmanaged
        where TRender : unmanaged
    {
        #region Members

        private MetadataStore<TCulling> m_CullingStore;
        private MetadataStore<TRender> m_RenderStore;

        #endregion

        #region ISpatialMetadataRegistry Properties

        /// <inheritdoc />
        public IMetadataStore CullingMetadata => m_CullingStore;

        /// <inheritdoc />
        public IMetadataStore RenderMetadata => m_RenderStore;

        #endregion

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
            m_CullingStore = AddStore<TCulling>();
            m_RenderStore = AddStore<TRender>();
        }

        #endregion

        #region Data Access (Setters)

        /// <summary>
        /// Updates both spatial and visual data for a specific key using a single index lookup.
        /// If the key does not exist, a new slot is automatically allocated.
        /// </summary>
        /// <returns>The absolute index in the metadata stores.</returns>
        public int SetMetadata(TKey key, TCulling culling, TRender render)
        {
            int idx = GetOrAllocateIndex(key);
            m_CullingStore.Set(idx, culling);
            m_RenderStore.Set(idx, render);
            return idx;
        }

        /// <summary>
        /// Updates only the spatial/culling data for a key.
        /// </summary>
        /// <returns>The absolute index in the metadata stores.</returns>
        public int SetCulling(TKey key, TCulling culling)
        {
            int idx = GetOrAllocateIndex(key);
            m_CullingStore.Set(idx, culling);
            return idx;
        }

        /// <summary>
        /// Updates only the visual/atlas data for a key.
        /// </summary>
        /// <returns>The absolute index in the metadata stores.</returns>
        public int SetRender(TKey key, TRender render)
        {
            int idx = GetOrAllocateIndex(key);
            m_RenderStore.Set(idx, render);
            return idx;
        }

        #endregion

        #region Data Access (Getters & State)

        /// <summary>
        /// Tries to retrieve the current spatial and visual data for a given key.
        /// </summary>
        public bool TryGetMetadata(TKey key, out TCulling culling, out TRender render)
        {
            if (TryGetIndex(key, out int index))
            {
                culling = m_CullingStore.Get(index);
                render = m_RenderStore.Get(index);
                return true;
            }
            culling = default;
            render = default;
            return false;
        }

        /// <summary>
        /// Tries to retrieve only the spatial data.
        /// </summary>
        public bool TryGetCulling(TKey key, out TCulling spatial)
        {
            if (TryGetIndex(key, out int index))
            {
                spatial = m_CullingStore.Get(index);
                return true;
            }
            spatial = default;
            return false;
        }

        /// <summary>
        /// Tries to retrieve only the visual data.
        /// </summary>
        public bool TryGetRender(TKey key, out TRender visual)
        {
            if (TryGetIndex(key, out int index))
            {
                visual = m_RenderStore.Get(index);
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

        #region Mass Operations & Management

        /// <summary>
        /// Marks all data in all registered stores as dirty, forcing a full GPU re-upload.
        /// Useful after a graphics context loss or buffer recreation.
        /// </summary>
        public void MarkAllDirty()
        {
            m_CullingStore.MarkAllDirty();
            m_RenderStore.MarkAllDirty();
        }

        /// <summary>
        /// Fully releases the key and ensures the GPU data is invalidated.
        /// This is the "Template Method" that provides a unified API.
        /// </summary>
        /// <returns>The index that was released, or -1 if the key was not found.</returns>
        public int ReleaseAndKill(TKey key)
        {
            if (TryGetIndex(key, out int index))
            {
                m_CullingStore.Set(index, GetInvalidCullingData());
                return Release(key);
            }
            return -1;
        }

        /// <summary>
        /// Must be implemented by child classes to define what "inactive" means for TSpatial.
        /// </summary>
        protected abstract TCulling GetInvalidCullingData();

        #endregion

        #region ISpatialMetadataRegistry Implementation

        #region High-Performance Sync (Zero-Allocation)

        /// <inheritdoc />
        public void ForEachCullingDirty<TAction>(ref TAction action, bool merge = true)
            where TAction : struct, IExecutionHandler<BufferSegmentMeta>
        {
            m_CullingStore.ForEachDirtySegment(ref action, merge);
        }

        /// <inheritdoc />
        public void ForEachRenderDirty<TAction>(ref TAction action, bool merge = true)
            where TAction : struct, IExecutionHandler<BufferSegmentMeta>
        {
            m_RenderStore.ForEachDirtySegment(ref action, merge);
        }

        /// <inheritdoc />
        public void ForEachSyncedDirty<TAction>(ref TAction action, int batchesPerWindow = 1)
            where TAction : struct, IExecutionHandler<SyncedBufferSegmentMeta>
        {
            int effectiveBatchCount = Math.Max(1, batchesPerWindow);
            int windowSizeInElements = effectiveBatchCount * BatchSize;

            var cullingScanner = m_CullingStore.GetDirtySegmentScanner(false);
            var renderScanner = m_RenderStore.GetDirtySegmentScanner(false);

            var syncState = new SyncedBufferIteratorState(
                cullingScanner,
                renderScanner,
                windowSizeInElements
            );

            var it = new Iterator<SyncedBufferSegmentMeta, SyncedBufferIteratorState>(syncState);
            while (it.MoveNext())
            {
                action.Execute(it.Current);
            }
        }

        #endregion

        #region Flexible Iteration (Boxing)

        /// <inheritdoc />
        public IIterator<BufferSegmentMeta> GetCullingDirtyIterator(bool merge = true)
            => m_CullingStore.GetDirtySegmentIterator(merge);

        /// <inheritdoc />
        public IIterator<BufferSegmentMeta> GetRenderDirtyIterator(bool merge = true)
            => m_RenderStore.GetDirtySegmentIterator(merge);

        /// <inheritdoc />
        public IIterator<SyncedBufferSegmentMeta> GetSyncedDirtyIterator(int batchesPerWindow = 1)
        {
            int effectiveBatchCount = Math.Max(1, batchesPerWindow);
            int windowSizeInElements = effectiveBatchCount * BatchSize;

            var cullingScanner = m_CullingStore.GetDirtySegmentScanner(false);
            var renderScanner = m_RenderStore.GetDirtySegmentScanner(false);

            var syncState = new SyncedBufferIteratorState(
                cullingScanner,
                renderScanner,
                windowSizeInElements
            );

            return new Iterator<SyncedBufferSegmentMeta, SyncedBufferIteratorState>(syncState);
        }

        #endregion

        #region Dirty State Management

        /// <inheritdoc />
        public void ClearCullingDirty() => m_CullingStore.ClearDirty();

        /// <inheritdoc />
        public void ClearRenderDirty() => m_RenderStore.ClearDirty();

        /// <inheritdoc />
        public void ClearAllDirty()
        {
            m_CullingStore.ClearDirty();
            m_RenderStore.ClearDirty();
        }

        #endregion

        #endregion
    }
}