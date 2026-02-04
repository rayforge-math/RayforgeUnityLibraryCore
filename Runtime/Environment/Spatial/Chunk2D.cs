using UnityEngine;

namespace Rayforge.Core.Environment.Spatial
{
    /// <summary>
    /// A specialized version of Chunk3D for 2D/Top-Down logic.
    /// Maps 3D spatial data to the XZ plane for easier heightmap management.
    /// </summary>
    /// <typeparam name="T">The derived type for type-safe processing.</typeparam>
    public abstract class Chunk2D<T> : Chunk3D<T> where T : Chunk2D<T>
    {
        #region 2D Accessors
        /// <summary>
        /// Gets or sets the horizontal half-size (XZ plane).
        /// </summary>
        public Vector2 areaExtent
        {
            get => new Vector2(localExtent.x, localExtent.z);
            protected set => localExtent = new Vector3(value.x, localExtent.y, value.y);
        }

        /// <summary>
        /// Gets or sets the vertical range (Y-axis).
        /// </summary>
        public float heightExtent
        {
            get => localExtent.y;
            protected set => localExtent = new Vector3(localExtent.x, value, localExtent.z);
        }

        /// <summary> 
        /// Returns the current grid key as a 2D vector (XZ).
        /// </summary>
        public Vector2Int currentGridKey2D => new Vector2Int(currentGridKey.x, currentGridKey.z);
        #endregion

        #region Distance Overrides (2D Logic)

        /// <summary> 
        /// Overrides distance calculation to ignore the Y-axis. 
        /// English: Provides a top-down distance ideal for LODs and Terrain-Surface logic.
        /// </summary>
        public override float GetSqrDistanceTo(Vector3 worldPos)
        {
            Vector3 center = transform.position;
            float dx = center.x - worldPos.x;
            float dz = center.z - worldPos.z;

            // English: Ignore Y to keep LODs stable regardless of height.
            return dx * dx + dz * dz;
        }

        /// <summary> 
        /// Precise squared distance to AABB edge in the XZ plane.
        /// English: Effectively checks distance to the bounding square instead of the cube.
        /// </summary>
        public override float GetSqrDistanceToClosestEdge(Vector3 worldPos)
        {
            Vector3 center = transform.position;

            // English: Only calculate horizontal offsets.
            float dx = Mathf.Max(0, (center.x - localExtent.x) - worldPos.x, worldPos.x - (center.x + localExtent.x));
            float dz = Mathf.Max(0, (center.z - localExtent.z) - worldPos.z, worldPos.z - (center.z + localExtent.z));

            return dx * dx + dz * dz;
        }

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

        /// <summary> Returns the full top-down area size (XZ). </summary>
        public Vector2 GetAreaSize() => areaExtent * 2f;

        #endregion

        #region Debugging & Gizmos

        protected override void OnDrawGizmosSelected()
        {
            // Draw the 3D wireframe volume from Chunk3D
            base.OnDrawGizmosSelected();

            // Add a semi-transparent "floor"
            Gizmos.color = new Color(0, 1, 1, 0.15f);
            Vector3 center = transform.position;
            Vector2 size = GetAreaSize();

            // Drawing a flat cube as a floor indicator
            Gizmos.DrawCube(center, new Vector3(size.x, 0.01f, size.y));

            // English: Draw a dashed-line style indicator for the ground level if needed
            Gizmos.color = new Color(0, 1, 1, 0.4f);
            Gizmos.DrawWireCube(center, new Vector3(size.x, 0f, size.y));
        }

        #endregion
    }
}
