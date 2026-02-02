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
        private const int MaxPoolSize = 32;

        /// <summary>
        /// Internal storage for the pooled arrays. Each index represents an array of that specific length.
        /// </summary>
        private static readonly T[][] s_Pool = new T[MaxPoolSize][];

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
            if (count <= 0) return s_Pool[0] ??= new T[0];

            if (count < MaxPoolSize)
            {
                return s_Pool[count] ??= new T[count];
            }

            // Fallback: Allocate a new array for sizes exceeding the pool limit.
            return new T[count];
        }
    }
}