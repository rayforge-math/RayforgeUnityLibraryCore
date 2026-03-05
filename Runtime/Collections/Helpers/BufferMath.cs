using System;

namespace Rayforge.Core.Collections.Helpers
{
    public static class BufferMath
    {
        /// <summary>
        /// Calculates the batch index for a given element index.
        /// Basic mapping from element space to batch space.
        /// </summary>
        public static int GetBatchIndex(int elementIndex, int batchSize)
            => elementIndex / batchSize;

        /// <summary>
        /// Calculates the total number of batches needed for a given capacity.
        /// Uses ceiling division to ensure the last partial batch is included.
        /// </summary>
        public static int GetTotalBatches(int capacity, int batchSize)
            => (capacity + batchSize - 1) / batchSize;

        /// <summary>
        /// Converts a range of batches into a range of elements.
        /// Useful for determining the exact Array segment for GPU uploads.
        /// </summary>
        public static void GetElementRange(int startBatch, int endBatch, int batchSize, int totalCapacity, out int startElement, out int count)
        {
            startElement = startBatch * batchSize;
            int endElement = Math.Min((endBatch + 1) * batchSize, totalCapacity);
            count = Math.Max(0, endElement - startElement);
        }

        /// <summary>
        /// Validates that one batch size is a multiple of the other.
        /// Ensures hierarchical alignment between two different data streams.
        /// </summary>
        public static bool IsPowerOfAligned(int batchSizeA, int batchSizeB)
        {
            int max = Math.Max(batchSizeA, batchSizeB);
            int min = Math.Min(batchSizeA, batchSizeB);
            return min > 0 && max % min == 0;
        }

        /// <summary>
        /// Calculates an aligned batch size that is at least the requested size 
        /// and a multiple of the largest involved batch size.
        /// </summary>
        public static int GetAlignedBatchSize(int requestedSize, int batchSizeA, int batchSizeB)
        {
            int maxBatch = Math.Max(batchSizeA, batchSizeB);
            if (maxBatch <= 0) return requestedSize;

            return GetTotalBatches(requestedSize, maxBatch) * maxBatch;
        }
    }
}