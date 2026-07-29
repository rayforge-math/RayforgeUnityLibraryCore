using Rayforge.Core.Collections.Abstractions;
using System;

namespace Rayforge.Core.Environment.Abstractions
{
    /// <summary>
    /// Provides read-only access to spatial and visual metadata stores.
    /// Used by systems that need to consume data (e.g., Renderer, Culling-Systems).
    /// </summary>
    public interface IReadOnlySpatialMetadataProvider<TKey, TCulling, TRender>
        where TKey : struct, IEquatable<TKey>
        where TCulling : unmanaged, IGpuData<TCulling>
        where TRender : unmanaged, IGpuData<TRender>
    {
        #region Configuration & Metrics

        /// <summary> Gets the maximum capacity of the registry. </summary>
        int Capacity { get; }

        /// <summary> Gets the batch size used for dirty-tracking and synchronization. </summary>
        int BatchSize { get; }

        /// <summary> Gets the stride (size in bytes) of a single culling data element. </summary>
        int CullingStride { get; }

        /// <summary> Gets the stride (size in bytes) of a single render data element. </summary>
        int RenderStride { get; }

        /// <summary> Gets the highest active index currently allocated in the registry. </summary>
        int HighestIndex { get; }

        #endregion

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
