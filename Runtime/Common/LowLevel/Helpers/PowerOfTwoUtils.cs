using UnityEngine;

namespace Rayforge.Core
{
    public static class PowerOfTwoUtils
    {
        #region Scaling

        /// <summary>
        /// Returns the next smaller power of two. 
        /// Clamps to 1 if already at minimum.
        /// </summary>
        public static int Downscale(this int current)
        {
            if (!current.IsPowerOfTwo())
                throw new System.ArgumentException($"Value {current} is not a power of two.");

            if (current <= 1) return 1;
            return current >> 1;
        }

        /// <summary>
        /// Returns the next larger power of two.
        /// </summary>
        public static int Upscale(this int current)
        {
            if (!current.IsPowerOfTwo())
                throw new System.ArgumentException($"Value {current} is not a power of two.");

            if (current >= 0x40000000) return 0x40000000;
            return current << 1;
        }

        #endregion

        #region Maths

        /// <summary>
        /// Validates if a number is a power of two.
        /// </summary>
        public static bool IsPowerOfTwo(this int value)
        {
            return value > 0 && (value & (value - 1)) == 0;
        }

        /// <summary>
        /// Calculates the Log2 of the resolution value.
        /// </summary>
        public static int GetPowerOfTwoExponent(this int current)
        {
            if (!current.IsPowerOfTwo())
                throw new System.ArgumentException($"Value {current} is not a power of two.");

            return Mathf.RoundToInt(Mathf.Log(current, 2));
        }

        #endregion
    }
}
