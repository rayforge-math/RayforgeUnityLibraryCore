using Rayforge.Core.Common.Rendering;
using Rayforge.Core.Environment.Abstractions;
using Rayforge.Core.Environment.Spatial.Chunks;
using Rayforge.Core.Environment.Spatial.Rendering;
using Rayforge.Core.Execution.Abstractions;
using Rayforge.Core.Execution.Handler;
using System;
using System.Runtime.CompilerServices;
using UnityEngine;

namespace Rayforge.Core.Environment.Spatial.Surfaces
{
    /// <summary>
    /// Orchestrates the synchronization between world chunks and the atlas mapping logic.
    /// Directly uses the LodAtlasMapper to manage slots and provides bake callbacks.
    /// </summary>
    public class TextureChunkCoordinator
    {
        private readonly SphereAtlasMapper<Vector3Int> m_Mapper = new();
        private readonly LODChunkRegistry<TextureChunk> m_ChunkRegistry = new();

        #region Properties

        /// <summary>
        /// The total number of slices required in the Texture2DArray to fit all LOD levels.
        /// Use this value as the 'depth' when allocating your Texture2DArray.
        /// </summary>
        public int RequiredSliceCount => m_Mapper?.RequiredSliceCount ?? 0;

        /// <summary>
        /// The reference resolution of a single slot at LOD 0.
        /// This defines the width and height of the Texture2DArray.
        /// </summary>
        public PowerOfTwoResolution BaseResolution => m_Mapper?.BaseResolution ?? default;

        /// <summary>
        /// Gets the total capacity (number of slots) required for the GPU buffers.
        /// Use this as the 'count' parameter for ComputeBuffer allocation.
        /// </summary>
        public int BufferCapacity => m_Mapper?.Registry?.Capacity ?? 0;

        /// <summary>
        /// Gets the number of entries processed per dirty-tracking batch.
        /// Used to align GPU buffer updates and optimize the transfer of modified data.
        /// </summary>
        public int BatchSize => m_Mapper?.Registry?.BatchSize ?? 0;

        /// <summary>
        /// Gets the stride (byte size) for the culling data buffer.
        /// </summary>
        public int CullingStride => m_Mapper?.Registry?.CullingStride ?? 0;

        /// <summary>
        /// Gets the stride (byte size) for the render mapping buffer.
        /// </summary>
        public int RenderStride => m_Mapper?.Registry?.RenderStride ?? 0;

        /// <summary>
        /// Gets the highest index currently in use to optimize Compute Shader dispatch.
        /// Helps avoiding unnecessary thread groups on the GPU.
        /// </summary>
        public int HighestActiveIndex => m_Mapper?.Registry?.HighestIndex ?? -1;

        /// <summary>
        /// Provides read-only access to the LOD configuration and spatial queries.
        /// Returns null if not initialized.
        /// </summary>
        public ILODGridProvider<Vector3Int> LodGridProvider => m_ChunkRegistry;

        /// <summary>
        /// Gets or sets the viewer transform used for LOD calculations.
        /// Safe to update every frame. Call UpdateLODs() afterwards.
        /// </summary>
        public Transform Viewer
        {
            get
            {
                if (!IsInitialized)
                    throw new InvalidOperationException("TextureChunkCoordinator must be initialized before getting the viewer.");
                return m_ChunkRegistry.Viewer;
            }
            set
            {
                if (!IsInitialized)
                    throw new InvalidOperationException("TextureChunkCoordinator must be initialized before setting the viewer.");
                m_ChunkRegistry.Viewer = value;
            }
        }

        /// <summary>
        /// Gets or sets the coordinate system anchor to support large-scale world movement.
        /// </summary>
        public Vector3 Anchor
        {
            get
            {
                if (!IsInitialized)
                    throw new InvalidOperationException("TextureChunkCoordinator must be initialized before getting the anchor.");
                return m_ChunkRegistry.Anchor;
            }
            set
            {
                if (!IsInitialized)
                    throw new InvalidOperationException("TextureChunkCoordinator must be initialized before setting the anchor.");
                m_ChunkRegistry.Anchor = value;
            }
        }

        /// <summary>
        /// Checks if the coordinator has been initialized with a valid registry and mapper.
        /// </summary>
        public bool IsInitialized =>
            m_Mapper != null && m_Mapper.IsInitialized &&
            m_ChunkRegistry != null && m_ChunkRegistry.IsInitialized;

        #endregion

        #region Lifecycle

