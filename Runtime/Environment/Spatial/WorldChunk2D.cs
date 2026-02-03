using UnityEngine;

namespace Rayforge.Core.Environment.Spatial
{
    using UnityEngine;

    /// <summary>
    /// A specialized version of WorldChunk3D for 2D/Top-Down logic.
    /// Uses the base localExtent directly and provides convenient access to 2D grid data.
    /// </summary>
    /// <typeparam name="T">The derived type for type-safe processing.</typeparam>
    public abstract class WorldChunk2D<T> : WorldChunk3D<T>
        where T : WorldChunk2D<T>
    {
        #region 2D Accessors
        /// <summary>
        /// Gets or sets the horizontal half-size (XZ plane) derived from localExtent.
        /// </summary>
        public Vector2 areaExtent
        {
            get => new Vector2(localExtent.x, localExtent.z);
            protected set => localExtent = new Vector3(value.x, localExtent.y, value.y);
        }

        /// <summary>
        /// Gets or sets the vertical range (Y-axis) derived from localExtent.
        /// </summary>
        public float heightExtent
        {
            get => localExtent.y;
            protected set => localExtent = new Vector3(localExtent.x, value, localExtent.z);
        }

        /// <summary> 
        /// Returns the current grid key from the base class as a 2D vector (XZ).
        /// Maps the 3D key directly to the 2D plane.
        /// </summary>
        public Vector2Int currentGridKey2D => new Vector2Int(currentGridKey.x, currentGridKey.z);
        #endregion

        #region Configuration
        /// <summary>
        /// Configures the dimensions of the chunk. 
        /// </summary>
        /// <param name="area">The half-extents on X and Z.</param>
        /// <param name="height">The half-extent on Y.</param>
        public void Configure(Vector2 area, float height)
        {
            localExtent = new Vector3(area.x, height, area.y);
        }
        #endregion

        #region 2D Logic Helpers
        /// <summary>
        /// Returns the full top-down area size (XZ).
        /// </summary>
        /// <returns>The full width and length of the chunk.</returns>
        public Vector2 GetAreaSize() => areaExtent * 2f;
        #endregion

        #region Debugging & Gizmos
        /// <summary>
        /// Visualizes the 2D area in the Scene View with a floor-plane.
        /// </summary>
        protected override void OnDrawGizmosSelected()
        {
            // Draw the 3D wireframe volume from the base class.
            base.OnDrawGizmosSelected();

            // Add a semi-transparent "floor" to emphasize the 2D area on the XZ plane.
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
