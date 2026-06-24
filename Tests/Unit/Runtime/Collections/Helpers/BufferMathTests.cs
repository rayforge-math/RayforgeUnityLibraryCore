using NUnit.Framework;

namespace Rayforge.Core.Collections.Helpers.Tests
{
    [TestFixture]
    public class BufferMathTests
    {
        // ================================================================
        // Standard Cases
        // ================================================================

        [Test]
        public void GetTotalBatches_Standard_CalculatesCorrectly()
        {
            // 100 elements, batch size 32 -> 4 batches (32, 32, 32, 4)
            Assert.AreEqual(4, BufferMath.GetTotalBatches(100, 32));

            // Perfectly divisible: 64 / 32 = 2
            Assert.AreEqual(2, BufferMath.GetTotalBatches(64, 32));

            // 127 elements, batch size 32 -> 4 batches (Last batch has exactly 31)
            // 3 * 32 = 96. 127 - 96 = 31.
            Assert.AreEqual(4, BufferMath.GetTotalBatches(127, 32));

            // Just one element over a boundary: 33 elements, batch size 32 -> 2 batches
            Assert.AreEqual(2, BufferMath.GetTotalBatches(33, 32));

            // Just one element under a boundary: 31 elements, batch size 32 -> 1 batch
            Assert.AreEqual(1, BufferMath.GetTotalBatches(31, 32));
        }

        [Test]
        public void GetBatchIndex_Standard_ReturnsCorrectIndex()
        {
            const int batchSize = 32;

            // First Batch: [0...31]
            Assert.AreEqual(0, BufferMath.GetBatchIndex(0, batchSize));
            Assert.AreEqual(0, BufferMath.GetBatchIndex(31, batchSize));

            // Second Batch: [32...63]
            Assert.AreEqual(1, BufferMath.GetBatchIndex(32, batchSize));
            Assert.AreEqual(1, BufferMath.GetBatchIndex(63, batchSize));

            // Third Batch: [64...95]
            Assert.AreEqual(2, BufferMath.GetBatchIndex(64, batchSize));
        }

        [Test]
        public void GetBatchIndex_LargeIndices_HandlesHighRanges()
        {
            const int batchSize = 1024;

            // Test a high index (e.g., 1 million elements deep)
            // 1,048,576 / 1024 = 1024
            int highIndex = 1048576;
            Assert.AreEqual(1024, BufferMath.GetBatchIndex(highIndex, batchSize));

            // Check upper boundary of that high batch
            Assert.AreEqual(1024, BufferMath.GetBatchIndex(highIndex + 1023, batchSize));

            // Check start of the very next batch
            Assert.AreEqual(1025, BufferMath.GetBatchIndex(highIndex + 1024, batchSize));
        }

        [Test]
        public void GetElementRange_Standard_ReturnsCorrectSlice()
        {
            // Batch size 10, request Batch 1 to 2
            // Elements: [10-19] (Batch 1) and [20-29] (Batch 2)
            // Expected: Start 10, Count 20
            BufferMath.GetElementRange(1, 2, 10, 100, out int start, out int count);

            Assert.AreEqual(10, start);
            Assert.AreEqual(20, count);
            Assert.LessOrEqual(start + count, 100, "Range must stay within total capacity.");
        }

        [Test]
        public void GetElementRange_Boundary_LastBatchIsPartial()
        {
            // Total elements: 25, Batch size: 10
            // Batches are: B0 [0-9], B1 [10-19], B2 [20-24]
            // Requesting Batch 1 to 2
            // Expected: Start 10, Count 15 (10 elements from B1 + 5 elements from B2)
            BufferMath.GetElementRange(1, 2, 10, 25, out int start, out int count);

            Assert.AreEqual(10, start);
            Assert.AreEqual(15, count);
            Assert.AreEqual(25, start + count, "Should end exactly at total capacity.");
        }

        [Test]
        public void GetElementRange_Boundary_RequestExceedsCapacity()
        {
            // Requesting batches that simply don't exist in a small array
            // Total: 5 elements, Batch size: 10
            // Requesting Batch 1 to 5
            BufferMath.GetElementRange(1, 5, 10, 5, out int start, out int count);

            // Start would be 10, but capacity is 5. 
            // The logic should clamp start to capacity and count to 0.
            Assert.AreEqual(5, start);
            Assert.AreEqual(0, count);
        }