        /// <summary>
        /// Initializes the coordinator by extracting spatial and visual configurations from the provided LOD definitions.
        /// Acts as the central bridge to synchronize the LOD registry and the atlas mapping logic.
        /// </summary>
        /// <param name="gridSize">The size of the spatial grid.</param>
        /// <param name="anchor">The spatial anchor position.</param>
        /// <param name="lodDistances">The master list of LOD distances, defining the threshold ranges.</param>
        /// <param name="baseResolution">The reference resolution of a single slot at LOD 0.</param>
        /// <param name="batchSize">Number of elements to process in a single GPU update block.</param>
        /// <param name="viewer">The transform used to calculate distances for LOD switching (cannot be null).</param>
        /// <param name="deactivateOnCulled">If true, chunks outside the maximum LOD range will be disabled.</param>
        /// <param name="parent">The parent transform where chunk GameObjects will be organized (optional).</param>
        public void Initialize(
            GridSize gridSize,
            Vector3 anchor,
            ReadOnlySpan<float> lodDistances,
            PowerOfTwoResolution baseResolution,
            int batchSize,
            Transform viewer,
            bool deactivateOnCulled = true,
            Transform parent = null)
        {
            if (viewer == null)
            {
                throw new ArgumentNullException(nameof(viewer), "Viewer transform cannot be null.");
            }

            if (batchSize <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(batchSize), "Batch size must be greater than zero.");
            }

            if (lodDistances.IsEmpty)
            {
                throw new ArgumentException("LOD distances span cannot be empty.", nameof(lodDistances));
            }

            for (int i = 0; i < lodDistances.Length; i++)
            {
                if (lodDistances[i] <= 0f)
                {
                    throw new ArgumentOutOfRangeException(nameof(lodDistances), "LOD distances must be greater than zero.");
                }

                if (i > 0 && lodDistances[i] <= lodDistances[i - 1])
                {
                    throw new ArgumentException("LOD distances must be strictly increasing.", nameof(lodDistances));
                }
            }

            Reset();

            m_ChunkRegistry.Initialize(gridSize, anchor, lodDistances, viewer, deactivateOnCulled, parent);

            int lodCount = m_ChunkRegistry.LodCount;
            int[] capacities = new int[lodCount];
            for (int i = 0; i < lodCount; i++)
            {
                capacities[i] = m_ChunkRegistry.GetMaxCapacityForLODLevel(i);
            }

            m_Mapper.Initialize(capacities, baseResolution, batchSize);
        }

        /// <summary>
        /// Clears all runtime data (chunks and mapping) but keeps the coordinator initialized.
        /// Use this for a soft restart without destroying the container or settings.
        /// </summary>
        public void Clear()
        {
            m_ChunkRegistry?.Clear();
            m_Mapper?.Clear();
        }

        /// <summary>
        /// Resets the coordinator, requires re-initialization.
        /// </summary>
        public void Reset()
        {
            Clear();
            m_ChunkRegistry?.Reset();
            m_Mapper?.Reset();
        }

        #endregion

        #region High-Frequency Updates

        /// <summary>
        /// Shifts the coordinate system anchor to support large-scale world movement.
        /// Updates the registry and ensures the spatial mapping stays consistent.
        /// </summary>
        public void NotifyOriginShift(Vector3 delta)
        {
            if (!IsInitialized)
                throw new InvalidOperationException("TextureChunkCoordinator must be initialized before notifying origin shifts.");

            m_ChunkRegistry.NotifyOriginShift(delta);
        }

        /// <summary>
        /// Updates all chunks' LOD state based on current viewer position.
        /// </summary>
        /// <returns>The number of chunks that actually changed their LOD level.</returns>
        public int UpdateLODs()
        {
            if (!IsInitialized)
                throw new InvalidOperationException("TextureChunkCoordinator must be initialized before updating LODs.");

            return m_ChunkRegistry.UpdateLODs();
        }

        #endregion

        #region Rendering Pipeline

        /// <summary>
        /// Iterates through all registered chunks and forces them into the mapper's update queue.
        /// Essential after LOD configuration changes to refresh the "World View".
        /// </summary>
        public void ForceRequeueAll()
        {
            if (!IsInitialized)
                throw new InvalidOperationException("TextureChunkCoordinator is not initialized.");

            foreach (var chunk in m_ChunkRegistry.AllEntries)
            {
                if (chunk.IsVisible)
                {
                    RequestChunkTile(chunk);
                }
                else
                {
                    RemoveChunkTile(chunk);
                }
            }

            m_Mapper.FlushTileRequests();
        }

        /// <summary>
        /// Phase 1: Update Mapping State.
        /// Feeds registry changes into the mapper. Call this when the external SpatialCollection has changed entries.
        /// </summary>
        public void UpdateTopology<TCollection>(TCollection masterSource)
            where TCollection : ISpatialCollection<Vector3Int>
        {
            if (!IsInitialized)
                throw new InvalidOperationException("TextureChunkCoordinator is not initialized.");

            try
            {
                var handler = new TopologyUpdateHandler<TCollection>(this, masterSource);
                masterSource.ForEachDirtyCell(ref handler);

                m_Mapper.FlushTileRequests();
            }
            catch (Exception e)
            {
                throw new Exception($"Topology update failed: {e.Message}", e);
            }
        }

