using UnityEngine;

namespace Rayforge.Core.Environment.Spatial
{
    /// <summary>
    /// A specialized version of Chunk3D for 2D/Top-Down logic.
    /// Maps 3D spatial data to the XZ plane for easier heightmap management.
    /// </summary>
    /// <typeparam name="T">The derived type for type-safe processing.</typeparam>
    [ChunkConfig(SpatialAxes.Surface)]
    public abstract class Chunk2D<T> : Chunk3D<T>
        where T : Chunk2D<T>
    {
        #region 2D Accessors

        /// <summary> Gets the horizontal half-size (XZ plane). </summary>
        public Vector2 areaExtent => new Vector2(localExtent.x, localExtent.z);

        /// <summary> Gets the vertical range (Y-axis). </summary>
        public float heightExtent => localExtent.y;

        /// <summary> 
        /// Returns the current grid key as a 2D vector (XZ).
        /// </summary>
        public Vector2Int GridKey2D => new Vector2Int(GridKey.x, GridKey.z);

        #endregion

        #region Distance Overrides (Optimized 2D Logic)

        /// <summary> 
        /// Overrides distance calculation to strictly ignore the Y-axis. 
        /// Provides a top-down distance ideal for LODs and Terrain-Surface logic.
        /// </summary>
        public override float GetSqrDistanceTo(Vector3 worldPos)
        {
            Vector3 center = transform.position;
            float dx = center.x - worldPos.x;
            float dz = center.z - worldPos.z;

            return dx * dx + dz * dz;
        }

        /// <summary> 
        /// Precise squared distance to AABB edge in the XZ plane.
        /// Optimized to avoid Mathf.Max(params) array allocations.
        /// </summary>
        public override float GetSqrDistanceToClosestEdge(Vector3 worldPos)
        {
            Vector3 center = transform.position;

            // Calculate X-distance to edge (0 if inside)
            float deltaX = Mathf.Abs(center.x - worldPos.x) - localExtent.x;
            float dx = deltaX > 0 ? deltaX : 0;

            // Calculate Z-distance to edge (0 if inside)
            float deltaZ = Mathf.Abs(center.z - worldPos.z) - localExtent.z;
            float dz = deltaZ > 0 ? deltaZ : 0;

            return dx * dx + dz * dz;
        }

        #endregion

        #region 2D Logic Helpers

        /// <summary> Returns the full top-down area size (XZ). </summary>
        public Vector2 GetAreaSize() => areaExtent * 2f;

        /// <summary>
        /// Checks if a world position is within the XZ boundaries, ignoring height.
        /// </summary>
        public bool Overlaps2D(Vector3 worldPos)
        {
            Vector3 center = transform.position;
            return Mathf.Abs(center.x - worldPos.x) <= localExtent.x &&
                   Mathf.Abs(center.z - worldPos.z) <= localExtent.z;
        }

        #endregion

        #region Debugging & Gizmos

        protected override void OnDrawGizmosSelected()
        {
            // Draw the wireframe from the base (will be green due to Surface flag)
            base.OnDrawGizmosSelected();

            Vector3 center = transform.position;
            Vector2 size = GetAreaSize();

            // Add a semi-transparent "floor" to emphasize the 2D nature
            Gizmos.color = new Color(0, 1, 1, 0.15f);
            Gizmos.DrawCube(center, new Vector3(size.x, 0.01f, size.y));

            // Draw a solid outline for the XZ area
            Gizmos.color = new Color(0, 1, 1, 0.4f);
            Gizmos.DrawWireCube(center, new Vector3(size.x, 0f, size.y));
        }

        #endregion
    }
}
