using Rayforge.Core.Common.Rendering;
using Rayforge.Core.Common.Rendering.Helpers;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Rayforge.Core.Environment.Spatial.Rendering
{
    public class TextureArrayLodAtlas : IDisposable
    {
        private Texture2DArray _dataArray;

        private LodSliceMetadata[] _lodMetaData;

        private struct LodSliceMetadata
        {
            public int startSlice;
            public int sliceCount;
            public int slotsPerDim;
        }

        // English: Maps LOD Level Index -> List of Slice Indices in the Texture2DArray
        private Dictionary<int, List<int>> _lodToSlicePool;

        public void Dispose()
        {
            Texture2DArray.Destroy(_dataArray);
        }

        /// <summary>
        /// Initializes the atlas as a generic spatial data provider based on LOD distances and tile size.
        /// </summary>
        /// <param name="lods">The LOD configuration array. lods[0] defines the base resolution of the atlas.</param>
        /// <param name="tileSize">The world size of a single tile (chunk).</param>
        /// <param name="format">The data format (e.g., R16 for precision, RGBA8 for masks).</param>
        public void Initialize(TextureLOD[] lods, float tileSize, TextureFormat format)
        {
            if (lods == null || lods.Length == 0)
            {
                _lodMetaData = Array.Empty<LodSliceMetadata>();
                return;
            }

            var baseRes = lods[0].mapResolution;
            _lodMetaData = new LodSliceMetadata[lods.Length];

            int currentSliceOffset = 0;
            float prevDist = -1;

            for (int i = 0; i < lods.Length; i++)
            {
                int tilesInRing = GetTileCountForRing(lods[i].distanceThreshold, prevDist, tileSize);
                prevDist = lods[i].distanceThreshold;

                int reqSlices = SliceCount(baseRes, lods[i].mapResolution, tilesInRing);

                _lodMetaData[i] = new LodSliceMetadata
                {
                    startSlice = currentSliceOffset,
                    sliceCount = reqSlices,
                    slotsPerDim = lods[i].mapResolution.ToSlotCountPerDim(baseRes)
                };

                currentSliceOffset += reqSlices;
            }

            _dataArray = new Texture2DArray(
                (int)baseRes,
                (int)baseRes,
                currentSliceOffset,
                format,
                false
            );

            _dataArray.filterMode = FilterMode.Bilinear;
            _dataArray.wrapMode = TextureWrapMode.Clamp;
        }

        /// <summary>
        /// Calculates the tile count for a ring based on world distances.
        /// If the configuration is invalid (prev >= current), it treats this LOD as a full square.
        /// </summary>
        private static int GetTileCountForRing(float largeRadius, float smallRadius, float tileSize)
        {
            int lTiles = GetTileCountForRadius(largeRadius, tileSize);

            if (smallRadius <= 0 || smallRadius >= largeRadius)
            {
                return lTiles;
            }

            int sTiles = GetTileCountForRadius(smallRadius, tileSize);

            return Mathf.Max(0, lTiles - sTiles);
        }

        private static int GetTileCountForRadius(float radius, float tileSize)
        {
            if (tileSize <= 0.001f) return 0;

            int tiles = Mathf.CeilToInt(radius / tileSize);
            int side = (tiles * 2) + 1;
            int area = side * side;

            return Mathf.Max(0, area);
        }

        private static int SliceCount(PowerOfTwoResolution baseRes, PowerOfTwoResolution res, int tiles)
        {
            var slotCountPerSlice = res.ToSlotCount(baseRes);
            int sliceCount = Mathf.CeilToInt((float)tiles / slotCountPerSlice);
            return sliceCount;
        }
    }
}