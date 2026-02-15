using System;

namespace Rayforge.Core.Environment.Spatial
{
    /// <summary>
    /// Registry for AABB-based spatial entities.
    /// Invalidation state: IsActive = 0.0f (default).
    /// </summary>
    public class AABBMetadataRegistry<TKey> : SpatialMetadataRegistry<TKey, AABBSpatialData, MatrixSpatialData>
        where TKey : struct, IEquatable<TKey>
    {
        public AABBMetadataRegistry(int capacity, int batchSize) : base(capacity, batchSize) { }

        protected override AABBSpatialData GetInvalidSpatialData() => default;
    }
}