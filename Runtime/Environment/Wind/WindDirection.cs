using Rayforge.Core.Maths.Helpers;
using UnityEngine;

namespace Rayforge.Core.Environment.Wind
{
    /// <summary>
    /// Represents a 2D wind direction with both vector and degree representations.
    /// This allows easy access to either the normalized vector (for calculations) 
    /// or the angle in degrees (for user-friendly adjustments).
    /// </summary>
    public struct WindDirection
    {
        // Internal normalized 2D vector representing the wind direction (XY-plane)
        private Vector2 windDirectionVector;

        // Internal angle in degrees (0-360) representing the same wind direction
        private float windDirectionDegree;

        /// <summary>
        /// The wind direction in degrees (0-360). 
        /// Setting this updates the internal vector representation automatically.
        /// </summary>
        public float degree
        {
            get => windDirectionDegree;
            set
            {
                value = Mathf.Repeat(value, 360f);

                if (!Mathf.Approximately(windDirectionDegree, value))
                {
                    windDirectionDegree = value;
                    windDirectionVector = TransformHelpers.DegToUnitVector(value);
                }
            }
        }

        /// <summary>
        /// The normalized 2D wind direction vector.
        /// Setting this updates the degree representation automatically.
        /// </summary>
        public Vector2 direction
        {
            get => windDirectionVector;
            set
            {
                value.Normalize();

                if (windDirectionVector != value)
                {
                    windDirectionVector = value;
                    windDirectionDegree = TransformHelpers.VectorToDeg(value);
                }
            }
        }
    }
}
