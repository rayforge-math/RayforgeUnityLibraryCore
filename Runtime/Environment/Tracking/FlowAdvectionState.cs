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
        private readonly float m_WrapValue;

        /// <summary>
        /// The current accumulated flow offset in coordinate space.
        /// </summary>
        public Vector3 Offset => m_Offset;

        /// <summary>
        /// Initializes a new instance of the <see cref="FlowAdvectionState"/> struct.
        /// </summary>
        /// <param name="wrapValue">The coordinate wrap value to prevent precision loss (default 1024).</param>
        public FlowAdvectionState(float wrapValue = 1024.0f)
        {
            m_Offset = Vector3.zero;
            m_WrapValue = wrapValue;
        }

        /// <summary>
        /// Advances the flow offset using the configured wrap value.
        /// </summary>
        /// <param name="velocity">Velocity vector of horizontal flow.</param>
        /// <param name="flowZ">Absolute vertical/depth-wise flow rate.</param>
        /// <param name="deltaTime">Time step.</param>
        public void Update(Vector2 velocity, float flowZ, float deltaTime)
        {
            m_Offset.x += velocity.x * deltaTime;
            m_Offset.y += velocity.y * deltaTime;
            m_Offset.z += flowZ * deltaTime;

            float wrap = m_WrapValue > 0f ? m_WrapValue : 1024.0f;

            Wrap(ref m_Offset, wrap);
        }

        private static void Wrap(ref Vector3 v, float wrap)
        {
            v.x = Mathf.Repeat(v.x, wrap);
            v.y = Mathf.Repeat(v.y, wrap);
            v.z = Mathf.Repeat(v.z, wrap);
        }
    }
}