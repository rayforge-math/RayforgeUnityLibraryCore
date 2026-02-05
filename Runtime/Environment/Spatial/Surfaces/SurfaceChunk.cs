using Rayforge.Core.Environment.Abstractions;
using UnityEngine;

namespace Rayforge.Core.Environment.Spatial.Surfaces
{
    /// <summary>
    /// A specialized 2D chunk representing a portion of the world surface.
    /// Stores surface-specific data like heightmaps and reacts to LOD changes to trigger re-bakes.
    /// </summary>
    public class SurfaceChunk : Chunk2D<SurfaceChunk>, ILODSpatialEntry
    {
        /// <summary>
        /// The current Level of Detail index. Initialized to -1 to ensure the first update triggers.
        /// Used by the Registry to determine if an update is necessary.
        /// </summary>
        public int CurrentLOD { get; private set; } = -1;

        /// <summary>
        /// The GPU texture holding height data for this chunk.
        /// This reference is managed by the SurfaceRegistry or a Baker system.
        /// </summary>
        public RenderTexture Heightmap { get; private set; }

        /// <summary>
        /// Updates the LOD state and marks the chunk as dirty for the baking system.
        /// Triggered by LODChunkRegistry. Changing the LOD usually requires a resolution change.
        /// </summary>
        /// <param name="newLod">The new LOD level calculated by the registry.</param>
        public void UpdateLOD(int newLod)
        {
            if (CurrentLOD == newLod) return;

            CurrentLOD = newLod;
            MarkDirty();
        }

        /// <summary>
        /// Cleans up GPU resources when the chunk is removed.
        /// Essential to prevent VRAM leaks when chunks are pooled or destroyed.
        /// </summary>
        protected override void OnDispose()
        {
            if (Heightmap != null)
            {
                Heightmap.Release();
                Heightmap = null;
            }
        }

        /// <summary>
        /// Assigns a new heightmap to this chunk. 
        /// Usually called by the Baker after a successful generation.
        /// </summary>
        public void SetHeightmap(RenderTexture map)
        {
            if (Heightmap != null && Heightmap != map)
                Heightmap.Release();

            Heightmap = map;
        }
    }
}
