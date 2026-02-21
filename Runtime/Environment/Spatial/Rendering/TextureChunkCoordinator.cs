using Rayforge.Core.Environment.Abstractions;
using Rayforge.Core.Environment.Spatial.Chunks;
using Rayforge.Core.Environment.Spatial.Rendering;
using Rayforge.Core.Rendering.Abstractions;
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
    public class TextureChunkCoordinator<TChunk> where TChunk : LODChunk<TChunk>, ITextureMapped
    {
        private readonly LodAtlasMapper<Vector3Int> _mapper;
        private readonly LODChunkRegistry<TChunk> _chunkRegistry;

        private readonly HashSet<Vector3Int> _toAssign = new();
        private readonly HashSet<Vector3Int> _toRelease = new();

        public TextureChunkCoordinator(LodAtlasMapper<Vector3Int> mapper, LODChunkRegistry<TChunk> registry)
        {
            _mapper = mapper;
            _chunkRegistry = registry;

            foreach (var chunk in _chunkRegistry.AllEntries)
            {
                SetupChunk(chunk);
            }
        }

        /// <summary>
        /// Phase 1: Update Mapping State.
        /// Feeds registry changes into the mapper. Call this after the LOD update.
        /// </summary>
        public void UpdateTopology(ISpatialCollection masterSource)
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

        private void SetupChunk(TChunk chunk)
        {
            chunk.OnLODChanged += (sender, old, @new) => {
                if (old >= 0) _toRelease.Add(sender.GridKey);
                if (@new >= 0) _toAssign.Add(sender.GridKey);
            };
            chunk.OnCleanup += sender => _toRelease.Add(sender.GridKey);
        }
    }
}