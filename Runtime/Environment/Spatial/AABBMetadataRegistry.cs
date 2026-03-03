using System;

namespace Rayforge.Core.Environment.Spatial
{
    /// <summary>
    /// A specialized registry for entities using Axis-Aligned Bounding Box (AABB) culling data.
    /// The visual data remains generic to allow for different rendering implementations.
    /// </summary>
    /// <typeparam name="TKey">The unique identifier type for the entities.</typeparam>
    /// <typeparam name="TVisual">The visual data struct used for rendering (e.g., Matrix4x4 or custom vertex data).</typeparam>
    public class AABBMetadataRegistry<TKey, TVisual> : SpatialMetadataRegistry<TKey, AABBSpatialData, TVisual>
        where TKey : struct, IEquatable<TKey>
        where TVisual : unmanaged
    {
        /// <summary>
        /// Initializes a new AABB-based registry.
        /// </summary>
        /// <param name="capacity">Maximum number of slots.</param>
        /// <param name="batchSize">Size of dirty-tracking batches.</param>
        public AABBMetadataRegistry(int capacity, int batchSize) : base(capacity, batchSize) { }

        /// <summary>
        /// Returns the default invalid state for AABBs (where IsActive = 0.0f).
        /// </summary>
        /// <returns>An inactive AABB spatial data structure.</returns>
        protected override AABBSpatialData GetInvalidCullingData() => default;
    }
}