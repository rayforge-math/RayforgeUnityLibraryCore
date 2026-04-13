using Rayforge.Core.Collections.Abstractions;
using Rayforge.Core.Execution.Abstractions;

namespace Rayforge.Core.Environment.Abstractions
{
    /// <summary>
    /// Specialized contract for registries that manage both culling (visibility) and rendering (atlas/shader) metadata.
    /// Inherits from IMetadataRegistry to provide a unified interface for 
    /// all registries participating in the culling-render pipeline.
    /// </summary>
    public interface ISpatialMetadataRegistry : IMetadataRegistry
    {
        #region Metadata Stores

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

        #endregion

        #region High-Performance Sync (Zero-Allocation)

        /// <summary>
        /// Executes an action for each modified culling data segment.
        /// Optimized for high-speed GPU buffer uploads without heap allocation.
        /// </summary>
        /// <typeparam name="TAction">A struct implementing IIterationAction for BufferSegmentMeta.</typeparam>
        /// <param name="action">The action to execute for each dirty segment. Passed by reference.</param>
        /// <param name="merge">Whether to combine contiguous dirty batches into single segments.</param>
        void ForEachCullingDirty<TAction>(ref TAction action, bool merge = true)
            where TAction : struct, IExecutionHandler<BufferSegmentMeta>;

        /// <summary>
        /// Executes an action for each modified rendering/atlas data segment.
        /// </summary>
        /// <typeparam name="TAction">A struct implementing IIterationAction for BufferSegmentMeta.</typeparam>
        /// <param name="action">The action to execute for each dirty segment. Passed by reference.</param>
        /// <param name="merge">Whether to combine contiguous dirty batches into single segments.</param>
        void ForEachRenderDirty<TAction>(ref TAction action, bool merge = true)
            where TAction : struct, IExecutionHandler<BufferSegmentMeta>;

        /// <summary>
        /// Executes an action for synchronized dirty segments from both stores simultaneously.
        /// Use this to ensure Culling and Render buffers stay in perfect sync within a single frame.
        /// </summary>
        /// <typeparam name="TAction">A struct implementing IIterationAction for SyncedBufferSegmentMeta.</typeparam>
        /// <param name="action">The action to execute for each synced segment. Passed by reference.</param>
        /// <param name="batchesPerWindow">How many dirty batches to process in one sync window.</param>
        void ForEachSyncedDirty<TAction>(ref TAction action, int batchesPerWindow = 1)
            where TAction : struct, IExecutionHandler<SyncedBufferSegmentMeta>;

        #endregion

        #region Flexible Iteration (Boxing)

        /// <summary>
        /// Provides an iterator over modified culling data segments.
        /// CAUTION: This boxes the internal struct iterator. Use ForEachCullingDirty for hot paths.
        /// </summary>
        /// <param name="merge">Whether to combine contiguous dirty batches into single segments.</param>
        /// <returns>A boxed IIterator instance for culling dirty segments.</returns>
        IIterator<BufferSegmentMeta> GetCullingDirtyIterator(bool merge = true);

        /// <summary>
        /// Provides an iterator over modified rendering/atlas data segments.
        /// </summary>
        /// <param name="merge">Whether to combine contiguous dirty batches into single segments.</param>
        /// <returns>A boxed IIterator instance for render dirty segments.</returns>
        IIterator<BufferSegmentMeta> GetRenderDirtyIterator(bool merge = true);

        /// <summary>
        /// Provides a synchronized iterator that yields dirty segments from both stores.
        /// </summary>
        /// <param name="batchesPerWindow">How many dirty batches to process in one sync window.</param>
        /// <returns>A boxed IIterator instance for synchronized segments.</returns>
        IIterator<SyncedBufferSegmentMeta> GetSyncedDirtyIterator(int batchesPerWindow = 1);

        #endregion

        #region Dirty State Management

        /// <summary>
        /// Clears the dirty tracking state only for the culling store.
        /// Call this after the culling GPU buffer has been successfully synchronized.
        /// </summary>
        void ClearCullingDirty();

        /// <summary>
        /// Clears the dirty tracking state only for the render store.
        /// Call this after the rendering/atlas GPU buffer has been successfully synchronized.
        /// </summary>
        void ClearRenderDirty();

        /// <summary>
        /// Clears the dirty tracking state for both culling and rendering stores at once.
        /// </summary>
        void ClearAllDirty();

        #endregion
    }
}