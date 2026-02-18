using Rayforge.Core.Common.Rendering.Helpers;
using Rayforge.Core.Diagnostics;
using Rayforge.Core.Environment.Abstractions;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using UnityEngine;

namespace Rayforge.Core.Environment.Spatial.Rendering
{
    /// <summary>
    /// Manages a multi-LOD texture atlas by calculating slice requirements.
    /// </summary>
    /// <typeparam name="TKey">The unique identifier type for tiles (must be equatable).</typeparam>
    public class LodAtlasController<TKey> where TKey : struct, IEquatable<TKey>
    {
        #region Internal Types

        public bool showDebugLogs = false;

        /// <summary>
        /// Stores the parameters for a pending tile update request.
        /// </summary>
        private struct TileUpdateRequest
        {
            public TKey Key;
            public int LodIndex;
            public Vector3 WorldPos;
            public float Radius;
            public Action<AtlasMappingData> OnBakeAction;
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
            public AtlasMappingData GetMapping(int slotIndex)
            {
                int slotsPerSlice = SlotsPerDim * SlotsPerDim;
                int localSlice = slotIndex / slotsPerSlice;
                int localSlot = slotIndex % slotsPerSlice;

                float scale = 1.0f / SlotsPerDim;
                int x = localSlot % SlotsPerDim;
                int y = localSlot / SlotsPerDim;

                return new AtlasMappingData
                {
                    SliceIndex = StartSlice + localSlice,
                    RelativeScale = scale,
                    RelativeOffset = new Vector2(x * scale, y * scale)
                };
            }
        }

        #endregion

        #region Configuration & State

        private SphereMetadataRegistry<TKey, AtlasMappingData> m_Registry;
        private LodLevelManager[] m_LodLevels;
        private readonly Dictionary<TKey, (int lodIndex, int slotIndex)> m_ActiveMappings = new();

        public int RequiredSliceCount { get; private set; }
        public bool IsInitialized => m_LodLevels != null;

        private readonly HashSet<TKey> m_PendingRemovals = new();
        private readonly Dictionary<TKey, TileUpdateRequest> m_PendingUpdates = new();

        #endregion

        #region Initialization

        /// <summary>
        /// Initializes the atlas using a centralized LOD provider.
        /// The provider dictates exactly how many slices are needed for each LOD ring.
        /// </summary>
        /// <param name="provider">The source of truth for spatial and LOD logic.</param>
        /// <param name="lodConfigs">Resolution settings for each LOD level.</param>
        /// <param name="registryCapacity">Total capacity of the metadata registry.</param>
        /// <param name="batchSize">GPU sync batch size.</param>
        public void Initialize(ILODGridProvider provider, TextureLOD[] lodConfigs, int registryCapacity, int batchSize)
        {
            m_Registry = new SphereMetadataRegistry<TKey, AtlasMappingData>(registryCapacity, batchSize);

            int lodCount = provider.LodCount;
            m_LodLevels = new LodLevelManager[lodCount];

            LogDebug($"Initializing Atlas with {lodCount} LOD levels from Provider.");

            int currentSliceOffset = 0;

            for (int i = 0; i < lodCount; i++)
            {
                int tilesInRing = provider.GetMaxCapacityForLODLevel(i);

                int slotsPerDim = lodConfigs[i].mapResolution.ToSlotCountPerDim(lodConfigs[0].mapResolution);
                int slotsPerSlice = slotsPerDim * slotsPerDim;

                int reqSlices = 0;
                if (tilesInRing > 0)
                {
                    reqSlices = Mathf.CeilToInt((float)tilesInRing / slotsPerSlice);
                }

                m_LodLevels[i] = new LodLevelManager
                {
                    StartSlice = currentSliceOffset,
                    SlotsPerDim = slotsPerDim,
                    TotalCapacity = reqSlices * slotsPerSlice
                };

                LogDebug($"LOD[{i}]: Tiles in Ring: {tilesInRing}, " +
                    $"Slices: {reqSlices} (Start: {currentSliceOffset}), Capacity: {m_LodLevels[i].TotalCapacity}");

                currentSliceOffset += reqSlices;
            }

            RequiredSliceCount = currentSliceOffset;
            LogDebug($"Initialization Complete. Total Required Slices: {RequiredSliceCount}");
        }

        #endregion

        #region Public API (Deferred)

