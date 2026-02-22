using Rayforge.Core.Environment.Abstractions;
using Rayforge.Core.Environment.Spatial.Chunks;
using Rayforge.Core.Environment.Spatial.Rendering;
using Rayforge.Core.Rendering.Textures;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Rayforge.Core.Environment.Spatial.Surfaces
{
    /// <summary>
    /// Orchestrates the synchronization between world chunks and the atlas mapping logic.
    /// Directly uses the LodAtlasMapper to manage slots and provides bake callbacks.
    /// </summary>
    public class TextureChunkCoordinator
    {
        private readonly LodAtlasMapper<Vector3Int> _mapper;
        private readonly LODChunkRegistry<TextureLodChunk> _chunkRegistry;

        private readonly HashSet<Vector3Int> _toAssign = new();
        private readonly HashSet<Vector3Int> _toRelease = new();

        public TextureChunkCoordinator(SpatialSettings spatialSettings, LodSettings lodSettings, Transform viewer, Transform container)
        {
            _mapper = new LodAtlasMapper<Vector3Int>();
            _chunkRegistry = new LODChunkRegistry<TextureLodChunk>(spatialSettings, lodSettings, viewer, container);

            foreach (var chunk in _chunkRegistry.AllEntries)
            {
                SetupChunk(chunk);
            }
        }

        /// <summary>
        /// Phase 1: Update Mapping State.
        /// Feeds registry changes into the mapper. Call this after the LOD update.
        /// </summary>
        public void UpdateTopology(ISpatialCollection<Vector3Int> masterSource)
        {
            ChunkSyncUtility.Synchronize(
                masterSource,
                _chunkRegistry,
                onCreate: chunk => {
                    SetupChunk(chunk);
                    if (chunk.IsVisible) _toAssign.Add(chunk.GridKey);
                },
                onDataChanged: chunk => {
                    if (chunk.IsVisible) _toAssign.Add(chunk.GridKey);
                }
            );

            foreach (var key in _toRelease) _mapper.RemoveTile(key);
            _toRelease.Clear();

            foreach (var key in _toAssign)
            {
                if (_chunkRegistry.TryGetEntry(key, out var chunk) && chunk.IsVisible)
                {
                    _mapper.SetTile(key, chunk.CurrentLOD, chunk.WorldPosition, chunk.localExtent.x);
                }
            }
            _toAssign.Clear();

            _mapper.UpdateMappings();
        }

        /// <summary>
        /// Phase 2: Execution.
        /// Iterates over the result set. This allows multiple bake-passes (Height, Splat, etc.).
        /// </summary>
        public void ExecuteBake(Action<Vector3Int, TextureMappingData> onBakeTile)
        {
            _mapper.BroadcastMappings((key, mapping) =>
            {
                if (_chunkRegistry.TryGetEntry(key, out var chunk))
                {
                    chunk.SetTextureMapping(mapping);
                }

                onBakeTile?.Invoke(key, mapping);
            });

            _mapper.ClearMappingUpdates();
        }

        /// <summary>
        /// Sets up a chunk by subscribing to its lifecycle and LOD state events.
        /// Attaches named handlers to ensure clear responsibility and prevent lambda allocations.
        /// </summary>
        /// <param name="chunk">The texture LOD chunk to initialize.</param>
        private void SetupChunk(TextureLodChunk chunk)
        {
            chunk.OnLODChanged += HandleLodChanged;
            chunk.OnCleanup += HandleChunkCleanup;
        }

        /// <summary>
        /// Handles the transition of a chunk between different LOD levels.
        /// Manages the movement of keys between release and assignment queues based on LOD validity.
        /// </summary>
        /// <param name="chunk">The chunk that changed its LOD.</param>
        /// <param name="oldLod">The previous LOD level (negative values indicate no LOD).</param>
        /// <param name="newLod">The new target LOD level (negative values indicate culling).</param>
        private void HandleLodChanged(ILODState chunk, int oldLod, int newLod)
        {
            if (oldLod >= 0)
            {
                _toRelease.Add(chunk.GridKey);
            }

            if (newLod >= 0)
            {
                _toAssign.Add(chunk.GridKey);
            }
        }

        /// <summary>
        /// Handles the cleanup process when a chunk is being destroyed or pooled.
        /// Ensures that the grid key is removed from active processing and resources are freed.
        /// </summary>
        /// <param name="chunk">The chunk being cleaned up.</param>
        private void HandleChunkCleanup(TextureLodChunk chunk)
        {
            _toRelease.Add(chunk.GridKey);

            chunk.OnLODChanged -= HandleLodChanged;
            chunk.OnCleanup -= HandleChunkCleanup;
        }
    }
}