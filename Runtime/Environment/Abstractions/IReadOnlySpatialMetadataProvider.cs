using Rayforge.Core.Collections.Abstractions;
using System;

namespace Rayforge.Core.Environment.Abstractions
{
    /// <summary>
    /// Provides read-only access to spatial and visual metadata stores.
    /// Used by systems that need to consume data (e.g., Renderer, Culling-Systems).
    /// </summary>
    public interface IReadOnlySpatialMetadataProvider<TKey, TCulling, TRender>
        : IReadOnlyGpuDataProvider<TKey>
        where TKey : struct, IEquatable<TKey>
        where TCulling : unmanaged, ISpatialData
        where TRender : unmanaged
    {
        #region Low-Level Access (Interop)

        /// <summary> Gets the zero-allocation span for hot-path iteration (Culling). </summary>
        ReadOnlySpan<TCulling> CullingAsSpan();

        /// <summary> Gets the zero-allocation span for hot-path iteration (Render). </summary>
        ReadOnlySpan<TRender> RenderAsSpan();

        #endregion

        #region Data Access

        /// <summary> Tries to retrieve the current spatial and visual data for a given key. </summary>
        bool TryGetMetadata(TKey key, out TCulling culling, out TRender render);
        /// <summary> Tries to retrieve only the spatial data. </summary>
        bool TryGetCulling(TKey key, out TCulling culling);
        /// <summary> Tries to retrieve only the visual data. </summary>
        bool TryGetRender(TKey key, out TRender render);
        /// <summary> Checks if a specific key is currently registered. </summary>
        bool Contains(TKey key);

        #endregion
    }
}