        /// <summary>
        /// Queues a tile to be updated or added. The actual processing is deferred until <see cref="ApplyChanges"/> is called.
        /// </summary>
        /// <param name="key">The unique identifier for the tile.</param>
        /// <param name="lodIndex">The target LOD level index.</param>
        /// <param name="worldPos">The world space position for spatial culling.</param>
        /// <param name="radius">The bounding radius for spatial culling.</param>
        /// <param name="updateAction">Callback invoked with the assigned atlas mapping data (useful for triggering texture bakes).</param>
        /// <remarks>
        /// If the tile was previously queued for removal in the same frame, the removal is cancelled.
        /// If multiple updates are queued for the same key, only the last one is preserved.
        /// </remarks>
        public void SetTile(TKey key, int lodIndex, Vector3 worldPos, float radius, Action<AtlasMappingData> updateAction)
        {
            m_PendingRemovals.Remove(key);
            m_PendingUpdates[key] = new TileUpdateRequest
            {
                Key = key,
                LodIndex = lodIndex,
                WorldPos = worldPos,
                Radius = radius,
                OnBakeAction = updateAction
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
        public void RemoveTile(TKey key)
        {
            m_PendingUpdates.Remove(key);
            m_PendingRemovals.Add(key);
        }

        /// <summary>
        /// Executes all queued removals and then all queued updates in a single batch.
        /// </summary>
        /// <remarks>
        /// Removals are processed first to ensure that released indices are immediately 
        /// available for new tile allocations, keeping the GPU buffer footprint minimal.
        /// </remarks>
        public void ApplyChanges()
        {
            int removeCount = m_PendingRemovals.Count;
            int updateCount = m_PendingUpdates.Count;

            if (removeCount > 0 || updateCount > 0)
            {
                LogDebug($"Applying Changes: Removing {removeCount}, Updating {updateCount}");
            }

            foreach (var key in m_PendingRemovals)
            {
                ExecuteRemove(key);
            }
            m_PendingRemovals.Clear();

            foreach (var request in m_PendingUpdates.Values)
            {
                ExecuteSet(request);
            }
            m_PendingUpdates.Clear();
        }

        /// <summary>
        /// Extracts modified metadata segments and passes them to external buffer synchronization callbacks.
        /// </summary>
        /// <param name="onSpatialChanged">Callback for spatial buffer updates: (sourceArray, startElement, elementCount).</param>
        /// <param name="onVisualChanged">Callback for visual buffer updates: (sourceArray, startElement, elementCount).</param>
        public void SyncMetadata(Action<Array, int, int> onSpatialChanged, Action<Array, int, int> onVisualChanged)
        {
            m_Registry?.ExtractChanges(onSpatialChanged, onVisualChanged);
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
                LogDebug($"ExecuteRemove: Key {key} (LOD: {mapping.lodIndex}, Slot: {mapping.slotIndex})");

                m_LodLevels[mapping.lodIndex].Release(mapping.slotIndex);
                m_Registry.ReleaseAndKill(key);
            }
        }

        /// <summary>
        /// Performs the actual allocation and metadata update for a tile request.
        /// </summary>
        /// <param name="req">The update request parameters.</param>
        private void ExecuteSet(TileUpdateRequest req)
        {
            bool isNew = !m_ActiveMappings.TryGetValue(req.Key, out var mapping);
            bool lodChanged = !isNew && mapping.lodIndex != req.LodIndex;

            if (lodChanged)
            {
                LogDebug($"LOD Change: Key {req.Key} moving from LOD {mapping.lodIndex} to {req.LodIndex}");

                m_LodLevels[mapping.lodIndex].Release(mapping.slotIndex);
                isNew = true;
            }

            if (isNew)
            {
                int slot = m_LodLevels[req.LodIndex].Acquire();
                mapping = (req.LodIndex, slot);
                m_ActiveMappings[req.Key] = mapping;

                if (!lodChanged) LogDebug($"New Allocation: Key {req.Key} -> LOD {req.LodIndex}, Slot {slot}");
            }

            var atlasData = m_LodLevels[mapping.lodIndex].GetMapping(mapping.slotIndex);
            var spatialData = new SphereSpatialData { Position = req.WorldPos, Radius = req.Radius };

            m_Registry.SetMetadata(req.Key, spatialData, atlasData);
            req.OnBakeAction?.Invoke(atlasData);
        }

        #endregion

        #region Debug Helper

        /// <summary>
        /// Logs a message to the custom DebugOutput if logging is enabled.
        /// This call is completely stripped from non-editor builds.
        /// </summary>
        [Conditional("UNITY_EDITOR")]
        private void LogDebug(string message, [CallerLineNumber] int line = 0)
        {
            DebugOutput.Log($"[LodAtlasController<{typeof(TKey).Name}>] {message}", showDebugLogs, lineNumber: line);
        }

        #endregion
    }
}