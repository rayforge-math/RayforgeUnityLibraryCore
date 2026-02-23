using Rayforge.Core.Rendering.Abstractions;
using Rayforge.Core.Rendering.Collections.Buffered;
using System;

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
        /// Convenience bridge for external systems to extract changes from both stores at once.
        /// </summary>
        void ExtractChanges(Action<Array, int, int> onSpatialChanged, Action<Array, int, int> onVisualChanged);
    }
}