using UnityEngine;

namespace Rayforge.Core.Rendering.Helpers
{
    public static class MappingUtils
    {
        /// <summary>
        /// Calculates how many tiles of the specified <paramref name="tile"/> resolution fit into the 
        /// <paramref name="baseRes"/> per axis.
        /// </summary>
        /// <param name="tile">The resolution of an individual tile.</param>
        /// <param name="baseRes">The total resolution of the base container.</param>
        /// <returns>The number of tiles that fit along a single axis.</returns>
        /// <exception cref="System.ArgumentException">Thrown when <paramref name="tile"/> is larger than <paramref name="baseRes"/>.</exception>
        /// <example>
        /// <code>
        /// int slotsPerAxis = 512.ToSlotCountPerDim(2048); // returns 4
        /// </code>
        /// </example>
        public static int ToSlotCountPerDim(this int tile, int baseRes)
        {
            if (tile > baseRes)
            {
                throw new System.ArgumentException($"Tile ({tile}) cannot be larger than base ({baseRes}).");
            }

            return baseRes / tile;
        }

        /// <summary>
        /// Calculates the total capacity (total number of tiles) the <paramref name="baseRes"/> can hold 
        /// for the given <paramref name="tile"/> resolution (area calculation).
        /// </summary>
        /// <param name="tile">The resolution of an individual tile.</param>
        /// <param name="baseRes">The total resolution of the base container.</param>
        /// <returns>The total number of tiles that fit into the area of the base resolution.</returns>
        /// <example>
        /// <code>
        /// int totalSlots = 512.ToSlotCount(2048); // returns 16 (4x4)
        /// </code>
        /// </example>
        public static int ToSlotCount(this int tile, int baseRes)
        {
            int countPerDim = tile.ToSlotCountPerDim(baseRes);
            return countPerDim * countPerDim;
        }
    }
}
