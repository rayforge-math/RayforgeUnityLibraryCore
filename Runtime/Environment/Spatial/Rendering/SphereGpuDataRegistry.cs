using Rayforge.Core.Collections.Abstractions;
using Rayforge.Core.Environment.Spatial.Rendering;
using System;
using UnityEngine;

namespace Rayforge.Core
{
    public class SphereGpuDataRegistry<TKey, TRender> : SpatialGpuDataRegistry<TKey, SphereSpatialData, TRender>
        where TKey : struct, IEquatable<TKey>
        where TRender : unmanaged, IGpuData<TRender>
    {
        public SphereGpuDataRegistry(int capacity, int batchSize) : base(capacity, batchSize)
        { }
    }
}
