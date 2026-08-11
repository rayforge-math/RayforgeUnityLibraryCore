using Rayforge.Core.Rendering.Abstractions;
using System;
using UnityEngine;

namespace Rayforge.Core.Environment.Spatial.Rendering
{
    public class AabbAtlasMapper<TKey> : LodAtlasMapper<TKey, AabbSpatialData, AabbGpuDataRegistry<TKey, TextureMappingData>>
            where TKey : struct, IEquatable<TKey>
    {
        protected override AabbGpuDataRegistry<TKey, TextureMappingData> CreateRegistry(int totalCapacity, int batchSize)
        {
            return new AabbGpuDataRegistry<TKey, TextureMappingData>(totalCapacity, batchSize);
        }

        protected override AabbSpatialData CreateSpatialEntry(Vector3 worldPos, float extent)
        {
            float halfExtent = extent * 0.5f;
            var minBounds = worldPos - new Vector3(halfExtent, halfExtent, halfExtent);
            var maxBounds = worldPos + new Vector3(halfExtent, halfExtent, halfExtent);
            return new AabbSpatialData { MinBounds = minBounds, MaxBounds = maxBounds };
        }
    }
}
