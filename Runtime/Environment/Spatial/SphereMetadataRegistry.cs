using System;

namespace Rayforge.Core.Environment.Spatial
{
    /// <summary>
    /// A specialized registry for entities using sphere-based culling data.
    /// The visual data remains generic to support different rendering techniques (e.g., Matrices, Billboards, or custom InstanceData).
    /// </summary>
    /// <typeparam name="TKey">The unique identifier type.</typeparam>
    /// <typeparam name="TVisual">The visual data struct used for rendering.</typeparam>
    public class SphereMetadataRegistry<TKey, TVisual> : SpatialMetadataRegistry<TKey, SphereSpatialData, TVisual>
        where TKey : struct, IEquatable<TKey>
        where TVisual : unmanaged
    {
        /// <summary>
        /// Initializes a new sphere-based registry.
        /// </summary>
        public SphereMetadataRegistry(int capacity, int batchSize) : base(capacity, batchSize) { }

        /// <summary>
        /// Returns the inactive state for spheres (Radius = -1) to the base registry.
        /// </summary>
        protected override SphereSpatialData GetInvalidSpatialData() => SphereSpatialData.Inactive;
    }
}