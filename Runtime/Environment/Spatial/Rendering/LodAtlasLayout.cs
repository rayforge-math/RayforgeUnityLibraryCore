using Rayforge.Core.Common.Rendering;
using Rayforge.Core.Common.Rendering.Helpers;
using Rayforge.Core.Rendering.Abstractions;
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

        // English: Removed readonly to allow the array to be resized or replaced during reconfiguration
        private LodLevelInfo[] m_Levels;

        #endregion

        /// <summary>
        /// Initializes a new atlas layout. 
        /// Use <see cref="Reconfigure"/> to populate data.
        /// </summary>
        public LodAtlasLayout() { }

        /// <summary>
        /// Initializes a new atlas layout based on the provided LOD grid and resolution settings.
        /// </summary>
        /// <param name="lodCount">Number of LOD levels to support.</param>
        /// <param name="maxCapacities">An array containing the maximum tile count for each LOD level.</param>
        /// <param name="lodResolutions">The target resolutions for each level (Index 0 is the base).</param>
        public LodAtlasLayout(int lodCount, int[] maxCapacities, ReadOnlySpan<PowerOfTwoResolution> lodResolutions)
        {
            Reconfigure(lodCount, maxCapacities, lodResolutions);
        }

        /// <summary>
        /// Updates the layout parameters and recalculates slice distribution.
        /// Reuses existing internal structures where possible to minimize GC pressure.
        /// </summary>
        /// <param name="lodCount">Number of LOD levels to support.</param>
        /// <param name="maxCapacities">An array containing the maximum tile count for each LOD level.</param>
        /// <param name="lodResolutions">The target resolutions for each level (Index 0 is the base).</param>
        /// <exception cref="ArgumentException">Thrown if <paramref name="lodResolutions"/> is empty.</exception>
        /// <exception cref="InvalidOperationException">Thrown if resolution math results in zero slots.</exception>
        public void Reconfigure(int lodCount, int[] maxCapacities, ReadOnlySpan<PowerOfTwoResolution> lodResolutions)
        {
            if (lodResolutions.Length == 0)
                throw new ArgumentException("[LodAtlasLayout] LOD resolutions cannot be empty.");

            if (m_Levels == null || m_Levels.Length != lodCount)
            {
                m_Levels = new LodLevelInfo[lodCount];
            }

            BaseResolution = lodResolutions[0];
            int currentSliceOffset = 0;
            int accumulatedCapacity = 0;

            for (int i = 0; i < lodCount; i++)
            {
                int tilesInLevel = maxCapacities[i];
                int slotsPerDim = lodResolutions[i].ToSlotCountPerDim(BaseResolution);

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

        /// <summary>
        /// Gets the maximum number of slots available for a specific LOD level.
        /// </summary>
        public int GetLevelCapacity(int lodIndex) => m_Levels[lodIndex].TotalCapacity;

        /// <summary>
        /// Calculates the normalized UV mapping data (Slice, Scale, Offset) for a specific slot within a LOD level.
        /// </summary>
        /// <param name="lodIndex">The LOD level the slot belongs to.</param>
        /// <param name="slotIndex">The local index within that LOD level.</param>
        /// <returns>A TextureMappingData structure containing GPU-ready coordinates.</returns>
        public TextureMappingData GetMapping(int lodIndex, int slotIndex)
        {
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

        /// <summary>
        /// Checks if a configuration is compatible with this layout to avoid unnecessary recalculations.
        /// </summary>
        public bool IsCompatible(int lodCount, int batchSize, PowerOfTwoResolution baseRes)
        {
            if (m_Levels == null) return false;
            return m_Levels.Length == lodCount && BaseResolution.Equals(baseRes);
        }
    }
}