using Rayforge.Core.Collections.Abstractions;
using Rayforge.Core.Collections.Buffering;
using Rayforge.Core.Collections.Helpers;
using Rayforge.Core.Common.Rendering;
using Rayforge.Core.Environment.Abstractions;
using Rayforge.Core.Execution.Abstractions;
using Rayforge.Core.Rendering.Abstractions;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Rayforge.Core.Environment.Spatial.Rendering
{
    /// <summary>
    /// Manages a multi-LOD texture atlas by calculating slice requirements.
    /// </summary>
    /// <typeparam name="TKey">The unique identifier type for tiles (must be equatable).</typeparam>
    public abstract class LodAtlasMapper<TKey, TSpatial, TRegistry>
        where TKey : struct, IEquatable<TKey>
        where TSpatial : unmanaged, IGpuData<TSpatial>
        where TRegistry : SpatialGpuDataRegistry<TKey, TSpatial, TextureMappingData>
    {
        #region Internal Types

        public struct TileMetadata
        {
            public TKey Key;
            public TextureMappingData Mapping;
        }

        /// <summary>
        /// Stores the parameters for a pending tile update request.
        /// </summary>
        private struct TileUpdateRequest
        {
            public TKey Key;
            public int LodIndex;
            public Vector3 WorldPos;
            public float Extent;
        }

        #endregion

        #region Private State

        private LodAtlasLayout m_Layout;
        private TRegistry m_Registry;
        private LinearSlotAllocator[] m_Allocators;

        private readonly RequestQueue<TKey, TileUpdateRequest> m_Queue = new();

        private readonly Dictionary<TKey, (int lodIndex, int slotIndex)> m_ActiveMappings = new();
        private readonly Dictionary<int, TileMetadata> m_BakeQueue = new();

        #endregion

        #region Configuration & Public Getters

        /// <summary>
        /// The total number of slices required in the Texture2DArray to fit all LOD levels.
        /// </summary>
        public int RequiredSliceCount => m_Layout?.RequiredSliceCount ?? 0;

        /// <summary>
        /// The reference resolution of a single slot at LOD 0.
        /// All other LOD resolutions are relative to this base.
        /// </summary>
        public PowerOfTwoResolution BaseResolution => m_Layout?.BaseResolution ?? PowerOfTwoResolution.None;

        public bool IsInitialized => m_Layout != null && m_Layout.IsInitialized && m_Registry != null;

        /// <summary>
        /// Indicates if there are pending requests (adds or removals) in the queue.
        /// Use this to determine if FlushTileRequests() needs to be executed this frame.
        /// </summary>
        public bool HasPendingRequests => m_Queue.HasRequests;

        /// <summary>
        /// Indicates if new atlas mappings were generated during the last flush.
        /// If true, a bake pass is required to update the texture content.
        /// </summary>
        public bool HasBakeCommands => m_BakeQueue.Count > 0;

        /// <summary>
        /// The total number of LOD levels configured in the atlas layout.
        /// </summary>
        public int LodCount => m_Layout?.LodCount ?? 0;

        /// <summary>
        /// The number of currently active tile mappings.
        /// </summary>
        public int ActiveTileCount => m_ActiveMappings.Count;

        /// <summary>
        /// Gets the maximum capacity for a specific LOD level.
        /// </summary>
        /// <param name="lodIndex">The target LOD index.</param>
        public int GetLodCapacity(int lodIndex)
        {
            if (m_Layout == null || lodIndex < 0 || lodIndex >= m_Layout.LodCount)
                return 0;
            return m_Layout.GetLodCapacity(lodIndex);
        }

        /// <summary>
        /// Checks if a tile is currently active in the atlas.
        /// </summary>
        public bool IsTileActive(TKey key) => m_ActiveMappings.ContainsKey(key);

        /// <summary>
        /// Tries to retrieve the current LOD index and mapping data for an active tile.
        /// </summary>
        public bool TryGetActiveTile(TKey key, out int lodIndex, out TextureMappingData mapping)
        {
            if (m_ActiveMappings.TryGetValue(key, out var internalMapping))
            {
                lodIndex = internalMapping.lodIndex;
                mapping = m_Layout.GetMapping(lodIndex, internalMapping.slotIndex);
                return true;
            }

            lodIndex = -1;
            mapping = default;
            return false;
        }

        /// <summary>
        /// Provides read-only access to the underlying metadata registry.
        /// </summary>
        public IReadOnlySpatialMetadataProvider<TKey, TSpatial, TextureMappingData> Registry => m_Registry;

        #endregion

        #region Abstract Methods

        protected abstract TRegistry CreateRegistry(int totalCapacity, int batchSize);

        protected abstract TSpatial CreateSpatialEntry(Vector3 worldPos, float extent);

        #endregion

        #region Configuration & Cleanup

        /// <summary>
        /// Central internal method to unconditionally calculate the atlas layout and structural slot management.
        /// Wipes the current state and redefines how texture slots are distributed across slices
        /// based on a base resolution that is automatically downscaled for each subsequent LOD level.
        /// </summary>
        /// <param name="maxCapacities">An array containing the maximum tile count for each LOD level.</param>
        /// <param name="baseResolution">The resolution of LOD level 0. Subsequent levels are derived via Downscale.</param>
        /// <param name="batchSize">The number of entries per dirty-tracking batch for GPU synchronization.</param>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="maxCapacities"/> is null.</exception>
        /// <exception cref="InvalidOperationException">
        /// Thrown if insufficient downscales are available from the base resolution.
        /// </exception>
        /// <exception cref="ArgumentOutOfRangeException">Thrown if <paramref name="batchSize"/> is less than 1.</exception>
        public void Initialize(int[] maxCapacities, PowerOfTwoResolution baseResolution, int batchSize)
        {
            if (maxCapacities == null)
                throw new ArgumentNullException(nameof(maxCapacities), "Max capacities array cannot be null.");

            if (batchSize <= 0)
                throw new ArgumentOutOfRangeException(nameof(batchSize), "Batch size must be at least 1.");

            if (m_Layout == null)
                m_Layout = new LodAtlasLayout();

            m_Layout.Initialize(maxCapacities, baseResolution);

            if (m_Allocators == null || m_Allocators.Length != m_Layout.LodCount)
            {
                m_Allocators = new LinearSlotAllocator[m_Layout.LodCount];
            }

            int currentGlobalOffset = 0;
            for (int i = 0; i < m_Layout.LodCount; i++)
            {
                int levelCap = m_Layout.GetLodCapacity(i);
                m_Allocators[i] = new LinearSlotAllocator(levelCap, currentGlobalOffset);
                currentGlobalOffset += levelCap;
            }

            m_Registry = CreateRegistry(m_Layout.TotalCombinedCapacity, batchSize);

            Clear();
        }

        /// <summary>
        /// Updates the dirty-tracking granularity without losing any mapping data.
        /// Safe to call at runtime for performance tuning. Non-destructive.
        /// </summary>
        /// <returns>True if the batch size was changed and migrated; false if already at target size.</returns>
        public bool UpdateBatchSize(int newBatchSize)
        {
            if (!IsInitialized) return false;

            return m_Registry.UpdateBatchSize(newBatchSize);
        }

        /// <summary>
        /// Clears all runtime mappings and releases all slots, but keeps the internal 
        /// LOD structures and registry allocation intact.
        /// Use this to wipe the current "world state" without necessity to re-allocate GPU buffers.
        /// </summary>
        public void Clear()
        {
            m_Registry?.Clear();

            m_ActiveMappings.Clear();
            m_Queue.Clear();
            m_BakeQueue.Clear();

            if (m_Allocators != null)
            {
                foreach (var alloc in m_Allocators) alloc.Reset();
            }
        }

        #endregion

        #region Enqueue Change Requests (Deferred)

        /// <summary>
        /// Queues a tile to be updated or added. The actual processing is deferred until <see cref="FlushTileRequests"/> is called.
        /// </summary>
        /// <param name="key">The unique identifier for the tile.</param>
        /// <param name="lodIndex">The target LOD level index.</param>
        /// <param name="worldPos">The world space position for spatial culling.</param>
        /// <param name="extent">The bounding extent for spatial culling.</param>
        /// <exception cref="InvalidOperationException">Thrown if the mapper is not initialized.</exception>
        /// <exception cref="ArgumentOutOfRangeException">Thrown if <paramref name="lodIndex"/> is out of valid LOD bounds.</exception>
        /// <remarks>
        /// If the tile was previously queued for removal in the same frame, the removal is cancelled.
        /// If multiple updates are queued for the same key, only the last one is preserved.
        /// </remarks>
        public void RequestTile(TKey key, int lodIndex, Vector3 worldPos, float extent)
        {
            if (!IsInitialized)
                throw new InvalidOperationException("Mapper is not initialized.");

            if (lodIndex < 0 || lodIndex >= m_Allocators.Length)
                throw new ArgumentOutOfRangeException(nameof(lodIndex), "LOD index is out of valid range.");

            m_Queue.EnqueueUpdate(key, new TileUpdateRequest
            {
                Key = key,
                LodIndex = lodIndex,
                WorldPos = worldPos,
                Extent = extent
            });
        }

        /// <summary>
        /// Queues a tile for removal from the atlas and the culling registry.
        /// </summary>
        /// <param name="key">The unique identifier of the tile to remove.</param>
        /// <exception cref="InvalidOperationException">Thrown if the mapper is not initialized.</exception>
        /// <remarks>
        /// If an update for this tile was already queued in the same frame, it will be discarded.
        /// The actual slot release happens during <see cref="FlushTileRequests"/>.
        /// </remarks>
        public void ReleaseTile(TKey key)
        {
            if (!IsInitialized)
                throw new InvalidOperationException("Mapper is not initialized.");

            m_Queue.EnqueueRemoval(key);
        }

        #endregion

        #region Bake Queue Control

        /// <summary>
        /// Executes all queued removals and then all queued updates in a single batch and
        /// adds mapping updates to the bake queue.
        /// </summary>
        /// <remarks>
        /// Removals are processed first to ensure that released indices are immediately 
        /// available for new tile allocations, keeping the GPU buffer footprint minimal.
        /// </remarks>
        public void FlushTileRequests()
        {
            if (!IsInitialized)
                throw new InvalidOperationException("Mapper is not initialized.");

            m_BakeQueue.Clear();

            if (!m_Queue.HasRequests) return;

            var removeHandler = new RemovalHandler(this);
            m_Queue.ForEachRemoval(ref removeHandler);

            var updateHandler = new UpdateHandler(this);
            m_Queue.ForEachUpdate(ref updateHandler);

            m_Queue.Clear();
        }

        /// <summary>
        /// Provides an iterator over all pending tile bakes. 
        /// Each element contains the tile metadata required for bake.
        /// </summary>
        /// <exception cref="InvalidOperationException">Thrown if the mapper is not initialized.</exception>
        public IIterator<TileMetadata> GetPendingBakes()
        {
            if (!IsInitialized)
                throw new InvalidOperationException("Mapper is not initialized.");

            return m_BakeQueue.Values.GetEnumerator().ToIterator();
        }

        /// <summary>
        /// Provides a fresh iterator for all segments that need a GPU update (metadata or texture).
        /// </summary>
        /// <param name="merge">If true, contiguous dirty batches are merged into larger segments.</param>
        /// <exception cref="InvalidOperationException">Thrown if the mapper is not initialized.</exception>
        public IIterator<BufferSegmentMeta<TSpatial>> GetCullingDirtyIterator(bool merge = false)
        {
            if (!IsInitialized)
                throw new InvalidOperationException("Mapper is not initialized.");

            return m_Registry.GetCullingDirtyIterator(merge);
        }

        /// <summary>
        /// Provides a fresh iterator for all segments that need a GPU update (metadata or texture).
        /// </summary>
        /// <param name="merge">If true, contiguous dirty batches are merged into larger segments.</param>
        /// <exception cref="InvalidOperationException">Thrown if the mapper is not initialized.</exception>
        public IIterator<BufferSegmentMeta<TextureMappingData>> GetRenderDirtyIterator(bool merge = false)
        {
            if (!IsInitialized)
                throw new InvalidOperationException("Mapper is not initialized.");

            return m_Registry.GetRenderDirtyIterator(merge);
        }

        /// <summary>
        /// Provides a synchronized iterator that yields dirty segments from both stores.
        /// Aligns dirty streams into windows defined by a fixed number of batches.
        /// </summary>
        /// <param name="batchesPerWindow">
        /// How many dirty batches to process in one sync window. 
        /// Higher values reduce SetData calls, lower values improve time-slicing granularity.
        /// </param>
        /// <exception cref="InvalidOperationException">Thrown if the mapper is not initialized.</exception>
        public IIterator<SyncedSegmentMeta<TSpatial, TextureMappingData>> GetSyncedDirtyIterator(int batchesPerWindow = 1)
        {
            if (!IsInitialized)
                throw new InvalidOperationException("Mapper is not initialized.");

            return m_Registry.GetSyncedDirtyIterator(batchesPerWindow);
        }

        /// <summary>
        /// Executes a handler for each pending tile bake without allocations.
        /// </summary>
        /// <exception cref="InvalidOperationException">Thrown if the mapper is not initialized.</exception>
        public void ForEachPendingBake<THandler>(ref THandler handler)
            where THandler : struct, IExecutionHandler<TileMetadata>
        {
            if (!IsInitialized)
                throw new InvalidOperationException("Mapper is not initialized.");

            var iter = m_BakeQueue.Values.GetEnumerator().ToIterator();
            while (iter.MoveNext())
            {
                handler.Execute(iter.Current);
            }
        }

        /// <summary>
        /// Executes a handler for each culling dirty segment without allocations.
        /// </summary>
        /// <param name="handler">The execution handler to process each segment.</param>
        /// <param name="merge">If true, contiguous dirty batches are merged into larger segments.</param>
        /// <exception cref="InvalidOperationException">Thrown if the mapper is not initialized.</exception>
        public void ForEachCullingDirty<THandler>(ref THandler handler, bool merge = false)
            where THandler : struct, IExecutionHandler<BufferSegmentMeta<TSpatial>>
        {
            if (!IsInitialized)
                throw new InvalidOperationException("Mapper is not initialized.");

            m_Registry.ForEachCullingDirty(ref handler, merge);
        }

        /// <summary>
        /// Executes a handler for each render dirty segment without allocations.
        /// </summary>
        /// <param name="handler">The execution handler to process each segment.</param>
        /// <param name="merge">If true, contiguous dirty batches are merged into larger segments.</param>
        /// <exception cref="InvalidOperationException">Thrown if the mapper is not initialized.</exception>
        public void ForEachRenderDirty<THandler>(ref THandler handler, bool merge = false)
            where THandler : struct, IExecutionHandler<BufferSegmentMeta<TextureMappingData>>
        {
            if (!IsInitialized)
                throw new InvalidOperationException("Mapper is not initialized.");

            m_Registry.ForEachRenderDirty(ref handler, merge);
        }

        /// <summary>
        /// Executes a handler for each synchronized dirty segment from both stores without allocations.
        /// </summary>
        /// <param name="handler">The execution handler to process each synchronized segment.</param>
        /// <param name="batchesPerWindow">How many dirty batches to process in one sync window.</param>
        /// <exception cref="InvalidOperationException">Thrown if the mapper is not initialized.</exception>
        public void ForEachSyncedDirty<THandler>(ref THandler handler, int batchesPerWindow = 1)
            where THandler : struct, IExecutionHandler<SyncedSegmentMeta<TSpatial, TextureMappingData>>
        {
            if (!IsInitialized)
                throw new InvalidOperationException("Mapper is not initialized.");

            m_Registry.ForEachSyncedDirty(ref handler, batchesPerWindow);
        }

        /// <summary>
        /// Clears the bake lookup. Useful when the whole atlas is invalidated.
        /// </summary>
        public void ClearBakeQueue() => m_BakeQueue.Clear();

        #endregion

        #region Execution Handlers (Zero-Allocation)

        private struct RemovalHandler : IExecutionHandler<TKey>
        {
            private readonly LodAtlasMapper<TKey, TSpatial, TRegistry> m_Mapper;

            public RemovalHandler(LodAtlasMapper<TKey, TSpatial, TRegistry> mapper)
            {
                m_Mapper = mapper;
            }

            public void Execute(TKey key)
            {
                m_Mapper.ExecuteRemove(key);
            }
        }

        private struct UpdateHandler : IExecutionHandler<KeyValuePair<TKey, TileUpdateRequest>>
        {
            private readonly LodAtlasMapper<TKey, TSpatial, TRegistry> m_Mapper;

            public UpdateHandler(LodAtlasMapper<TKey, TSpatial, TRegistry> mapper)
            {
                m_Mapper = mapper;
            }

            public void Execute(KeyValuePair<TKey, TileUpdateRequest> kvp)
            {
                m_Mapper.ExecuteSet(kvp.Value);
            }
        }

        #endregion

        #region Internal Execution

        /// <summary>
        /// Performs the actual removal of a tile from the internal state and the registry.
        /// </summary>
        /// <param name="key">The unique key of the tile.</param>
        private void ExecuteRemove(TKey key)
        {
            if (m_ActiveMappings.Remove(key, out var mapping))
            {
                m_Allocators[mapping.lodIndex].Release(mapping.slotIndex);
                m_Registry.ReleaseAndKill(key);
            }
        }

        /// <summary>
        /// Performs the actual allocation and metadata update for a tile request.
        /// </summary>
        /// <param name="req">The update request parameters.</param>
        /// <returns>Returns true if the tile is brand new or changed its LOD level (requires full re-bake).</returns>
        private bool ExecuteSet(TileUpdateRequest req)
        {
            bool isNew = !m_ActiveMappings.TryGetValue(req.Key, out var mapping);
            bool lodChanged = !isNew && mapping.lodIndex != req.LodIndex;

            if (lodChanged)
            {
                m_Allocators[mapping.lodIndex].Release(mapping.slotIndex);
                isNew = true;
            }

            if (isNew)
            {
                int slot = m_Allocators[req.LodIndex].Acquire();
                mapping = (req.LodIndex, slot);
                m_ActiveMappings[req.Key] = mapping;
            }

            var atlasMapping = m_Layout.GetMapping(mapping.lodIndex, mapping.slotIndex);
            var spatialData = CreateSpatialEntry(req.WorldPos, req.Extent);

            int bufferIndex = m_Registry.SetMetadata(req.Key, spatialData, atlasMapping);
            m_BakeQueue[bufferIndex] = new TileMetadata
            {
                Key = req.Key,
                Mapping = atlasMapping
            };

            return isNew;
        }

        #endregion
    }
}