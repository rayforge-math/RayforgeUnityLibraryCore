using Rayforge.Core.Rendering.Abstractions;
using System;
using UnityEngine;

namespace Rayforge.Core.Environment.Spatial.Rendering
{
    /// <summary>
    /// Specialized mapper for AABB (Axis-Aligned Bounding Box) spatial objects.
    /// </summary>
    public class AabbLodAtlasMapper<TKey> : LodAtlasMapper<TKey, AabbSpatialData, SpatialGpuDataRegistry<TKey, AabbSpatialData, TextureMappingData>>
        where TKey : struct, IEquatable<TKey>
    {
        #region LodAtlasMapper Impl

        /// <summary>
        /// Creates the registry specific to AABB spatial data.
        /// </summary>
        protected override SpatialGpuDataRegistry<TKey, AabbSpatialData, TextureMappingData> CreateRegistry(int totalCapacity, int batchSize)
        {
            return new SpatialGpuDataRegistry<TKey, AabbSpatialData, TextureMappingData>(totalCapacity, batchSize);
        }

        /// <summary>
        /// Formats raw spatial data into the AABB data structure.
        /// </summary>
        /// <param name="worldPos">The center position of the AABB.</param>
        /// <param name="extent">The uniform extent (half-size) of the box.</param>
        protected override AabbSpatialData CreateSpatialEntry(Vector3 worldPos, float extent)
        {
            Vector3 ext = new Vector3(extent, extent, extent);

            return new AabbSpatialData
            {
                MinBounds = worldPos - ext,
                MaxBounds = worldPos + ext,
                LayerMask = BitConverter.Int32BitsToSingle(0x1),
                ActiveFlag = BitConverter.Int32BitsToSingle(0x1)
            };
        }

        #endregion
    }
}
