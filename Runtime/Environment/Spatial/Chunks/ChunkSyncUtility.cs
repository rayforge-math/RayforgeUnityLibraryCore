using Rayforge.Core.Environment.Abstractions;
using System;
using UnityEngine;

namespace Rayforge.Core.Environment.Spatial.Chunks
{
    /// <summary>
    /// Provides agnostic utility methods to synchronize spatial data collections with chunk-based registries.
    /// This utility acts as a bridge, ensuring that visual or logical chunks exist only where data is present.
    /// </summary>
    public static class ChunkSyncUtility
    {
        /// <summary>
        /// Synchronizes a spatial data source with a chunk registry by comparing "dirty" cells.
        /// Uses a data passthrough pattern to ensure callbacks can be static and allocation-free.
        /// </summary>
        /// <typeparam name="TChunk">The type of the chunk shell. Must be a reference type.</typeparam>
        /// <typeparam name="TData">The type of the configuration data passed to the lifecycle callbacks.</typeparam>
        /// <param name="spatialData">The source of truth containing tracked objects in a grid.</param>
        /// <param name="chunkRegistry">The registry managing the lifecycle and storage of the chunk shells.</param>
        /// <param name="data">The state object passed to onCreate and onDataChanged (prevents closures).</param>
        /// <param name="onCreate">
        /// Callback invoked when a new chunk is created. 
        /// Use static lambdas here to avoid GC pressure during synchronization.
        /// </param>
        /// <param name="onDataChanged">
        /// Callback invoked when a chunk already exists but the underlying spatial data has changed.
        /// </param>
        public static void Synchronize<TChunk, TData>(
            ISpatialCollection<Vector3Int> spatialData,
            ChunkRegistry<TChunk> chunkRegistry,
            TData data,
            Action<TChunk, TData> onCreate,
            Action<TChunk, TData> onDataChanged)
            where TChunk : Chunk<TChunk>
        {
            if (!spatialData.IsInitialized) return;

            foreach (var key in spatialData.GetDirtyCells())
            {
                bool hasData = spatialData.HasEntriesInCell(key);
                bool exists = chunkRegistry.TryGetEntry(key, out TChunk chunk);

                if (hasData)
                {
                    if (exists)
                    {
                        onDataChanged?.Invoke(chunk, data);
                    }
                    else
                    {
                        chunkRegistry.GetOrCreateChunk(key, data, onCreate, out _);
                    }
                }
                else if (exists)
                {
                    chunkRegistry.RemoveAndDestroy(key);
                }
            }
        }
    }
}