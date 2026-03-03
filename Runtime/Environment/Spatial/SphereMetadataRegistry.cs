using System;

namespace Rayforge.Core.Environment.Spatial
{
    /// <summary>
    /// A specialized registry for entities using sphere-based culling data.
    /// The 'Render' data remains generic to support different techniques (e.g., Atlas Mapping, Matrices, or Billboards).
    /// </summary>
    /// <typeparam name="TKey">The unique identifier type.</typeparam>
    /// <typeparam name="TRender">The visual data struct used for rendering/shader input.</typeparam>
    public class SphereMetadataRegistry<TKey, TRender> : SpatialMetadataRegistry<TKey, SphereSpatialData, TRender>
        where TKey : struct, IEquatable<TKey>
        where TRender : unmanaged
    {
        /// <summary>
        /// Initializes a new sphere-based registry.
        /// </summary>
        /// <param name="capacity">Total number of slots.</param>
        /// <param name="batchSize">Granularity for dirty tracking.</param>
        public SphereMetadataRegistry(int capacity, int batchSize) : base(capacity, batchSize) { }

        /// <summary>
        /// Returns the inactive state for spheres (Radius = -1) to the base registry.
        /// This ensures the GPU culler skips these indices immediately.
        /// </summary>
        protected override SphereSpatialData GetInvalidCullingData() => SphereSpatialData.Inactive;
    }
}