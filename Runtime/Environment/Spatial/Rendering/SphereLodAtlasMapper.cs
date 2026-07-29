using Rayforge.Core.Rendering.Abstractions;
using System;
using UnityEngine;

namespace Rayforge.Core.Environment.Spatial.Rendering
{
    /// <summary>
    /// Specialized mapper for spherical spatial objects.
    /// </summary>
    public class SphereLodAtlasMapper<TKey> : LodAtlasMapper<TKey, SphereSpatialData, SpatialGpuDataRegistry<TKey, SphereSpatialData, TextureMappingData>>
        where TKey : struct, IEquatable<TKey>
    {
        #region LodAtlasMapper Impl

        /// <summary>
        /// Creates the registry specific to spherical spatial data.
        /// </summary>
        protected override SpatialGpuDataRegistry<TKey, SphereSpatialData, TextureMappingData> CreateRegistry(int totalCapacity, int batchSize)
        {
            return new SpatialGpuDataRegistry<TKey, SphereSpatialData, TextureMappingData>(totalCapacity, batchSize);
        }

        /// <summary>
        /// Formats raw spatial data into the spherical data structure.
        /// </summary>
        protected override SphereSpatialData CreateSpatialEntry(Vector3 worldPos, float extent)
        {
            return new SphereSpatialData
            {
                Position = worldPos,
                Radius = extent
            };
        }

        #endregion
    }
}
