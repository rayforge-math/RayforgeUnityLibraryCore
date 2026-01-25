using UnityEngine;

namespace Rayforge.Core.Environment.Wind
{
    /// <summary>
    /// Represents a time-integrated wind advection state.
    /// Can be used to drive noise UVs, 3D noise sampling or any
    /// procedural animation requiring coherent wind motion.
    /// </summary>
    public struct WindAdvectionState
    {
        private Vector3 m_Offset;

        /// <summary>
        /// Accumulated wind offset in UV / noise space.
        /// XY: Horizontal advection
        /// Z : Vertical / depth-wise flow (e.g. buoyancy)
        /// </summary>
        public Vector3 Offset => m_Offset;

        /// <summary>
        /// Advances the wind offset using the given parameters.
        /// XY is scaled by the speed multiplier; Z is treated as absolute vertical flow.
        /// </summary>
        public void Update(
            Vector2 directionXY,
            float flowZ,
            float speed,
            float deltaTime)
        {
            m_Offset.x += directionXY.x * speed * deltaTime;
            m_Offset.y += directionXY.y * speed * deltaTime;
            m_Offset.z += flowZ * deltaTime;

            Wrap(ref m_Offset, 1024.0f);
        }

        /// <summary>
        /// Wraps the offset to prevent floating-point precision issues and
        /// avoid visible tiling artifacts in procedural noise sampling.
        /// </summary>
        private static void Wrap(ref Vector3 v, float wrap)
        {
            v.x = Mathf.Repeat(v.x, wrap);
            v.y = Mathf.Repeat(v.y, wrap);
            v.z = Mathf.Repeat(v.z, wrap);
        }
    }
}