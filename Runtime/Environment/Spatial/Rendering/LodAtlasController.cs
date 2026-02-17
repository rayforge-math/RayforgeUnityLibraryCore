using Rayforge.Core.Common.Rendering.Helpers;
using System;
using System.Collections.Generic;
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

        #endregion

        #region Initialization

        /// <summary>
        /// Initializes the atlas, calculates required slices per LOD ring, and creates the hardware resource.
        /// </summary>
        /// <param name="lods">Configuration for each LOD level.</param>
        /// <param name="tileSize">The world-space size of a single tile.</param>
        /// <param name="batchSize">GPU buffer update batch size.</param>
        public void Initialize(TextureLOD[] lods, float tileSize, int registryCapacity, int batchSize)
        {
            m_Registry = new SphereMetadataRegistry<TKey, AtlasMappingData>(registryCapacity, batchSize);
            m_LodLevels = new LodLevelManager[lods.Length];

            int currentSliceOffset = 0;
            for (int i = 0; i < lods.Length; i++)
            {
                float prevDist = (i == 0) ? -1 : lods[i - 1].distanceThreshold;
                int tilesInRing = GetTileCountForRing(lods[i].distanceThreshold, prevDist, tileSize);

                int slotsPerSlice = lods[i].mapResolution.ToSlotCount(lods[0].mapResolution);
                int reqSlices = Mathf.CeilToInt((float)tilesInRing / slotsPerSlice);

                m_LodLevels[i] = new LodLevelManager
                {
                    StartSlice = currentSliceOffset,
                    SlotsPerDim = lods[i].mapResolution.ToSlotCountPerDim(lods[0].mapResolution),
                    TotalCapacity = reqSlices * slotsPerSlice
                };

                currentSliceOffset += reqSlices;
            }

            RequiredSliceCount = currentSliceOffset;
        }

        #endregion

        #region Public API

        /// <summary>
        /// Registers a tile, allocates an atlas slot, and provides rendering metadata to the baker callback.
        /// </summary>
        /// <param name="key">The identifier for the tile.</param>
        /// <param name="lodIndex">The targeted LOD level ring.</param>
        /// <param name="worldPos">Spatial position for culling.</param>
        /// <param name="radius">Spatial radius for culling.</param>
        /// <param name="updateAction">Callback containing the Slice and Viewport for rendering.</param>
        public void SetTile(TKey key, int lodIndex, Vector3 worldPos, float radius, Action<AtlasMappingData> updateAction)
        {
            bool isNew = !m_ActiveMappings.TryGetValue(key, out var mapping);
            bool lodChanged = !isNew && mapping.lodIndex != lodIndex;

            if (lodChanged)
            {
                m_LodLevels[mapping.lodIndex].Release(mapping.slotIndex);
                isNew = true;
            }

            if (isNew)
            {
                int slot = m_LodLevels[lodIndex].Acquire();
                mapping = (lodIndex, slot);
                m_ActiveMappings[key] = mapping;
            }

            var atlasData = m_LodLevels[mapping.lodIndex].GetMapping(mapping.slotIndex);
            var spatialData = new SphereSpatialData { Position = worldPos, Radius = radius };

            m_Registry.SetMetadata(key, spatialData, atlasData);
            updateAction?.Invoke(atlasData);
        }

        /// <summary>
        /// Frees the atlas slot and removes the tile from the GPU culling registry.
        /// </summary>
        /// <param name="key">The identifier of the tile to remove.</param>
        public void RemoveTile(TKey key)
        {
            if (m_ActiveMappings.Remove(key, out var mapping))
            {
                m_LodLevels[mapping.lodIndex].Release(mapping.slotIndex);
                m_Registry.ReleaseAndKill(key);
            }
        }

        /// <summary>
        /// Passes all modified metadata ranges to the provided callbacks.
        /// Keeps this class decoupled from specific GPU buffer types.
        /// </summary>
        /// <param name="onSpatialChanged">Callback for (Array source, int start, int count).</param>
        /// <param name="onVisualChanged">Callback for (Array source, int start, int count).</param>
        public void SyncMetadata(Action<Array, int, int> onSpatialChanged, Action<Array, int, int> onVisualChanged)
        {
            if (m_Registry == null) return;

            m_Registry.ExtractChanges(onSpatialChanged, onVisualChanged);
        }

        #endregion

        #region Geometry Helpers

        /// <summary>
        /// Calculates the number of tiles contained within a specific distance ring.
        /// </summary>
        private static int GetTileCountForRing(float largeRadius, float smallRadius, float tileSize)
        {
            int lTiles = GetTileCountForRadius(largeRadius, tileSize);
            if (smallRadius <= 0 || smallRadius >= largeRadius) return lTiles;
            int sTiles = GetTileCountForRadius(smallRadius, tileSize);
            return Mathf.Max(0, lTiles - sTiles);
        }

        /// <summary>
        /// Calculates the number of tiles in a square grid covered by a given radius.
        /// </summary>
        private static int GetTileCountForRadius(float radius, float tileSize)
        {
            if (tileSize <= 0.001f) return 0;
            int tiles = Mathf.CeilToInt(radius / tileSize);
            int side = (tiles * 2) + 1;
            return Mathf.Max(0, side * side);
        }

        #endregion
    }
}