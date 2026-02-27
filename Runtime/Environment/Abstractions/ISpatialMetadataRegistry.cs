using Rayforge.Core.Collections.Abstractions;
using Rayforge.Core.Rendering.Abstractions;
using Rayforge.Core.Rendering.Collections.Buffered;
using Rayforge.Core.Rendering.Collections.Iterator;

namespace Rayforge.Core.Environment.Abstractions
{
    /// <summary>
    /// Specialized contract for registries that manage both spatial (culling) and visual (rendering) metadata.
    /// Inherits from IMetadataRegistry to provide a unified interface for 
    /// all registries participating in the spatial-visual pipeline.
    /// </summary>
    public interface ISpatialMetadataRegistry : IMetadataRegistry
    {
        /// <summary>
        /// Gets the untyped metadata store for spatial/culling information.
        /// </summary>
        IMetadataStore SpatialMetadata { get; }

        /// <summary>
        /// Gets the untyped metadata store for visual/transformation information.
        /// </summary>
        IMetadataStore VisualMetadata { get; }

        /// <summary>
        /// Provides an iterator over modified spatial data segments.
        /// Use this to perform optimized GPU uploads for the culling buffer.
        /// </summary>
        IIterator<BufferSegmentMeta> SpatialDirtyIterator { get; }

        /// <summary>
        /// Provides an iterator over modified visual data segments.
        /// Use this to perform optimized GPU uploads for the rendering/atlas buffer.
        /// </summary>
        IIterator<BufferSegmentMeta> VisualDirtyIterator { get; }

        /// <summary>
        /// Clears the dirty tracking state only for the spatial store.
        /// Call this after the spatial GPU buffer has been synchronized.
        /// </summary>
        void ClearSpatialDirty();

        /// <summary>
        /// Clears the dirty tracking state only for the visual store.
        /// Call this after the visual GPU buffer has been synchronized.
        /// </summary>
        void ClearVisualDirty();

        /// <summary>
        /// Clears the dirty tracking state for both spatial and visual stores at once.
        /// </summary>
        void ClearAllDirty();
    }
}