        /// <summary>
        /// Internal handler for zero-allocation dirty cell topology updates.
        /// </summary>
        private readonly struct TopologyUpdateHandler<TCollection> : IExecutionHandler<Vector3Int>
            where TCollection : ISpatialCollection<Vector3Int>
        {
            private readonly TextureChunkCoordinator _coordinator;
            private readonly TCollection _masterSource;

            public TopologyUpdateHandler(TextureChunkCoordinator coordinator, TCollection masterSource)
            {
                _coordinator = coordinator;
                _masterSource = masterSource;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void Execute(Vector3Int key)
            {
                bool hasData = _masterSource.IsCellActive(key);
                bool exists = _coordinator.m_ChunkRegistry.TryGetEntry(key, out TextureChunk chunk);

                if (hasData)
                {
                    var handler = new StatefulActionHandler<TextureChunk, TextureChunkCoordinator>(_coordinator, static (c, coord) =>
                    {
                        coord.SetupChunk(c);
                    });

                    _coordinator.m_ChunkRegistry.GetOrCreateChunk(key, ref handler, out chunk);
                }
                else if (exists)
                {
                    _coordinator.m_ChunkRegistry.RemoveAndDestroy(key);
                }
            }
        }

        /// <summary>
        /// Phase 2: Execution.
        /// Iterates over the result set. This allows multiple bake-passes (Height, Splat, etc.) using the passed in function pointer.
        /// </summary>
        public void ForEachBakeCommand<THandler>(ref THandler handler) 
            where THandler : struct, IExecutionHandler<TileMetadata<Vector3Int>>
        {
            if (!IsInitialized)
                throw new InvalidOperationException("TextureChunkCoordinator is not initialized.");

            try
            {
                var bridge = new BakeBridgeHandler<THandler>(this, ref handler);
                m_Mapper.ForEachPendingBake(ref bridge);
                handler = bridge._userHandler;
            }
            catch (Exception e)
            {
                throw new Exception($"Bake execution failed: {e.Message}", e);
            }
            finally
            {
                m_Mapper.ClearBakeQueue();
            }
        }

        /// <summary>
        /// Internal bridge to link high-level chunk tracking with the low-level mapper output.
        /// </summary>
        private struct BakeBridgeHandler<TUserHandler> : IExecutionHandler<TileMetadata<Vector3Int>>
            where TUserHandler : struct, IExecutionHandler<TileMetadata<Vector3Int>>
        {
            private TextureChunkCoordinator _coordinator;
            public TUserHandler _userHandler;

            public BakeBridgeHandler(TextureChunkCoordinator coordinator, ref TUserHandler userHandler)
            {
                _coordinator = coordinator;
                _userHandler = userHandler;
            }

            public void Execute(TileMetadata<Vector3Int> metadata)
            {
                if (_coordinator.m_ChunkRegistry.TryGetEntry(metadata.Key, out var chunk))
                {
                    chunk.SetTextureMapping(metadata.Mapping);
                    _userHandler.Execute(metadata);
                }
            }
        }

        #endregion

        #region Texture Chunk Setup

        /// <summary>
        /// Sets up a chunk by subscribing to its lifecycle and LOD state events.
        /// Attaches named handlers to ensure clear responsibility and prevent lambda allocations.
        /// </summary>
        /// <param name="chunk">The texture LOD chunk to initialize.</param>
        private void SetupChunk(TextureChunk chunk)
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
                RemoveChunkTile(chunk as TextureChunk);
            }

            if (newLod >= 0)
            {
                RequestChunkTile(chunk as TextureChunk);
            }
        }

        /// <summary>
        /// Handles the cleanup process when a chunk is being destroyed or pooled.
        /// Ensures that the grid key is removed from active processing and resources are freed.
        /// </summary>
        /// <param name="chunk">The chunk being cleaned up.</param>
        private void HandleChunkCleanup(TextureChunk chunk)
        {
            RemoveChunkTile(chunk);

            chunk.OnLODChanged -= HandleLodChanged;
            chunk.OnCleanup -= HandleChunkCleanup;
        }

        /// <summary>
        /// Helper to avoid repeating the SetTile calls.
        /// </summary>
        private void RequestChunkTile(TextureChunk chunk)
        {
            if (chunk != null && chunk.CurrentLOD >= 0)
            {
                m_Mapper.RequestTile(chunk.GridKey, chunk.CurrentLOD, chunk.WorldPosition, chunk.LocalExtent.x);
            }
        }

        /// <summary>
        /// Helper to avoid repeating the RemoveTile calls.
        /// </summary>
        private void RemoveChunkTile(TextureChunk chunk)
        {
            m_Mapper.ReleaseTile(chunk.GridKey);
        }

        #endregion
    }
}