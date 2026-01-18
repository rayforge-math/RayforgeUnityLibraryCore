using UnityEngine;
using Unity.Mathematics;
using Rayforge.Core.Maths.Spaces;
using Unity.Burst;

namespace Rayforge.Core.Maths.Helpers
{ 
    public static class TransformHelpers
    {
        /// <summary>
        /// Converts an angle in degrees to a 2D unit vector on the Cartesian plane.
        /// </summary>
        /// <param name="deg">The angle in degrees.</param>
        /// <returns>
        /// A <see cref="float2"/> representing the direction vector (x = cos, y = sin) 
        /// with a magnitude of 1.
        /// </returns>
        /// <remarks>
        /// This method internally converts degrees to radians and calls <see cref="RadToUnitVector"/>.
        /// </remarks>
        [BurstCompile]
        public static float2 DegToUnitVector(float deg)
        {
            var rad = deg * Mathf.Deg2Rad;
            return RadToUnitVector(rad);
        }

        /// <summary>
        /// Converts an angle in radians to a 2D unit vector on the Cartesian plane.
        /// </summary>
        /// <param name="rad">The angle in radians.</param>
        /// <returns>
        /// A <see cref="float2"/> representing the direction vector (x = cos, y = sin) 
        /// with a magnitude of 1.
        /// </returns>
        /// <remarks>
        /// This method uses <see cref="Polar"/> coordinates internally and is 
        /// optimized for use within Burst-compiled jobs.
        /// </remarks>
        [BurstCompile]
        public static float2 RadToUnitVector(float rad)
        {
            var polar = new Polar(1.0f, rad);
            return polar.ToCartesian();
        }

        /// <summary>
        /// Converts a 2D vector direction into an angle in radians using complex number math.
        /// </summary>
        /// <param name="vector">The direction vector (x, y).</param>
        /// <returns>
        /// The angle (Phase) in radians, typically in the range (-π, π]. 
        /// (0 is right/East on the unit circle).
        /// </returns>
        public static float VectorToRad(float2 vector)
        {
            var complex = new Complex(vector.x, vector.y);
            return complex.Phase();
        }

        /// <summary>
        /// Converts a 2D vector direction into an angle in degrees.
        /// </summary>
        /// <param name="vector">The direction vector (x, y).</param>
        /// <returns>The angle in degrees, ranging from -180° to 180°.</returns>
        public static float VectorToDeg(float2 vector)
        {
            var rad = VectorToRad(vector);
            return rad * Mathf.Rad2Deg;
        }
    }
}