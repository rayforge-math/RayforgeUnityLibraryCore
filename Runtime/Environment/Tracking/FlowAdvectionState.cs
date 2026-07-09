using UnityEngine;

namespace Rayforge.Core.Environment.Tracking
{
    /// <summary>
    /// Represents a time-integrated state for procedural flow advection.
    /// Used for driving noise UVs, 3D noise sampling, or coherent flow animations.
    /// </summary>
    public struct FlowAdvectionState
    {
        private Vector3 m_Offset;

        /// <summary>
        /// The current accumulated flow offset in coordinate space.
        /// </summary>
        public Vector3 Offset => m_Offset;

        /// <summary>
        /// Advances the flow offset.
        /// </summary>
        /// <param name="velocity">Velocity vector of horizontal flow.</param>
        /// <param name="flowZ">Absolute vertical/depth-wise flow rate.</param>
        /// <param name="deltaTime">Time step.</param>
        /// <param name="wrapValue">The coordinate wrap value to prevent precision loss (default 1024).</param>
        public void Update(
            Vector2 velocity,
            float flowZ,
            float deltaTime,
            float wrapValue = 1024.0f)
        {
            m_Offset.x += velocity.x * deltaTime;
            m_Offset.y += velocity.y * deltaTime;
            m_Offset.z += flowZ * deltaTime;

            Wrap(ref m_Offset, wrapValue);
        }

        private static void Wrap(ref Vector3 v, float wrap)
        {
            v.x = Mathf.Repeat(v.x, wrap);
            v.y = Mathf.Repeat(v.y, wrap);
            v.z = Mathf.Repeat(v.z, wrap);
        }
    }
}