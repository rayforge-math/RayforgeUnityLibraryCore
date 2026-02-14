using Rayforge.Core.Common.Rendering;
using Rayforge.Core.Common.Rendering.Helpers;
using Rayforge.Core.ManagedResources.Abstractions;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Rayforge.Core.Environment.Spatial.Rendering
{
    /// <summary>
    /// Manages a multi-LOD texture atlas by calculating slice requirements 
    /// and controlling a hardware-agnostic <see cref="IDynamicTextureArray"/>.
    /// </summary>
    public class TextureArrayLodAtlas : IDisposable
    {
        private IDynamicTextureArray m_AtlasResource;

        private LodSliceMetadata[] m_LodMetaData;

        private struct LodSliceMetadata
        {
            public int startSlice;
            public int sliceCount;
            public int slotsPerDim;
        }

        /// <summary>
        /// Initializes the atlas layout and resizes the underlying resource.
        /// </summary>
        /// <param name="resource">The dynamic resource to be used as storage.</param>
        /// <param name="lods">LOD configuration array.</param>
        /// <param name="tileSize">World size of a single tile.</param>
        public void Initialize(IDynamicTextureArray resource, TextureLOD[] lods, float tileSize)
        {
            m_AtlasResource = resource ?? throw new ArgumentNullException(nameof(resource));

            if (lods == null || lods.Length == 0)
            {
                m_LodMetaData = Array.Empty<LodSliceMetadata>();
                return;
            }

            var baseRes = lods[0].mapResolution;
            m_LodMetaData = new LodSliceMetadata[lods.Length];

            int currentSliceOffset = 0;
            float prevDist = -1;

            for (int i = 0; i < lods.Length; i++)
            {
                int tilesInRing = GetTileCountForRing(lods[i].distanceThreshold, prevDist, tileSize);
                prevDist = lods[i].distanceThreshold;

                int reqSlices = SliceCount(baseRes, lods[i].mapResolution, tilesInRing);

                m_LodMetaData[i] = new LodSliceMetadata
                {
                    startSlice = currentSliceOffset,
                    sliceCount = reqSlices,
                    slotsPerDim = lods[i].mapResolution.ToSlotCountPerDim(baseRes)
                };

                currentSliceOffset += reqSlices;
            }

            m_AtlasResource.Create(currentSliceOffset);
        }

        /// <summary>
        /// Updates a tile using the abstract SetSlice method.
        /// </summary>
        public void UpdateTile(int globalSliceIndex, Texture source)
        {
            m_AtlasResource?.SetSlice(globalSliceIndex, source);
        }

        /// <summary>
        /// Provides the resource for shader binding via the interface.
        /// </summary>
        public Texture GetGPUResource() => m_AtlasResource?.GetBaseResource();

        public void Dispose()
        {
            m_AtlasResource?.Release();
            m_AtlasResource = null;
        }

        #region Geometry Helpers

        private static int GetTileCountForRing(float largeRadius, float smallRadius, float tileSize)
        {
            int lTiles = GetTileCountForRadius(largeRadius, tileSize);
            if (smallRadius <= 0 || smallRadius >= largeRadius) return lTiles;
            int sTiles = GetTileCountForRadius(smallRadius, tileSize);
            return Mathf.Max(0, lTiles - sTiles);
        }

        private static int GetTileCountForRadius(float radius, float tileSize)
        {
            if (tileSize <= 0.001f) return 0;
            int tiles = Mathf.CeilToInt(radius / tileSize);
            int side = (tiles * 2) + 1;
            return Mathf.Max(0, side * side);
        }

        private static int SliceCount(PowerOfTwoResolution baseRes, PowerOfTwoResolution res, int tiles)
        {
            var slotCountPerSlice = res.ToSlotCount(baseRes);
            return Mathf.CeilToInt((float)tiles / slotCountPerSlice);
        }

        #endregion
    }
}