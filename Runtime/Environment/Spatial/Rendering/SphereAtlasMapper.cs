using Rayforge.Core.Rendering.Abstractions;
using System;
using UnityEngine;

namespace Rayforge.Core.Environment.Spatial.Rendering
{
    public class SphereAtlasMapper<TKey> : LodAtlasMapper<TKey, SphereSpatialData, SphereGpuDataRegistry<TKey, TextureMappingData>>
            where TKey : struct, IEquatable<TKey>
    {
        protected override SphereGpuDataRegistry<TKey, TextureMappingData> CreateRegistry(int totalCapacity, int batchSize)
        {
            return new SphereGpuDataRegistry<TKey, TextureMappingData>(totalCapacity, batchSize);
        }

        protected override SphereSpatialData CreateSpatialEntry(Vector3 worldPos, float extent)
        {
            float halfExtent = extent * 0.5f;
            float radius = halfExtent * Mathf.Sqrt(3f);
            return new SphereSpatialData { Position = worldPos, Radius = radius };
        }
    }
}
