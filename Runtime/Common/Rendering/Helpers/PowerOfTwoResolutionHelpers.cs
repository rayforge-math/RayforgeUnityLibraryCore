using UnityEngine;

namespace Rayforge.Core.Common.Rendering.Helpers
{
    /// <summary>
    /// Provides utility extension methods for the PowerOfTwoResolution enum to simplify comparisons and scaling.
    /// </summary>
    public static class PowerOfTwoExtensions
    {
        /// <summary>
        /// Checks if the current resolution is strictly higher than the specified resolution.
        /// </summary>
        /// <param name="current">The source resolution.</param>
        /// <param name="other">The resolution to compare against.</param>
        /// <returns>True if the integer value of current is greater than other.</returns>
        public static bool IsHigherThan(this PowerOfTwoResolution current, PowerOfTwoResolution other)
        {
            return (int)current > (int)other;
        }

        /// <summary>
        /// Checks if the current resolution is strictly lower than the specified resolution.
        /// </summary>
        /// <param name="current">The source resolution.</param>
        /// <param name="other">The resolution to compare against.</param>
        /// <returns>True if the integer value of current is less than other.</returns>
        public static bool IsLowerThan(this PowerOfTwoResolution current, PowerOfTwoResolution other)
        {
            return (int)current < (int)other;
        }

        /// <summary>
        /// Returns the next smaller power of two resolution. 
        /// Clamps to <see cref="PowerOfTwoResolution.Resolution32"/> if already at the minimum.
        /// </summary>
        /// <param name="current">The source resolution.</param>
        /// <returns>The decremented resolution step.</returns>
        public static PowerOfTwoResolution Downscale(this PowerOfTwoResolution current)
        {
            return current switch
            {
                PowerOfTwoResolution.Resolution1024 => PowerOfTwoResolution.Resolution512,
                PowerOfTwoResolution.Resolution512 => PowerOfTwoResolution.Resolution256,
                PowerOfTwoResolution.Resolution256 => PowerOfTwoResolution.Resolution128,
                PowerOfTwoResolution.Resolution128 => PowerOfTwoResolution.Resolution64,
                _ => PowerOfTwoResolution.Resolution32 // Smallest possible
            };
        }

        /// <summary>
        /// Returns the next larger power of two resolution. 
        /// Clamps to <see cref="PowerOfTwoResolution.Resolution1024"/> if already at the maximum.
        /// </summary>
        /// <param name="current">The source resolution.</param>
        /// <returns>The incremented resolution step.</returns>
        public static PowerOfTwoResolution Upscale(this PowerOfTwoResolution current)
        {
            return current switch
            {
                PowerOfTwoResolution.Resolution32 => PowerOfTwoResolution.Resolution64,
                PowerOfTwoResolution.Resolution64 => PowerOfTwoResolution.Resolution128,
                PowerOfTwoResolution.Resolution128 => PowerOfTwoResolution.Resolution256,
                PowerOfTwoResolution.Resolution256 => PowerOfTwoResolution.Resolution512,
                _ => PowerOfTwoResolution.Resolution1024 // Largest possible
            };
        }

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
    }
}
