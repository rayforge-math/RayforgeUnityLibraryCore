using Rayforge.Core.Collections.Abstractions;
using Rayforge.Core.Collections.Iterator.Helpers;
using Rayforge.Core.Common.Rendering;
using Rayforge.Core.Common.Rendering.Helpers;
using Rayforge.Core.Environment.Abstractions;
using Rayforge.Core.Rendering.Textures;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Rayforge.Core.Environment.Spatial.Rendering
{
    /// <summary>
    /// Manages a multi-LOD texture atlas by calculating slice requirements.
    /// </summary>
    /// <typeparam name="TKey">The unique identifier type for tiles (must be equatable).</typeparam>
    public class LodAtlasMapper<TKey> where TKey : struct, IEquatable<TKey>
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
            public float Radius;
        }

        /// <summary>
        /// Encapsulates both the structural layout and the slot occupancy of a specific LOD level.
        /// </summary>
        private class LodLevelManager
        {
            public int StartSlice;
            public int SlotsPerDim;
            public int TotalCapacity;

            private int m_NextAvailableIndex = 0;
            private readonly Stack<int> m_FreeSlots = new();

            /// <summary>
            /// Acquires the next available slot index, either from the free stack or by incrementing the counter.
            /// </summary>
            public int Acquire()
            {
                if (m_FreeSlots.Count > 0) return m_FreeSlots.Pop();
                if (m_NextAvailableIndex >= TotalCapacity)
                    throw new OverflowException("LOD level capacity exceeded.");

                return m_NextAvailableIndex++;
            }

            /// <summary>
            /// Returns a slot index to the pool for reuse.
            /// </summary>
            public void Release(int index) => m_FreeSlots.Push(index);

            /// <summary>
            /// Calculates the normalized atlas mapping data for a specific slot.
            /// </summary>
            public TextureMappingData GetMapping(int slotIndex)
            {
                int slotsPerSlice = SlotsPerDim * SlotsPerDim;
                int localSlice = slotIndex / slotsPerSlice;
                int localSlot = slotIndex % slotsPerSlice;

                float scale = 1.0f / SlotsPerDim;
                int x = localSlot % SlotsPerDim;
                int y = localSlot / SlotsPerDim;

                return new TextureMappingData
                {
                    SliceIndex = StartSlice + localSlice,
                    RelativeScale = scale,
                    RelativeOffset = new Vector2(x * scale, y * scale)
                };
            }

            /// <summary>
            /// Resets the occupancy state of this LOD level, effectively freeing all slots.
            /// Clears the free-slots stack and resets the linear allocation counter.
            /// </summary>
            public void Reset()
            {
                m_NextAvailableIndex = 0;
                m_FreeSlots.Clear();
            }
        }

        #endregion

        #region Private State

        private const string Tag = "[AtlasMapper]";

        private SphereMetadataRegistry<TKey, TextureMappingData> m_Registry;
        private LodLevelManager[] m_LodLevels;
        private readonly Dictionary<TKey, (int lodIndex, int slotIndex)> m_ActiveMappings = new();

        private readonly HashSet<TKey> m_PendingRemovals = new();
        private readonly Dictionary<TKey, TileUpdateRequest> m_PendingUpdates = new();

        private readonly List<TileMetadata> m_BakeQueue = new();

        #endregion

        #region Configuration & Public Getters

        /// <summary>
        /// The total number of slices required in the Texture2DArray to fit all LOD levels.
        /// </summary>
        public int RequiredSliceCount { get; private set; }

        /// <summary>
        /// The reference resolution of a single slot at LOD 0.
        /// All other LOD resolutions are relative to this base.
        /// </summary>
        public PowerOfTwoResolution BaseResolution { get; private set; }

        public bool IsInitialized => m_LodLevels != null && m_LodLevels.Length > 0 && m_Registry != null;

        /// <summary>
        /// Indicates if there are pending requests (adds or removals) in the queue.
        /// Use this to determine if FlushTileRequests() needs to be executed this frame.
        /// </summary>
        public bool HasPendingRequests => m_PendingUpdates.Count > 0 || m_PendingRemovals.Count > 0;

        /// <summary>
        /// Indicates if new atlas mappings were generated during the last flush.
        /// If true, a bake pass is required to update the texture content.
        /// </summary>
        public bool HasBakeCommands => m_BakeQueue.Count > 0;

        #endregion

        #region Configuration & Cleanup

        /// <summary>
        /// Configures or reconfigures the atlas layout based on the provided LOD settings.
        /// This method checks if the structural configuration (resolutions, capacities, or batching) 
        /// has changed before triggering a heavy rebuild of the internal slot management.
        /// </summary>
        /// <param name="provider">The source of truth for spatial logic and maximum tile capacities per LOD level.</param>
        /// <param name="lodResolutions">A span of resolutions for each LOD level. Index 0 defines the BaseResolution.</param>
        /// <param name="batchSize">The number of entries per dirty-tracking batch for GPU synchronization.</param>
        /// <returns>
        /// True if the configuration changed and a full layout rebuild was performed (invalidating current mappings). 
        /// False if the configuration was identical to the current state, resulting in no changes.
        /// </returns>
        public bool Configure(ILODGridProvider<TKey> provider, ReadOnlySpan<PowerOfTwoResolution> lodResolutions, int batchSize)
        {
            return CheckAndCalculateLayout(provider, lodResolutions, batchSize);
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
        /// Use this to wipe the current "world state" without re-allocating GPU buffers.
        /// </summary>
        public void Clear()
        {
            m_Registry?.Clear();

            m_ActiveMappings.Clear();
            m_PendingRemovals.Clear();
            m_PendingUpdates.Clear();
            m_BakeQueue.Clear();

            if (m_LodLevels != null)
            {
                foreach (var level in m_LodLevels) level.Reset();
            }
        }

        #endregion

        #region Internal Setup

        /// <summary>
        /// Central internal method to (re)calculate the atlas layout and structural slot management.
        /// Wipes the current state and redefines how texture slots are distributed across slices
        /// based on the provided LOD resolutions.
        /// </summary>
        /// <param name="provider">The source of truth for spatial logic and maximum tile capacities per ring.</param>
        /// <param name="lodResolutions">A span of resolutions for each LOD level. Index 0 defines the BaseResolution.</param>
        /// <param name="batchSize">The number of entries per dirty-tracking batch for GPU synchronization.</param>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="provider"/> is null.</exception>
        /// <exception cref="ArgumentException">Thrown if <paramref name="lodResolutions"/> is empty.</exception>
        /// <exception cref="InvalidOperationException">
        /// Thrown if the number of resolutions doesn't match the provider's LOD count, 
        /// or if a LOD resolution is larger than the base resolution.
        /// </exception>
        /// <exception cref="ArgumentOutOfRangeException">Thrown if <paramref name="batchSize"/> is less than 1.</exception>
        /// <returns>
        /// True if the layout was rebuilt (all current mappings were cleared).
        /// False if the configuration was compatible and the state remains intact.
        /// </returns>
        private bool CheckAndCalculateLayout(ILODGridProvider<TKey> provider, ReadOnlySpan<PowerOfTwoResolution> lodResolutions, int batchSize)
        {
            if (provider == null)
                throw new ArgumentNullException(nameof(provider), $"Provider is null. Initialization aborted.");

            if (lodResolutions.Length == 0)
                throw new ArgumentException($"lodResolutions array is empty.", nameof(lodResolutions));

            if (provider.LodCount != lodResolutions.Length)
                throw new InvalidOperationException($"LOD Count mismatch: Provider expects {provider.LodCount}, but received {lodResolutions.Length} configurations.");

            if (batchSize <= 0)
                throw new ArgumentOutOfRangeException(nameof(batchSize), $"Batch size must be at least 1.");

            bool changed = 
                !IsInitialized ||
                m_LodLevels.Length != lodResolutions.Length ||
                m_Registry.BatchSize != batchSize ||
                !BaseResolution.Equals(lodResolutions[0]);

            int lodCount = lodResolutions.Length;

            var nextLevels = new LodLevelManager[lodCount];
            int currentSliceOffset = 0;
            int totalCapacityNeeded = 0;
            PowerOfTwoResolution incomingBase = lodResolutions[0];

            for (int i = 0; i < lodCount; i++)
            {
                int tilesInRing = provider.GetMaxCapacityForLODLevel(i);
                int slotsPerDim = lodResolutions[i].ToSlotCountPerDim(incomingBase);
                int slotsPerSlice = slotsPerDim * slotsPerDim;

                if (slotsPerDim <= 0)
                    throw new InvalidOperationException($"Resolution for LOD {i} is too large for BaseResolution {incomingBase}.");

                int reqSlices = (tilesInRing > 0) ? Mathf.CeilToInt((float)tilesInRing / slotsPerSlice) : 0;
                int levelCapacity = reqSlices * slotsPerSlice;

                if (!changed)
                {
                    var current = m_LodLevels[i];
                    if (current.StartSlice != currentSliceOffset ||
                        current.SlotsPerDim != slotsPerDim ||
                        current.TotalCapacity != levelCapacity)
                    {
                        changed = true;
                    }
                }

                nextLevels[i] = new LodLevelManager
                {
                    StartSlice = currentSliceOffset,
                    SlotsPerDim = slotsPerDim,
                    TotalCapacity = levelCapacity
                };

                totalCapacityNeeded += levelCapacity;
                currentSliceOffset += reqSlices;
            }

            if (!changed) return false;

            Clear();

            m_LodLevels = nextLevels;
            BaseResolution = incomingBase;
            RequiredSliceCount = currentSliceOffset;

            if (m_Registry == null)
                m_Registry = new SphereMetadataRegistry<TKey, TextureMappingData>(totalCapacityNeeded, batchSize);
            else
                m_Registry.Reconfigure(totalCapacityNeeded, batchSize);

            return true;
        }

        #endregion

        #region Enqueue Change Requests (Deferred)

        /// <summary>
        /// Queues a tile to be updated or added. The actual processing is deferred until <see cref="ApplyChanges"/> is called.
        /// </summary>
        /// <param name="key">The unique identifier for the tile.</param>
        /// <param name="lodIndex">The target LOD level index.</param>
        /// <param name="worldPos">The world space position for spatial culling.</param>
        /// <param name="radius">The bounding radius for spatial culling.</param>
        /// <remarks>
        /// If the tile was previously queued for removal in the same frame, the removal is cancelled.
        /// If multiple updates are queued for the same key, only the last one is preserved.
        /// </remarks>
        public void RequestTile(TKey key, int lodIndex, Vector3 worldPos, float radius)
        {
            if (lodIndex < 0 || lodIndex >= m_LodLevels.Length)
                return;

            m_PendingRemovals.Remove(key);
            m_PendingUpdates[key] = new TileUpdateRequest
            {
                Key = key,
                LodIndex = lodIndex,
                WorldPos = worldPos,
                Radius = radius
            };
        }

        /// <summary>
        /// Queues a tile for removal from the atlas and the culling registry.
        /// </summary>
        /// <param name="key">The unique identifier of the tile to remove.</param>
        /// <remarks>
        /// If an update for this tile was already queued in the same frame, it will be discarded.
        /// The actual slot release happens during <see cref="ApplyChanges"/>.
        /// </remarks>
        public void ReleaseTile(TKey key)
        {
            m_PendingUpdates.Remove(key);
            m_PendingRemovals.Add(key);
        }

        #endregion

        #region Execute Queue

        /// <summary>
        /// Executes all queued removals and then all queued updates in a single batch and
        /// adds mapping updates to the broadcast queue.
        /// </summary>
        /// <remarks>
        /// Removals are processed first to ensure that released indices are immediately 
        /// available for new tile allocations, keeping the GPU buffer footprint minimal.
        /// </remarks>
        public void FlushTileRequests()
        {
            int removeCount = m_PendingRemovals.Count;
            int updateCount = m_PendingUpdates.Count;

            if (removeCount == 0 && updateCount == 0)
            {
                return;
            }

            foreach (var key in m_PendingRemovals)
            {
                ExecuteRemove(key);
            }
            m_PendingRemovals.Clear();

            foreach (var request in m_PendingUpdates.Values)
            {
                var mapping = ExecuteSet(request);
                m_BakeQueue.Add(new TileMetadata
                {
                    Key = request.Key,
                    Mapping = mapping
                });
            }
            m_PendingUpdates.Clear();
        }

        #endregion

        #region Dispatch GPU Updates

        /// <summary>
        /// Broadcast Iterator. 
        /// Iterates over the cached results. 
        /// Can be called multiple times for different texture passes.
        /// </summary>
        public bool TryGetBakeIterator(out IIterator<TileMetadata> iter)
        {
            if (!IsInitialized)
            {
                iter = IIterator<TileMetadata>.Empty;
                return false;
            }

            iter = m_BakeQueue.GetEnumerator().ToIterator();
            return true;
        }

        /// <summary>
        /// Final cleanup of the pending requests. 
        /// Call this only after ALL atlases have processed the changes.
        /// </summary>
        public void ClearBakeQueue()
        {
            m_BakeQueue.Clear();
        }

        /// <summary>
        /// Grants read-only access to the internal registry.
        /// </summary>
        public ISpatialMetadataRegistry Registry => m_Registry;

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
                m_LodLevels[mapping.lodIndex].Release(mapping.slotIndex);
                m_Registry.ReleaseAndKill(key);
            }
        }

        /// <summary>
        /// Performs the actual allocation and metadata update for a tile request.
        /// </summary>
        /// <param name="req">The update request parameters.</param>
        private TextureMappingData ExecuteSet(TileUpdateRequest req)
        {
            bool isNew = !m_ActiveMappings.TryGetValue(req.Key, out var mapping);
            bool lodChanged = !isNew && mapping.lodIndex != req.LodIndex;

            if (lodChanged)
            {
                m_LodLevels[mapping.lodIndex].Release(mapping.slotIndex);
                isNew = true;
            }

            if (isNew)
            {
                int slot = m_LodLevels[req.LodIndex].Acquire();
                mapping = (req.LodIndex, slot);
                m_ActiveMappings[req.Key] = mapping;
            }

            var atlasData = m_LodLevels[mapping.lodIndex].GetMapping(mapping.slotIndex);
            var spatialData = new SphereSpatialData { Position = req.WorldPos, Radius = req.Radius };

            m_Registry.SetMetadata(req.Key, spatialData, atlasData);
            return atlasData;
        }

        #endregion
    }
}