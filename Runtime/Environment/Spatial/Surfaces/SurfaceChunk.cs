using UnityEngine;

namespace Rayforge.Core.Environment.Spatial.Surfaces
{
    /// <summary>
    /// A specialized 2D chunk representing a portion of the world surface.
    /// Stores surface-specific data like heightmaps and reacts to LOD changes to trigger re-bakes.
    /// </summary>
    [ChunkConfig(SpatialAxes.Surface)]
    public class SurfaceChunk : LODChunk<SurfaceChunk>
    {
        /// <summary>
        /// The GPU texture holding height data for this chunk.
        /// This reference is managed by the SurfaceRegistry or a Baker system.
        /// </summary>
        public RenderTexture Heightmap { get; private set; }

        /// <summary>
        /// Cleans up GPU resources when the chunk is removed.
        /// Essential to prevent VRAM leaks when chunks are pooled or destroyed.
        /// </summary>
        protected override void OnDisposeInternal()
        {
            if (Heightmap != null)
            {
                Heightmap.Release();
                Heightmap = null;
            }
        }

        protected override void OnLODChangedInternal(int oldLod, int newLod)
        {
            
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

        #region Debugging
        protected override void OnDrawGizmosSelected()
        {
            Vector3 pos = transform.position;
            Vector3 size = new Vector3(localExtent.x * 2f, 0.1f, localExtent.z * 2f);

            Gizmos.color = GetLODColor(CurrentLOD);
            Gizmos.DrawCube(pos, size);
            Gizmos.DrawWireCube(pos, size);
        }

        private Color GetLODColor(int lod)
        {
            return lod switch
            {
                -1 => Color.red,
                0 => Color.green,
                1 => Color.yellow,
                _ => Color.orange
            };
        }
        #endregion
    }
}
