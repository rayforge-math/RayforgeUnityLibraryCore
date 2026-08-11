using System;

namespace Rayforge.Core.Collections.Helpers
{
    public static class BufferMath
    {
        /// <summary>
        /// Calculates the batch index for a given element index.
        /// Returns 0 if batchSize is invalid to prevent crashes.
        /// </summary>
        public static int GetBatchIndex(int elementIndex, int batchSize)
        {
            if (batchSize <= 0) return 0;
            return Math.Max(0, elementIndex) / batchSize;
        }

        /// <summary>
        /// Calculates the total number of batches needed for a given capacity.
        /// Uses long math internally to prevent overflow during calculation.
        /// </summary>
        public static int GetTotalBatches(int capacity, int batchSize)
        {
            if (batchSize <= 0 || capacity <= 0) return 0;
            return (int)(((long)capacity + batchSize - 1) / batchSize);
        }

        /// <summary>
        /// Converts a range of batches into a range of elements.
        /// Clamps all values to valid array bounds.
        /// </summary>
        public static void GetElementRange(int startBatch, int endBatch, int batchSize, int totalCapacity, out int startElement, out int count)
        {
            if (batchSize <= 0 || totalCapacity <= 0 || startBatch > endBatch)
            {
                startElement = 0;
                count = 0;
                return;
            }

            // Clamp inputs to prevent negative access
            int safeStartBatch = Math.Max(0, startBatch);

            // Use long for intermediate calculation to prevent overflow
            long rawStart = (long)safeStartBatch * batchSize;
            startElement = (int)Math.Min(rawStart, (long)totalCapacity);

            long rawEnd = ((long)endBatch + 1) * batchSize;
            int endElement = (int)Math.Min(rawEnd, (long)totalCapacity);

            count = Math.Max(0, endElement - startElement);
        }

        /// <summary>
        /// Validates that one batch size is a multiple of the other.
        /// </summary>
        public static bool IsPowerOfAligned(int batchSizeA, int batchSizeB)
        {
            if (batchSizeA <= 0 || batchSizeB <= 0) return false;

            int max = Math.Max(batchSizeA, batchSizeB);
            int min = Math.Min(batchSizeA, batchSizeB);
            return max % min == 0;
        }

        /// <summary>
        /// Calculates an aligned batch size. Returns requestedSize if alignment is impossible.
        /// </summary>
        public static int GetAlignedBatchSize(int requestedSize, int batchSizeA, int batchSizeB)
        {
            int maxBatch = Math.Max(batchSizeA, batchSizeB);

            if (maxBatch <= 0 || requestedSize <= 0)
                return Math.Max(0, requestedSize);

            long batches = GetTotalBatches(requestedSize, maxBatch);

            // Calculate the aligned size in 64-bit to detect overflow
            long alignedSize = batches * maxBatch;

            // If the aligned size exceeds int.MaxValue, we clamp it.
            if (alignedSize > int.MaxValue)
                return int.MaxValue;

            return (int)alignedSize;
        }
    }
}