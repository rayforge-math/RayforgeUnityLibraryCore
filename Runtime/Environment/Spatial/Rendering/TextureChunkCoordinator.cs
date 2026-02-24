using Rayforge.Core.Common.Rendering;
using Rayforge.Core.Environment.Abstractions;
using Rayforge.Core.Environment.Spatial.Chunks;
using Rayforge.Core.Environment.Spatial.Rendering;
using Rayforge.Core.Rendering.EditorStructures;
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
        private const string Tag = "[TextureCoordinator]";

        private readonly LodAtlasMapper<Vector3Int> _mapper = new();
        private readonly LODChunkRegistry<TextureLodChunk> _chunkRegistry = new();

        private readonly HashSet<Vector3Int> _toAssign = new();
        private readonly HashSet<Vector3Int> _toRelease = new();

        #region GPU Buffer Metadata Access

        /// <summary>
        /// The total number of slices required in the Texture2DArray to fit all LOD levels.
        /// Use this value as the 'depth' when allocating your Texture2DArray.
        /// </summary>
        public int RequiredSliceCount => _mapper?.RequiredSliceCount ?? 0;

        /// <summary>
        /// The reference resolution of a single slot at LOD 0.
        /// This defines the width and height of the Texture2DArray.
        /// </summary>
        public PowerOfTwoResolution BaseResolution => _mapper?.BaseResolution ?? default;

        /// <summary>
        /// Gets the total capacity (number of slots) required for the GPU buffers.
        /// Use this as the 'count' parameter for ComputeBuffer allocation.
        /// </summary>
        public int BufferCapacity => _mapper?.Registry.Capacity ?? 0;

        /// <summary>
        /// Gets the number of entries processed per dirty-tracking batch.
        /// Used to align GPU buffer updates and optimize the transfer of modified data.
        /// </summary>
        public int BatchSize => _mapper?.Registry.BatchSize ?? 0;

        /// <summary>
        /// Gets the stride (byte size) for the spatial data buffer.
        /// </summary>
        public int SpatialStride => _mapper?.Registry.SpatialMetadata.Stride ?? 0;

        /// <summary>
        /// Gets the stride (byte size) for the visual mapping buffer.
        /// </summary>
        public int VisualStride => _mapper?.Registry.VisualMetadata.Stride ?? 0;

        /// <summary>
        /// Gets the highest index currently in use to optimize Compute Shader dispatch.
        /// Helps avoiding unnecessary thread groups on the GPU.
        /// </summary>
        public int HighestActiveIndex => _mapper?.Registry.HighestIndex ?? -1;

        #endregion

        #region Properties

        /// <summary>
        /// Provides read-only access to the LOD configuration and spatial queries.
        /// Returns null if not initialized.
        /// </summary>
        public ILODGridProvider<Vector3Int> LodGridProvider => _chunkRegistry;

        /// <summary>
        /// Checks if the coordinator has been initialized with a valid registry and mapper.
        /// </summary>
        public bool IsInitialized =>
            _mapper != null && _mapper.IsInitialized &&
            _chunkRegistry != null && _chunkRegistry.IsInitialized;

        #endregion

        #region Lifecycle

        /// <summary>
        /// Initializes the coordinator by extracting spatial and visual configurations from the provided LOD definitions.
        /// Acts as the central bridge to synchronize the LOD registry and the atlas mapping logic.
        /// </summary>
        /// <param name="spatialSettings">The core grid configuration (size, active axes, etc.).</param>
        /// <param name="lodConfigs">The master list of LOD levels, defining both distances and texture resolutions.</param>
        /// <param name="batchSize">Number of elements to process in a single GPU update block.</param>
        /// <param name="viewer">The transform used to calculate distances for LOD switching.</param>
        /// <param name="container">The parent transform where chunk GameObjects will be organized.</param>
        /// <param name="deactivateOnCulled">If true, chunks outside the maximum LOD range will be disabled.</param>
        public void Initialize(
            SpatialSettings spatialSettings,
            ReadOnlySpan<TextureLOD> lodConfigs,
            int batchSize,
            Transform viewer,
            Transform container,
            bool deactivateOnCulled = true)
        {
            try
            {
                Reset();

                ExtractLodConfiguration(lodConfigs, deactivateOnCulled, out var lodSettings, out var resolutions);

                _chunkRegistry.Initialize(spatialSettings, lodSettings, viewer, container);
                _mapper.Initialize(_chunkRegistry, resolutions, batchSize);
            }
            catch (Exception e)
            {
                throw new Exception($"{Tag} Initialization failed. Check your LOD settings and spatial configuration: {e.Message}", e);
            }
        }

        /// <summary>
        /// Clears all runtime data (chunks and mapping) but keeps the coordinator initialized.
        /// Use this for a soft restart without destroying the container or settings.
        /// </summary>
        public void Clear()
        {
            _chunkRegistry?.Clear();

            _mapper?.Clear();

            _toAssign.Clear();
            _toRelease.Clear();
        }

        /// <summary>
        /// Clears all chunks and resets the atlas mapping, but keeps the coordinator alive.
        /// </summary>
        public void Reset()
        {
            Clear();
            _chunkRegistry?.Reset();
        }

        #endregion

        #region High-Frequency Updates

        /// <summary>
        /// Updates the viewer position for LOD calculations. 
        /// Safe to call every frame or when the camera changes.
        /// </summary>
        public void SetViewer(Transform viewer)
        {
            if (_chunkRegistry == null || !_chunkRegistry.IsInitialized) return;
            _chunkRegistry.SetViewer(viewer);
        }

        /// <summary>
        /// Shifts the coordinate system anchor to support large-scale world movement.
        /// </summary>
        public void SetAnchor(Vector3 newAnchor)
        {
            if (_chunkRegistry == null || !_chunkRegistry.IsInitialized) return;
            _chunkRegistry.SetAnchor(newAnchor);
        }

        /// <summary>
        /// Shifts the coordinate system anchor to support large-scale world movement.
        /// Updates the registry and ensures the spatial mapping stays consistent.
        /// </summary>
        public void NotifyOriginShift(Vector3 delta)
        {
            if (_chunkRegistry == null || !_chunkRegistry.IsInitialized) return;
            _chunkRegistry.NotifyOriginShift(delta);
        }

        /// <summary>
        /// Updates all chunks' LOD state based on current viewer position.
        /// Call this before UpdateTopology.
        /// </summary>
        public void UpdateLODs()
        {
            if (_chunkRegistry == null || !_chunkRegistry.IsInitialized) return;
            _chunkRegistry.UpdateLODs();
        }

        #endregion

        #region Low-Frequency Updates

        /// <summary>
        /// Passes new LOD distance thresholds directly to the registry.
        /// After updating thresholds, we refresh all chunks to apply the new logic.
        /// </summary>
        public void UpdateLodDistances(ReadOnlySpan<TextureLOD> lodConfigs)
        {
            if (!IsInitialized) return;

            ExtractLodConfiguration(lodConfigs, _chunkRegistry.DeactivateOnCulled, out var lodSettings, out var resolutions);

            if (_chunkRegistry.UpdateLodDistances(lodSettings.LodDistances))
            {
                UpdateLODs();
            }

            _mapper.Initialize(_chunkRegistry, resolutions, _mapper.Registry.BatchSize);
        }

        #endregion

        #region Rendering Pipeline

        /// <summary>
        /// Phase 1: Update Mapping State.
        /// Feeds registry changes into the mapper. Call this after the LOD update.
        /// </summary>
        public void UpdateTopology(ISpatialCollection<Vector3Int> masterSource)
        {
            if (!IsInitialized) return;

            try
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
            catch (Exception e)
            {
                throw new Exception($"{Tag} Topology update failed: {e.Message}", e);
            }
        }

        /// <summary>
        /// Phase 2: Execution.
        /// Iterates over the result set. This allows multiple bake-passes (Height, Splat, etc.).
        /// </summary>
        public void ExecuteBake(Action<Vector3Int, TextureMappingData> onBakeTile)
        {
            if (!IsInitialized) return;

            try
            {
                _mapper.BroadcastMappings((key, mapping) =>
                {
                    if (_chunkRegistry.TryGetEntry(key, out var chunk))
                    {
                        chunk.SetTextureMapping(mapping);
                    }

                    onBakeTile?.Invoke(key, mapping);
                });
            }
            catch (Exception e)
            {
                throw new Exception($"{Tag} Bake execution failed: {e.Message}", e);
            }
            finally
            {
                _mapper.ClearMappingUpdates();
            }
        }

        #endregion

        #region Texture Chunk Setup

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

        #endregion

        #region Helpers

        /// <summary>
        /// Centralized helper to extract distances and resolutions from the master config.
        /// </summary>
        private void ExtractLodConfiguration(
            ReadOnlySpan<TextureLOD> lodConfigs,
            bool deactivateOnCulled,
            out LodSettings lodSettings,
            out ReadOnlySpan<PowerOfTwoResolution> resolutions)
        {
            if (lodConfigs.Length == 0)
                throw new ArgumentException("LOD configurations cannot be empty.");

            float[] distances = new float[lodConfigs.Length];
            PowerOfTwoResolution[] resArray = new PowerOfTwoResolution[lodConfigs.Length];

            for (int i = 0; i < lodConfigs.Length; i++)
            {
                distances[i] = lodConfigs[i].distanceThreshold;
                resArray[i] = lodConfigs[i].mapResolution;
            }

            lodSettings = new LodSettings
            {
                LodDistances = distances,
                DeactivateOnCulled = deactivateOnCulled
            };
            resolutions = resArray;
        }

        #endregion
    }
}