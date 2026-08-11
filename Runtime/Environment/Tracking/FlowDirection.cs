using Rayforge.Core.Maths.Vector;
using UnityEngine;

namespace Rayforge.Core.Environment.Tracking
{
    /// <summary>
    /// Represents a 2D flow direction with synchronized vector and degree representations.
    /// Allows seamless switching between normalized direction vectors and angles.
    /// </summary>
    public struct FlowDirection
    {
        private Vector2 m_Direction;
        private float m_Degree;

        /// <summary>
        /// Creates a flow direction from an angle in degrees.
        /// </summary>
        public FlowDirection(float degree)
        {
            m_Degree = Mathf.Repeat(degree, 360f);
            m_Direction = VectorMath.DegreeToVector(m_Degree);
        }

        /// <summary>
        /// Creates a flow direction from a normalized direction vector.
        /// </summary>
        public FlowDirection(Vector2 direction)
        {
            if (direction.sqrMagnitude < 0.0001f)
            {
                m_Degree = 0f;
                m_Direction = Vector2.right;
            }
            else
            {
                m_Direction = direction.normalized;
                m_Degree = VectorMath.VectorToDegree(m_Direction);
            }
        }

        /// <summary>
        /// The flow direction in degrees (0-360). 
        /// Automatically synchronizes the direction vector.
        /// </summary>
        public float Degree
        {
            get => m_Degree;
            set
            {
                float clamped = Mathf.Repeat(value, 360f);
                if (!Mathf.Approximately(m_Degree, clamped))
                {
                    m_Degree = clamped;
                    m_Direction = VectorMath.DegreeToVector(m_Degree);
                }
            }
        }

        /// <summary>
        /// The normalized 2D flow direction vector.
        /// Automatically synchronizes the degree representation.
        /// </summary>
        public Vector2 Direction
        {
            get
            {
                // Self-Healing
                if (m_Direction.sqrMagnitude < 0.0001f) return Vector2.right;
                return m_Direction;
            }
            set
            {
                if (value.sqrMagnitude < 0.0001f)
                {
                    throw new System.ArgumentException("FlowDirection cannot be set to a zero vector. It must have a valid direction.", nameof(value));
                }

                Vector2 normalized = value.normalized;
                if (m_Direction != normalized)
                {
                    m_Direction = normalized;
                    m_Degree = VectorMath.VectorToDegree(m_Direction);
                }
            }
        }
    }
}
