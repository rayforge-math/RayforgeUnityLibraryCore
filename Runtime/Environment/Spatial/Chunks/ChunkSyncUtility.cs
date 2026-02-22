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
        /// It handles the lifecycle (creation, update, removal) of chunks without being coupled to specific 
        /// logic like LODs, Atlasing, or Baking.
        /// </summary>
        /// <typeparam name="TChunk">
        /// The type of the chunk shell. Must be a reference type.
        /// </typeparam>
        /// <param name="spatialData">
        /// The source of truth containing tracked objects in a grid.
        /// </param>
        /// <param name="chunkRegistry">
        /// The registry managing the lifecycle and storage of the chunk shells.
        /// </param>
        /// <param name="onCreate">
        /// Callback invoked when a new chunk is created. 
        /// Use this to initialize the chunk (e.g., subscribe to LOD events).
        /// </param>
        /// <param name="onDataChanged">
        /// Callback invoked when a chunk already exists but the underlying 
        /// spatial data in its cell has been modified (e.g., to trigger a re-bake).
        /// </param>
        public static void Synchronize<TChunk>(
            ISpatialCollection<Vector3Int> spatialData,
            ChunkRegistry<TChunk> chunkRegistry,
            Action<TChunk> onCreate,
            Action<TChunk> onDataChanged) 
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
                        onDataChanged?.Invoke(chunk);
                    }
                    else
                    {
                        chunkRegistry.GetOrCreateChunk(key, onCreate, out _);
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