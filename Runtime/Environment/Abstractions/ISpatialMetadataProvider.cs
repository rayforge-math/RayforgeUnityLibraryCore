using Rayforge.Core.Collections.Abstractions;
using Rayforge.Core.Execution.Abstractions;
using System;

namespace Rayforge.Core.Environment.Abstractions
{
    /// <summary>
    /// Provides a standardized, high-performance interface for registries involved in GPU-upload pipelines.
    /// Manages data stores, dirty-state tracking, and synchronized iteration across spatial and visual data.
    /// </summary>
    /// <typeparam name="TKey">The unique identifier type for entities.</typeparam>
    /// <typeparam name="TCulling">The struct type used for GPU culling (e.g., SphereSpatialData).</typeparam>
    /// <typeparam name="TRender">The struct type used for GPU rendering (e.g., MatrixSpatialData).</typeparam>
    public interface ISpatialMetadataProvider<TKey, TCulling, TRender>
        : IReadOnlySpatialMetadataProvider<TKey, TCulling, TRender>
        where TKey : struct, IEquatable<TKey>
        where TCulling : unmanaged, IGpuData<TCulling>
        where TRender : unmanaged, IGpuData<TRender>
    {
        #region Low-Level Access (Interop)

        /// <summary> Gets the untyped array for legacy/UI operations (Culling). </summary>
        Array CullingUntypedBuffer { get; }
        /// <summary> Gets the typed array for CPU-side interop (Culling). </summary>
        TCulling[] CullingTypedBuffer { get; }

        /// <summary> Gets the untyped array for legacy/UI operations (Render). </summary>
        Array RenderUntypedBuffer { get; }
        /// <summary> Gets the typed array for CPU-side interop (Render). </summary>
        TRender[] RenderTypedBuffer { get; }

        #endregion

        #region Data Modification

        /// <summary> Updates both spatial and visual data for a specific key. Allocates a new slot if the key is unknown. </summary>
        int SetMetadata(TKey key, TCulling culling, TRender render);
        /// <summary> Updates only the spatial/culling data for a key. </summary>
        int SetCulling(TKey key, TCulling culling);
        /// <summary> Updates only the visual/atlas data for a key. </summary>
        int SetRender(TKey key, TRender render);

        /// <summary> Fully releases the key and ensures the GPU data is invalidated. </summary>
        int ReleaseAndKill(TKey key);

        #endregion
    }
}