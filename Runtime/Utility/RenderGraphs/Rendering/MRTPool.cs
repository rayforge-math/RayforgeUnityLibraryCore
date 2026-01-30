using UnityEngine.Rendering;

namespace Rayforge.Core.Utility.RenderGraphs.Rendering
{
    /// <summary>
    /// Provides a zero-allocation pool of arrays for Multiple Render Target (MRT) setups.
    /// Since Unity's CommandBuffer.SetRenderTarget(RenderTargetIdentifier[], ...) 
    /// uses the array's length to determine target count, we provide arrays of exact sizes.
    /// </summary>
    public static class MRTPool
    {
        private static readonly RenderTargetIdentifier[][] s_Pool = new RenderTargetIdentifier[9][]
        {
            new RenderTargetIdentifier[0],
            new RenderTargetIdentifier[1],
            new RenderTargetIdentifier[2],
            new RenderTargetIdentifier[3],
            new RenderTargetIdentifier[4],
            new RenderTargetIdentifier[5],
            new RenderTargetIdentifier[6],
            new RenderTargetIdentifier[7],
            new RenderTargetIdentifier[8] 
        };

        /// <summary>
        /// Gets a pre-allocated array of exactly the specified length.
        /// </summary>
        /// <param name="count">The required number of render targets (0-8).</param>
        /// <returns>A shared array instance of the requested length.</returns>
        /// <exception cref="System.ArgumentOutOfRangeException">Thrown if count is greater than 8.</exception>
        public static RenderTargetIdentifier[] Get(int count)
        {
            if (count < 0 || count > 8)
                throw new System.ArgumentOutOfRangeException(nameof(count), "MRT count must be between 0 and 8.");

            return s_Pool[count];
        }
    }
}