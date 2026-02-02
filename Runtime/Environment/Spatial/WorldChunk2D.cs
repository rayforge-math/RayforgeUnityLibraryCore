using UnityEngine;

namespace Rayforge.Core.Environment.Spatial
{
    /// <summary>
    /// A specialized version of WorldChunk3D for 2D/Top-Down logic.
    /// Simplifies spatial settings to a 2D plane while maintaining 3D compatibility for culling.
    /// </summary>
    /// <typeparam name="T">The derived type for type-safe processing.</typeparam>
    public abstract class WorldChunk2D<T> : WorldChunk3D<T>
        where T : WorldChunk2D<T>
    {
        #region 2D Spatial Settings
        [Header("2D Spatial Settings")]
        /// <summary> The horizontal half-size of the chunk (XZ plane). </summary>
        [Tooltip("The half-size of the chunk on the X and Z axes.")]
        public Vector2 areaExtent = new Vector2(50, 50);

        /// <summary>
        /// The vertical range of the chunk. 
        /// Important for 3D Frustum Culling even in 2D systems.
        /// </summary>
        [Tooltip("The vertical height range (Y-axis). Used for 3D culling logic.")]
        public float heightExtent = 50f;
        #endregion

        #region Initialization & Validation
        /// <summary>
        /// Synchronizes the 2D extents to the underlying 3D spatial system.
        /// Called when values are changed in the Inspector.
        /// </summary>
        protected virtual void OnValidate()
        {
            SyncExtent();
        }

        /// <summary>
        /// Ensures localExtent is initialized correctly on startup.
        /// </summary>
        protected virtual void Awake()
        {
            SyncExtent();
        }

        /// <summary>
        /// Maps 2D area and height to the 3D localExtent used by the base class.
        /// This ensures that GetWorldBounds() from the base class works correctly.
        /// </summary>
        private void SyncExtent()
        {
            // Bridges the 2D area (XZ) and height (Y) to the 3D extent system.
            localExtent = new Vector3(areaExtent.x, heightExtent, areaExtent.y);
        }
        #endregion

        #region 2D Logic Helpers
        /// <summary>
        /// Returns the top-down area size as a Vector2.
        /// </summary>
        /// <returns>The full width and length (XZ) of the chunk.</returns>
        public Vector2 GetAreaSize()
        {
            return areaExtent * 2f;
        }

        /// <summary>
        /// Conveniently gets the 2D key using the inherited spatial logic.
        /// </summary>
        public Vector2Int GetCurrentKey2D(float gridSize, Vector2 anchor = default)
        {
            // Reuses the base 3D grid logic but focused on the 2D plane.
            return GetGridKey2D(gridSize, new Vector3(anchor.x, 0, anchor.y));
        }
        #endregion

        #region Debugging & Gizmos
        /// <summary>
        /// Visualizes the 2D area in the Scene View with a distinct color and a floor-plane.
        /// </summary>
        protected override void OnDrawGizmosSelected()
        {
            // Draw the 3D wireframe volume from the base class (WorldChunk3D).
            base.OnDrawGizmosSelected();

            // Add a semi-transparent "floor" to emphasize the 2D area on the XZ plane.
            Gizmos.color = new Color(0, 1, 1, 0.2f);
            Vector3 center = transform.position;

            // Draw a flat box as a floor visualizer.
            Gizmos.DrawCube(center, new Vector3(areaExtent.x * 2f, 0.01f, areaExtent.y * 2f));

            // Draw a stronger outline for the floor for better visual clarity.
            Gizmos.color = new Color(0, 1, 1, 0.5f);
            Gizmos.DrawWireCube(center, new Vector3(areaExtent.x * 2f, 0.01f, areaExtent.y * 2f));
        }
        #endregion
    }
}
