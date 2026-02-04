using UnityEngine;
using Rayforge.Core.Environment.Abstractions;

namespace Rayforge.Core.Environment.Spatial
{
    /// <summary>
    /// A high-performance 3D implementation of the spatial chunk system.
    /// Pre-configured for volumetric objects where X, Y, and Z axes are all active.
    /// </summary>
    /// <typeparam name="T">The derived type for type-safe processing.</typeparam>
    public abstract class Chunk3D<T> : Chunk<T>
        where T : Chunk3D<T>
    {
        #region Identity Configuration

        static Chunk3D()
        {
            ActiveAxes = SpatialAxes.Voxel;
        }

        #endregion

        #region 3D Convenience Methods

        /// <summary>
        /// Shortcut to get the world-space position (center) of the chunk.
        /// </summary>
        public Vector3 WorldPosition => transform.position;

        /// <summary>
        /// Calculates the total volume of the chunk in world units.
        /// </summary>
        public float GetVolume() => (localExtent.x * 2f) * (localExtent.y * 2f) * (localExtent.z * 2f);

        #endregion

        #region Gizmos Implementation

        /// <summary>
        /// Ensures the 3D chunk is always drawn with the Voxel-specific cyan color.
        /// </summary>
        protected override void OnDrawGizmosSelected()
        {
            // Simply uses the base logic which already handles the color based on ActiveAxes
            base.OnDrawGizmosSelected();
        }

        #endregion
    }
}
