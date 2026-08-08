using Rayforge.Core.Collections.Abstractions;
using System;
using UnityEngine;

namespace Rayforge.Core.Environment.Spatial.Rendering
{
    public class AabbGpuDataRegistry<TKey, TRender> : SpatialGpuDataRegistry<TKey, AabbSpatialData, TRender>
        where TKey : struct, IEquatable<TKey>
        where TRender : unmanaged, IGpuData<TRender>
    {
        public AabbGpuDataRegistry(int capacity, int batchSize) : base(capacity, batchSize)
        { }
    }
}
