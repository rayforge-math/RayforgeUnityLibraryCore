using UnityEngine;

namespace Rayforge.Core.Environment.Spatial
{
    using UnityEngine;

    /// <summary>
    /// A specialized version of WorldChunk3D for 2D/Top-Down logic.
    /// Uses the base localExtent directly to avoid redundant sync logic.
    /// </summary>
    /// <typeparam name="T">The derived type for type-safe processing.</typeparam>
    public abstract class WorldChunk2D<T> : WorldChunk3D<T>
        where T : WorldChunk2D<T>
    {
        #region 2D Accessors
        // Instead of storing extra fields, we point directly to the base class data.
        // This makes 'areaExtent' and 'heightExtent' virtual views of the 3D data.

        /// <summary> The horizontal half-size (XZ plane) derived from localExtent. </summary>
        public Vector2 areaExtent
        {
            get => new Vector2(localExtent.x, localExtent.z);
            protected set => localExtent = new Vector3(value.x, localExtent.y, value.y);
        }

        /// <summary> The vertical range (Y-axis) derived from localExtent. </summary>
        public float heightExtent
        {
            get => localExtent.y;
            protected set => localExtent = new Vector3(localExtent.x, value, localExtent.z);
        }
        #endregion

        #region Configuration
        /// <summary>
        /// Set the dimensions of the chunk. 
        /// This is the clean way to initialize the chunk from a manager.
        /// </summary>
        public void Configure(Vector2 area, float height)
        {
            // Directly setting the protected localExtent of the base class.
            localExtent = new Vector3(area.x, height, area.y);
        }
        #endregion

        #region 2D Logic Helpers
        /// <summary> Returns the full top-down area size (XZ). </summary>
        public Vector2 GetAreaSize() => areaExtent * 2f;

        /// <summary>
        /// Gets the 2D key using the inherited spatial logic.
        /// Note: anchor must be passed to support Floating Origin.
        /// </summary>
        public Vector2Int GetCurrentKey2D(float gridSize, Vector3 anchor)
        {
            // Reuse the base 3D logic which already handles the anchor correctly.
            return GetGridKey2D(gridSize, anchor);
        }
        #endregion

        #region Debugging & Gizmos
        protected override void OnDrawGizmosSelected()
        {
            // Draw the 3D volume (WireCube)
            base.OnDrawGizmosSelected();

            // Visualize the floor plane using the live areaExtent properties
            Gizmos.color = new Color(0, 1, 1, 0.2f);
            Vector3 center = transform.position;
            Vector2 size = GetAreaSize();

            Gizmos.DrawCube(center, new Vector3(size.x, 0.01f, size.y));

            Gizmos.color = new Color(0, 1, 1, 0.5f);
            Gizmos.DrawWireCube(center, new Vector3(size.x, 0.01f, size.y));
        }
        #endregion
    }
}
