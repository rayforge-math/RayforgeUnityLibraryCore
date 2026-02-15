using Rayforge.Core.Rendering.Collections.Buffered;
using System;

namespace Rayforge.Core.Environment.Spatial
{
    /// <summary>
    /// Generic bridge for registries that participate in a spatial culling and rendering pipeline.
    /// This class enforces the presence of spatial (culling) and visual (rendering) data stores.
    /// </summary>
    /// <typeparam name="TKey">The unique identifier type for the entities (e.g., Vector3Int for Chunks).</typeparam>
    /// <typeparam name="TSpatial">The struct type used for GPU culling (e.g., SphereSpatialData).</typeparam>
    /// <typeparam name="TVisual">The struct type used for GPU rendering (e.g., MatrixSpatialData).</typeparam>
    public abstract class SpatialMetadataRegistry<TKey, TSpatial, TVisual> : MetadataRegistry<TKey>
        where TKey : struct, IEquatable<TKey>
        where TSpatial : unmanaged
        where TVisual : unmanaged
    {
        /// <summary>
        /// The primary data store for spatial/culling information. 
        /// Usually consumed by compute shaders for frustum or occlusion culling.
        /// </summary>
        public MetadataStore<TSpatial> SpatialStore { get; }

        /// <summary>
        /// The primary data store for visual/transformation information. 
        /// Usually contains matrices or other vertex-relevant data.
        /// </summary>
        public MetadataStore<TVisual> VisualStore { get; }

        /// <summary>
        /// Initializes a new spatial registry and automatically registers the mandatory spatial and visual stores.
        /// </summary>
        /// <param name="capacity">Maximum number of slots available in the registry.</param>
        /// <param name="batchSize">Size of a single dirty-tracking batch for optimized GPU uploads.</param>
        protected SpatialMetadataRegistry(int capacity, int batchSize) : base(capacity, batchSize)
        {
            // English: Pre-registering the mandatory pipeline stores to ensure consistent memory layout
            SpatialStore = AddStore<TSpatial>();
            VisualStore = AddStore<TVisual>();
        }

        /// <summary>
        /// Updates both spatial and visual data for a specific key using a single index lookup.
        /// If the key does not exist, a new slot is automatically allocated.
        /// </summary>
        /// <param name="key">The unique identifier of the entity.</param>
        /// <param name="spatial">The new spatial/culling data.</param>
        /// <param name="visual">The new visual/rendering data.</param>
        public void SetSpatial(TKey key, TSpatial spatial, TVisual visual)
        {
            int idx = GetOrAllocateIndex(key);
            SpatialStore.Set(idx, spatial);
            VisualStore.Set(idx, visual);
        }

        /// <summary>
        /// Tries to retrieve the current spatial and visual data for a given key.
        /// </summary>
        /// <param name="key">The unique identifier of the entity.</param>
        /// <param name="spatial">The current spatial data (out).</param>
        /// <param name="visual">The current visual data (out).</param>
        /// <returns>True if the key was found and data was retrieved.</returns>
        public bool TryGetSpatial(TKey key, out TSpatial spatial, out TVisual visual)
        {
            if (TryGetIndex(key, out int index))
            {
                spatial = SpatialStore.Get(index);
                visual = VisualStore.Get(index);
                return true;
            }

            spatial = default;
            visual = default;
            return false;
        }

        /// <summary>
        /// Checks if a specific key is currently registered and has an allocated slot.
        /// </summary>
        /// <param name="key">The key to check.</param>
        /// <returns>True if the key exists in the registry.</returns>
        public bool Contains(TKey key) => TryGetIndex(key, out _);

        /// <summary>
        /// Marks all data in all registered stores as dirty, forcing a full GPU re-upload.
        /// Useful after a graphics context loss or buffer recreation.
        /// </summary>
        public void MarkAllDirty()
        {
            SpatialStore.MarkAllDirty();
            VisualStore.MarkAllDirty();
        }
    }
}