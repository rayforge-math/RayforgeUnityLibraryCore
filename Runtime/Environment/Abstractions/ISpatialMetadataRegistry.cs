using Rayforge.Core.Collections.Abstractions;

namespace Rayforge.Core.Environment.Abstractions
{
    /// <summary>
    /// Specialized contract for registries that manage both culling (visibility) and rendering (atlas/shader) metadata.
    /// Inherits from IMetadataRegistry to provide a unified interface for 
    /// all registries participating in the culling-render pipeline.
    /// </summary>
    public interface ISpatialMetadataRegistry : IMetadataRegistry
    {
        /// <summary>
        /// Gets the untyped metadata store for culling information (e.g., Position, Radius).
        /// Use this to manage the GPU buffer for spatial culling passes.
        /// </summary>
        IMetadataStore CullingMetadata { get; }

        /// <summary>
        /// Gets the untyped metadata store for rendering/atlas information (e.g., UVs, Slices).
        /// Use this to manage the GPU buffer for the final fragment shader.
        /// </summary>
        IMetadataStore RenderMetadata { get; }

        /// <summary>
        /// Provides an iterator over modified culling data segments.
        ///  Use merge = true for immediate, efficient GPU uploads of spatial changes.
        /// </summary>
        /// <param name="merge">Whether to combine contiguous dirty batches into single segments.</param>
        IIterator<BufferSegmentMeta> GetCullingDirtyIterator(bool merge = true);

        /// <summary>
        /// Provides an iterator over modified rendering/atlas data segments.
        /// Use merge = false for staggered processing (e.g., texture baking budget).
        /// </summary>
        /// <param name="merge">Whether to combine contiguous dirty batches into single segments.</param>
        IIterator<BufferSegmentMeta> GetRenderDirtyIterator(bool merge = true);

        /// <summary>
        /// Provides a synchronized iterator that yields dirty segments from both stores simultaneously.
        /// Use this for unified synchronization where spatial and visual data must stay in sync.
        /// </summary>
        /// <param name="merge">If true, uses "Greedy Windowing" to combine adjacent dirty areas into larger sync windows.</param>
        IIterator<SyncedBufferSegmentMeta> GetSyncIterator(bool merge = true);

        /// <summary>
        /// Clears the dirty tracking state only for the culling store.
        /// Call this after the culling GPU buffer has been synchronized.
        /// </summary>
        void ClearCullingDirty();

        /// <summary>
        /// Clears the dirty tracking state only for the render store.
        /// Call this after the rendering/atlas GPU buffer has been synchronized.
        /// </summary>
        void ClearRenderDirty();

        /// <summary>
        /// Clears the dirty tracking state for both culling and rendering stores at once.
        /// </summary>
        void ClearAllDirty();
    }
}