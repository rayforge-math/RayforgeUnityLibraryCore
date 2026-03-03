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
    }
}