        [Test]
        public void GetElementRange_Boundary_StartAtZero()
        {
            // Standard start case
            BufferMath.GetElementRange(0, 0, 32, 100, out int start, out int count);

            Assert.AreEqual(0, start);
            Assert.AreEqual(32, count);
        }

        [Test]
        public void IsPowerOfAligned_Standard_IdentifiesMultiples()
        {
            Assert.IsTrue(BufferMath.IsPowerOfAligned(16, 64)); // 64 is 16 * 4
            Assert.IsTrue(BufferMath.IsPowerOfAligned(128, 64)); // 128 is 64 * 2
            Assert.IsFalse(BufferMath.IsPowerOfAligned(10, 32)); // Not a multiple
        }

        [Test]
        public void GetAlignedBatchSize_Standard_AlignsToNextMultiple()
        {
            // Alignment is 64. 
            // Requested 100 -> Next multiple of 64 is 128.
            Assert.AreEqual(128, BufferMath.GetAlignedBatchSize(100, 64, 16));

            // Requested 64 -> Already aligned, should stay 64.
            Assert.AreEqual(64, BufferMath.GetAlignedBatchSize(64, 64, 32));

            // Requested 1 -> Should jump to 64.
            Assert.AreEqual(64, BufferMath.GetAlignedBatchSize(1, 64, 64));
        }

        [Test]
        public void GetAlignedBatchSize_RespectsLargestAlignment()
        {
            // Two different alignments: 16 and 128.
            // The method must align to the LARGEST (128).
            // Requested 130 -> Next multiple of 128 is 256.
            Assert.AreEqual(256, BufferMath.GetAlignedBatchSize(130, 16, 128));

            // Reverse order of arguments should not matter.
            Assert.AreEqual(256, BufferMath.GetAlignedBatchSize(130, 128, 16));
        }

        [Test]
        public void GetAlignedBatchSize_SmallValues_AlignsCorrectly()
        {
            // Align to 4. 
            // 5 -> 8
            Assert.AreEqual(8, BufferMath.GetAlignedBatchSize(5, 4, 2));
            // 3 -> 4
            Assert.AreEqual(4, BufferMath.GetAlignedBatchSize(3, 4, 4));
        }

        // ================================================================
        // Boundary & Nonsense Cases (Stress Testing)
        // ================================================================

        [Test]
        [TestCase(100, 0, Description = "Division by zero fallback")]
        [TestCase(100, -32, Description = "Negative batch size fallback")]
        [TestCase(0, 32, Description = "Zero capacity")]
        [TestCase(-100, 32, Description = "Negative capacity")]
        [TestCase(-100, -32, Description = "Both negative")]
        public void GetTotalBatches_NonsenseInputs_ReturnsZero(int cap, int batch)
        {
            Assert.AreEqual(0, BufferMath.GetTotalBatches(cap, batch));
        }

        [Test]
        public void GetTotalBatches_ExtremeOverflow_CalculatesSafe()
        {
            // capacity + batchSize - 1 => would be int.MaxValue + 1023
            // Internal use of 'long' must prevent wrapping to a negative result.
            int hugeCap = int.MaxValue;
            int batchSize = 1024;
            int result = BufferMath.GetTotalBatches(hugeCap, batchSize);

            Assert.AreEqual(2097152, result); // Expected: 2^31 / 2^10
        }

        [Test]
        public void GetTotalBatches_MaxCapacity_MaxBatchSize_NoOverflow()
        {
            // Both are int.MaxValue. 
            // The sum (cap + batch - 1) is ~4.2 billion, which exceeds int but fits in long.
            int result = BufferMath.GetTotalBatches(int.MaxValue, int.MaxValue);
            Assert.AreEqual(1, result, "Two MaxValues should result in exactly 1 batch.");
        }

        [Test]
        [TestCase(-500, 32, 0)]
        [TestCase(100, 0, 0)]
        [TestCase(100, -1, 0)]
        public void GetBatchIndex_NonsenseInputs_ClampsToZero(int idx, int size, int expected)
        {
            Assert.AreEqual(expected, BufferMath.GetBatchIndex(idx, size));
        }

