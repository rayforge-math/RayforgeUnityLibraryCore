using System.Threading;
using UnityEngine;

namespace Rayforge.Core.Common.Cache
{
    /// <summary>
    /// Provides a zero-allocation pool of static arrays for transient operations.
    /// This is specifically designed for Unity APIs that require an array of an exact length 
    /// to determine parameter counts (e.g., CommandBuffer.SetRenderTarget or Shader.SetGlobalVectorArray).
    /// </summary>
    /// <typeparam name="T">The type of the array elements.</typeparam>
    public static class StaticArrayPool<T>
    {
        private const int k_MaxPoolSize = 1024;

        /// <summary>
        /// The maximum size of cached arrays, everything exceeding this value will return a temporary, uncached array.
        /// </summary>
        public static int MaxPoolSize => k_MaxPoolSize;

        /// <summary>
        /// Internal storage for the pooled arrays. Each index represents an array of that specific length.
        /// </summary>
        private static readonly T[][] s_Pool = new T[k_MaxPoolSize][];

        /// <summary>
        /// Retrieves a shared array instance of the exact requested length.
        /// </summary>
        /// <remarks>
        /// <para><b>CRITICAL:</b> Do not store a reference to the returned array. It is shared across the entire application.</para>
        /// <para>Use this only for immediate, one-time calls and discard it immediately after use.</para>
        /// </remarks>
        /// <param name="count">The required number of elements in the array.</param>
        /// <returns>
        /// A shared array of exactly <paramref name="count"/> elements if within <see cref="MaxPoolSize"/>; 
        /// otherwise, a new array allocation.
        /// </returns>
        public static T[] Get(int count)
        {
            if (!ThreadingMeta.IsMainThread)
            {
                throw new System.InvalidOperationException(
                    "StaticArrayPool must only be accessed from the Unity Main Thread.");
            }

            if (count < 1)
            {
                throw new System.ArgumentOutOfRangeException(nameof(count),
                    "Requested array size must be at least 1.");
            }

            if (count <= k_MaxPoolSize)
            {
                return s_Pool[count - 1] ??= new T[count];
            }

            // Fallback: Allocate a new array for sizes exceeding the pool limit.
            return new T[count];
        }
    }
}