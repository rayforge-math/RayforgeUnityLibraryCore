using Rayforge.Core.Common.Rendering;
using Rayforge.Core.Common.Rendering.Helpers;
using Rayforge.Core.Rendering.Abstractions;
using Rayforge.Core.Rendering.Helpers;
using System;
using UnityEngine;

namespace Rayforge.Core.Environment.Spatial.Rendering
{
    /// <summary>
    /// Defines the structural blueprint of a multi-LOD texture atlas.
    /// Calculates slice distribution and UV offsets without managing runtime occupancy.
    /// Supports reconfiguration to reuse internal arrays during quality setting changes.
    /// </summary>
    public class LodAtlasLayout
    {
        #region Internal Types

        /// <summary>
        /// Static configuration data for a specific LOD level within the atlas.
        /// </summary>
        private struct LodLevelInfo
        {
            public int StartSlice;
            public int SlotsPerDim;
            public int TotalCapacity;
        }

        #endregion

        #region Properties

        /// <summary>
        /// Gets a value indicating whether this layout has been initialized.
        /// </summary>
        public bool IsInitialized => m_Levels != null;

        /// <summary>
        /// The total number of texture array slices required to accommodate all configured LOD levels.
        /// </summary>
        public int RequiredSliceCount { get; private set; }

        /// <summary>
        /// The resolution of a single slot at LOD 0, used as the reference for all other levels.
        /// </summary>
        public PowerOfTwoResolution BaseResolution { get; private set; }

        /// <summary>
        /// The total number of addressable slots across all LOD levels.
        /// </summary>
        public int TotalCombinedCapacity { get; private set; }

        /// <summary>
        /// The number of configured LOD levels in this layout.
        /// </summary>
        public int LodCount => m_Levels?.Length ?? 0;

        #endregion

        #region Fields

        // Note: Removed readonly to allow the array to be resized or replaced during reconfiguration
        private LodLevelInfo[] m_Levels;

        #endregion

        #region Lifecycle

        /// <summary>
        /// Initializes a new atlas layout. 
        /// Use <see cref="Initialize"/> to populate data.
        /// </summary>
        public LodAtlasLayout() { }

        /// <summary>
        /// Updates the layout parameters and recalculates slice distribution.
        /// Reuses existing internal structures where possible to minimize GC pressure.
        /// </summary>
        /// <param name="maxCapacities">An array containing the maximum tile count for each LOD level. Its length defines the LOD count.</param>
        /// <param name="baseResolution">The resolution of LOD level 0. Subsequent levels are automatically derived via Downscale.</param>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="maxCapacities"/> is null.</exception>
        /// <exception cref="ArgumentException">Thrown if <paramref name="maxCapacities"/> is empty.</exception>
        /// <exception cref="InvalidOperationException">Thrown if resolution math results in zero slots or insufficient downscales.</exception>
        public void Initialize(int[] maxCapacities, PowerOfTwoResolution baseResolution)
        {
            if (maxCapacities == null)
                throw new ArgumentNullException(nameof(maxCapacities), "[LodAtlasLayout] Max capacities array cannot be null.");

            if (maxCapacities.Length == 0)
                throw new ArgumentException("[LodAtlasLayout] Max capacities array cannot be empty.", nameof(maxCapacities));

            int lodCount = maxCapacities.Length;

            Span<PowerOfTwoResolution> lodResolutions = stackalloc PowerOfTwoResolution[lodCount];
            PowerOfTwoResolution currentRes = baseResolution;

            for (int i = 0; i < lodCount; i++)
            {
                lodResolutions[i] = currentRes;

                if (i < lodCount - 1)
                {
                    if (currentRes <= PowerOfTwoResolution.Res1)
                    {
                        throw new InvalidOperationException(
                            $"[LodAtlasLayout] Insufficient downscales available: Cannot provide {lodCount} distinct LOD levels starting from base resolution {baseResolution}.");
                    }
                    currentRes = currentRes.Downscale();
                }
            }

            if (m_Levels == null || m_Levels.Length != lodCount)
            {
                m_Levels = new LodLevelInfo[lodCount];
            }

            BaseResolution = baseResolution;
            int currentSliceOffset = 0;
            int accumulatedCapacity = 0;

            for (int i = 0; i < lodCount; i++)
            {
                int tilesInLevel = maxCapacities[i];
                int slotsPerDim = ((int)lodResolutions[i]).ToSlotCountPerDim((int)BaseResolution);

                if (slotsPerDim <= 0)
                    throw new InvalidOperationException($"[LodAtlasLayout] Resolution for LOD {i} is invalid relative to BaseResolution.");

                int slotsPerSlice = slotsPerDim * slotsPerDim;
                int reqSlices = (tilesInLevel > 0) ? Mathf.CeilToInt((float)tilesInLevel / slotsPerSlice) : 0;
                int levelCapacity = reqSlices * slotsPerSlice;

                m_Levels[i] = new LodLevelInfo
                {
                    StartSlice = currentSliceOffset,
                    SlotsPerDim = slotsPerDim,
                    TotalCapacity = levelCapacity
                };

                accumulatedCapacity += levelCapacity;
                currentSliceOffset += reqSlices;
            }

            RequiredSliceCount = currentSliceOffset;
            TotalCombinedCapacity = accumulatedCapacity;
        }

