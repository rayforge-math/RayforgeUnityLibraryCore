using UnityEngine;

namespace Rayforge.Core.Common.Rendering.Helpers
{
    /// <summary>
    /// Provides utility extension methods for the PowerOfTwoResolution enum to simplify comparisons and scaling.
    /// </summary>
    public static class PowerOfTwoExtensions
    {
        #region Comparison

        /// <summary>
        /// Checks if the current resolution is equal to the specified resolution.
        /// </summary>
        /// <param name="current">The source resolution.</param>
        /// <param name="other">The resolution to compare against.</param>
        /// <returns>True if the integer values are equal; otherwise, false.</returns>
        public static bool IsEqual(this PowerOfTwoResolution current, PowerOfTwoResolution other)
        {
            return (int)current == (int)other;
        }

        /// <summary>
        /// Checks if the current resolution is strictly higher than the specified resolution.
        /// </summary>
        /// <param name="current">The source resolution.</param>
        /// <param name="other">The resolution to compare against.</param>
        /// <returns>True if the integer value of current is greater than other.</returns>
        public static bool IsHigher(this PowerOfTwoResolution current, PowerOfTwoResolution other)
        {
            return (int)current > (int)other;
        }

        /// <summary>
        /// Checks if the current resolution is higher than or equal to the specified resolution.
        /// </summary>
        /// <param name="current">The source resolution.</param>
        /// <param name="other">The resolution to compare against.</param>
        /// <returns>True if the integer value of current is greater than or equal to other.</returns>
        public static bool IsHigherOrEqual(this PowerOfTwoResolution current, PowerOfTwoResolution other)
        {
            return (int)current >= (int)other;
        }

        /// <summary>
        /// Checks if the current resolution is strictly lower than the specified resolution.
        /// </summary>
        /// <param name="current">The source resolution.</param>
        /// <param name="other">The resolution to compare against.</param>
        /// <returns>True if the integer value of current is less than other.</returns>
        public static bool IsLower(this PowerOfTwoResolution current, PowerOfTwoResolution other)
        {
            return (int)current < (int)other;
        }

        /// <summary>
        /// Checks if the current resolution is lower than or equal to the specified resolution.
        /// </summary>
        /// <param name="current">The source resolution.</param>
        /// <param name="other">The resolution to compare against.</param>
        /// <returns>True if the integer value of current is less than or equal to other.</returns>
        public static bool IsLowerOrEqual(this PowerOfTwoResolution current, PowerOfTwoResolution other)
        {
            return (int)current <= (int)other;
        }

        #endregion

        #region Scaling

        /// <summary>
        /// Returns the next smaller power of two resolution. 
        /// Clamps to <see cref="PowerOfTwoResolution.Resolution32"/> if already at the minimum.
        /// </summary>
        /// <param name="current">The source resolution.</param>
        /// <returns>The decremented resolution step.</returns>
        public static PowerOfTwoResolution Downscale(this PowerOfTwoResolution current)
        {
            if (current <= PowerOfTwoResolution.Res1)
                return PowerOfTwoResolution.Res1;

            return (PowerOfTwoResolution)((int)current >> 1);
        }

        /// <summary>
        /// Returns the next larger power of two resolution. 
        /// Clamps to <see cref="PowerOfTwoResolution.Resolution1024"/> if already at the maximum.
        /// </summary>
        /// <param name="current">The source resolution.</param>
        /// <returns>The incremented resolution step.</returns>
        public static PowerOfTwoResolution Upscale(this PowerOfTwoResolution current)
        {
            if (current >= PowerOfTwoResolution.Res8192)
                return PowerOfTwoResolution.Res8192;

            return (PowerOfTwoResolution)((int)current << 1);
        }

        #endregion

        #region Maths

        /// <summary>
        /// Calculates the Log2 of the resolution value. 
        /// Useful for calculating Mip-Map levels or Compute Shader thread group counts.
        /// </summary>
        /// <example>Resolution256 returns 8.</example>
        /// <param name="current">The source resolution.</param>
        /// <returns>The power of two exponent (Log2).</returns>
        public static int GetPowerOfTwoExponent(this PowerOfTwoResolution current)
        {
            return Mathf.RoundToInt(Mathf.Log((int)current, 2));
        }

        #endregion
    }
}
