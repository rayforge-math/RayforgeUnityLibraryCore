using System;

namespace Rayforge.Core.Environment.Spatial
{
    /// <summary>
    /// Registry for sphere-based spatial entities.
    /// Invalidation state: Radius = -1.0f.
    /// </summary>
    public class SphereMetadataRegistry<TKey> : SpatialMetadataRegistry<TKey, SphereSpatialData, MatrixSpatialData>
        where TKey : struct, IEquatable<TKey>
    {
        public SphereMetadataRegistry(int capacity, int batchSize) : base(capacity, batchSize) { }

        protected override SphereSpatialData GetInvalidSpatialData() => SphereSpatialData.Inactive;
    }
}