        #endregion

        #region Public API

        /// <summary>
        /// Gets the maximum number of slots available for a specific LOD level.
        /// </summary>
        /// <exception cref="InvalidOperationException">Thrown if the layout has not been initialized.</exception>
        /// <exception cref="ArgumentOutOfRangeException">Thrown if <paramref name="lodIndex"/> is out of bounds.</exception>
        public int GetLodCapacity(int lodIndex)
        {
            ValidateStateAndIndex(lodIndex);
            return m_Levels[lodIndex].TotalCapacity;
        }

        /// <summary>
        /// Calculates the normalized UV mapping data (Slice, Scale, Offset) for a specific slot within a LOD level.
        /// </summary>
        /// <param name="lodIndex">The LOD level the slot belongs to.</param>
        /// <param name="slotIndex">The local index within that LOD level.</param>
        /// <returns>A TextureMappingData structure containing GPU-ready coordinates.</returns>
        /// <exception cref="InvalidOperationException">Thrown if the layout has not been initialized.</exception>
        /// <exception cref="ArgumentOutOfRangeException">Thrown if <paramref name="lodIndex"/> is out of bounds.</exception>
        public TextureMappingData GetMapping(int lodIndex, int slotIndex)
        {
            ValidateStateAndIndex(lodIndex);

            var info = m_Levels[lodIndex];

            int slotsPerSlice = info.SlotsPerDim * info.SlotsPerDim;
            int localSlice = slotIndex / slotsPerSlice;
            int localSlot = slotIndex % slotsPerSlice;

            float scale = 1.0f / info.SlotsPerDim;
            int x = localSlot % info.SlotsPerDim;
            int y = localSlot / info.SlotsPerDim;

            return new TextureMappingData
            {
                SliceIndex = info.StartSlice + localSlice,
                RelativeScale = scale,
                RelativeOffset = new Vector2(x * scale, y * scale)
            };
        }

        #endregion

        #region Helpers

        private void ValidateStateAndIndex(int lodIndex)
        {
            if (!IsInitialized)
                throw new InvalidOperationException("[LodAtlasLayout] Layout is not initialized. Call Initialize first.");

            if (lodIndex < 0 || lodIndex >= m_Levels.Length)
                throw new ArgumentOutOfRangeException(nameof(lodIndex), $"[LodAtlasLayout] LOD index {lodIndex} is out of bounds for {m_Levels.Length} levels.");
        }

        #endregion
    }
}