        [Test]
        public void GetBatchIndex_NegativeMaxValue_ClampsToZero()
        {
            // Even the most extreme negative index should never result in a negative batch.
            Assert.AreEqual(0, BufferMath.GetBatchIndex(int.MinValue, 32));
            Assert.AreEqual(0, BufferMath.GetBatchIndex(int.MinValue, int.MaxValue));
        }

        [Test]
        public void GetElementRange_NonsenseCombinations_SafeOuts()
        {
            // 1. StartBatch > EndBatch (Logical contradiction)
            BufferMath.GetElementRange(10, 5, 32, 1000, out int s1, out int c1);
            Assert.AreEqual(0, c1, "Count must be 0 if range is reversed");

            // 2. Negative Batch Indices
            BufferMath.GetElementRange(-10, -5, 32, 1000, out int s2, out int c2);
            Assert.AreEqual(0, c2, "Negative batches should result in empty range");

            // 3. Zero or Negative Batch Size
            BufferMath.GetElementRange(0, 10, 0, 1000, out int s3, out int c3);
            Assert.AreEqual(0, c3, "Invalid batch size must result in 0 count");

            // 4. Zero or Negative Capacity
            BufferMath.GetElementRange(0, 10, 32, -1, out int s4, out int c4);
            Assert.AreEqual(0, c4, "Invalid capacity must result in 0 count");
        }

        [Test]
        public void GetElementRange_MathOverflow_ClampsCorrectly()
        {
            // endBatch + 1 * batchSize would overflow if using 'int'
            // (int.MaxValue * 1024) is huge.
            BufferMath.GetElementRange(0, int.MaxValue, 1024, 5000, out int start, out int count);

            Assert.AreEqual(0, start);
            Assert.AreEqual(5000, count, "Should clamp exactly to total capacity without overflow");
        }

        [Test]
        public void GetElementRange_MaxValue_Reversed_ReturnsEmpty()
        {
            // StartBatch is Max, EndBatch is 0.
            BufferMath.GetElementRange(int.MaxValue, 0, 1024, 5000, out int start, out int count);
            Assert.AreEqual(0, count, "Reversed range with MaxValue must be empty.");
        }

        [Test]
        public void GetElementRange_AllValuesMaxValue_ClampsCorrectly()
        {
            // Everything is MaxValue.
            BufferMath.GetElementRange(int.MaxValue, int.MaxValue, int.MaxValue, int.MaxValue, out int start, out int count);

            // Start element would be MaxValue * MaxValue.
            // But it must be clamped to totalCapacity (MaxValue).
            Assert.AreEqual(int.MaxValue, start);
            Assert.AreEqual(0, count);
        }

        [Test]
        [TestCase(0, 16, false, Description = "Zero A")]
        [TestCase(16, 0, false, Description = "Zero B")]
        [TestCase(-16, 16, false, Description = "Negative A")]
        [TestCase(16, -16, false, Description = "Negative B")]
        [TestCase(int.MinValue, 16, false, Description = "Extreme Negative A")]
        [TestCase(1, int.MaxValue, true, Description = "Max Value is always aligned by 1")]
        [TestCase(int.MaxValue, int.MaxValue, true, Description = "Max Value is aligned by itself")]
        [TestCase(int.MaxValue, 2, false, Description = "Max Value (odd) is not aligned by 2")]
        public void IsPowerOfAligned_InvalidAndExtremeSizes_ReturnsExpected(int a, int b, bool expected)
        {
            Assert.AreEqual(expected, BufferMath.IsPowerOfAligned(a, b));
        }

        [Test]
        [TestCase(100, 0, 0, 100, Description = "Zero alignment returns requested")]
        [TestCase(100, -1, -1, 100, Description = "Negative alignment returns requested")]
        [TestCase(-100, 16, 16, 0, Description = "Negative requested returns zero")]
        public void GetAlignedBatchSize_Nonsense_ReturnsSafe(int req, int a, int b, int expected)
        {
            Assert.AreEqual(expected, BufferMath.GetAlignedBatchSize(req, a, b));
        }

        [Test]
        public void GetAlignedBatchSize_AlignmentCausesOverflow_ClampsOrHandles()
        {
            // Requested is near MaxValue, and maxBatch alignment would push it over.
            int requested = int.MaxValue - 5;
            int alignment = 1024;

            int result = BufferMath.GetAlignedBatchSize(requested, alignment, alignment);

            Assert.GreaterOrEqual(result, 0, "Aligned size should never wrap to negative.");
        }
    }
}