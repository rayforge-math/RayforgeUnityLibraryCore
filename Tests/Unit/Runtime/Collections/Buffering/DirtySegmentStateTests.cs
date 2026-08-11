using NUnit.Framework;
using Rayforge.Core.Collections.Abstractions;
using Rayforge.Core.Collections.Abstractions.Tests;
using Rayforge.Core.TestEnv;
using System;
using System.Collections;

namespace Rayforge.Core.Collections.Buffering.Tests
{
    [TestFixture(typeof(int))]
    [TestFixture(typeof(float))]
    [TestFixture(typeof(bool))]
    public class DirtySegmentStateTests<T> : IIterationLogicTests<BufferSegmentMeta<T>, DirtySegmentState<T>>
        where T : unmanaged
    {
        #region IIterationLogicTests Implementation

        protected override IterationTestData<BufferSegmentMeta<T>, DirtySegmentState<T>> CreateLogic(int count)
        {
            var dirtyBits = new BitArray(count);

            for(int i = 0; i < count; ++i)
            {
                dirtyBits.Set(i, true);
            }

            T[] items = TestUtility.CreateSampleItems<T>(count);

            var state = new DirtySegmentState<T>(items, dirtyBits, 0, items.Length, 1);

            var expected = new BufferSegmentMeta<T>[count];
            for(int i = 0; i < count; ++i)
            {
                expected[i] = new BufferSegmentMeta<T>
                {
                    Source = items,
                    Start = i,
                    Count = 1
                };
            }

            return new IterationTestData<BufferSegmentMeta<T>, DirtySegmentState<T>>
            {
                expected = expected,
                logic = state
            };
        }

        #endregion

        #region Constructor Tests

        [Test]
        public void Constructor_NullSource_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() =>
                new DirtySegmentState<int>(null, new BitArray(1), 0, 10, 2));
        }

        [Test]
        public void Constructor_NullDirtyBits_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() =>
                new DirtySegmentState<int>(new int[10], null, 0, 10, 2));
        }

        [Test]
        public void Constructor_NegativeOffset_ThrowsArgumentOutOfRangeException()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                new DirtySegmentState<int>(new int[10], new BitArray(5), -1, 10, 2));
        }

        [Test]
        public void Constructor_OffsetExceedsLength_ThrowsArgumentOutOfRangeException()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                new DirtySegmentState<int>(new int[10], new BitArray(5), 11, 10, 2));
        }

        [Test]
        public void Constructor_NegativeSize_ThrowsArgumentOutOfRangeException()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                new DirtySegmentState<int>(new int[10], new BitArray(5), 0, -1, 2));
        }

        [Test]
        public void Constructor_SizeExceedsBounds_ThrowsArgumentOutOfRangeException()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                new DirtySegmentState<int>(new int[10], new BitArray(5), 5, 6, 2));
        }

        [Test]
        public void Constructor_ZeroOrNegativeBatchSize_ThrowsArgumentOutOfRangeException()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                new DirtySegmentState<int>(new int[10], new BitArray(5), 0, 10, 0));

            Assert.Throws<ArgumentOutOfRangeException>(() =>
                new DirtySegmentState<int>(new int[10], new BitArray(5), 0, 10, -5));
        }

        [Test]
        public void Constructor_BitArrayTooSmall_ThrowsArgumentOutOfRangeException()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                new DirtySegmentState<int>(new int[10], new BitArray(4), 0, 10, 2));
        }

        [Test]
        public void Constructor_OffsetNotMultipleOfBatchSize_ThrowsArgumentException()
        {
            int[] source = new int[10];
            BitArray bits = new BitArray(10);

            // offset=3 is not a multiple of batchSize=2
            var ex = Assert.Throws<ArgumentException>(() =>
                new DirtySegmentState<int>(source, bits, offset: 3, size: 4, batchSize: 2));

            Assert.That(ex.ParamName, Is.EqualTo("offset"));
        }

        [Test]
        public void Constructor_SizeNotMultipleOfBatchSize_ThrowsArgumentException()
        {
            int[] source = new int[10];
            BitArray bits = new BitArray(10);

            // size=5 is not a multiple of batchSize=2
            var ex = Assert.Throws<ArgumentException>(() =>
                new DirtySegmentState<int>(source, bits, offset: 2, size: 5, batchSize: 2));

            Assert.That(ex.ParamName, Is.EqualTo("size"));
        }

        [Test]
        public void Constructor_BitArrayTooSmallForOffsetSlice_ThrowsArgumentOutOfRangeException()
        {
            // offset=6, batchSize=2 -> startBatch=3
            // size=6, batchSize=2 -> totalBatches=3
            // Scanner needs bits [3,4,5], i.e. BitArray.Length must be >= 6.
            int[] source = new int[12];
            BitArray bits = new BitArray(5); // one bit short of what's needed

            var ex = Assert.Throws<ArgumentOutOfRangeException>(() =>
                new DirtySegmentState<int>(source, bits, offset: 6, size: 6, batchSize: 2));

            Assert.That(ex.ParamName, Is.EqualTo("dirtyBits"));
        }

        [Test]
        public void Constructor_BitArrayExactlyLargeEnoughForOffsetSlice_DoesNotThrow()
        {
            // Same as above but BitArray.Length == 6 -> exactly enough, must NOT throw.
            int[] source = new int[12];
            BitArray bits = new BitArray(6);

            Assert.DoesNotThrow(() =>
                new DirtySegmentState<int>(source, bits, offset: 6, size: 6, batchSize: 2));
        }

        [TestCase(0, 4, 2)]  // offset=0 -> startBatch=0
        [TestCase(2, 4, 2)]  // offset=2, batchSize=2 -> startBatch=1
        [TestCase(6, 6, 2)]  // offset=6, batchSize=2 -> startBatch=3
        [TestCase(4, 4, 4)]  // offset=4, batchSize=4 -> startBatch=1
        [TestCase(8, 8, 4)]  // offset=8, batchSize=4 -> startBatch=2
        public void Constructor_ValidOffsetAndSizeCombinations_DoesNotThrow(int offset, int size, int batchSize)
        {
            int arrayLength = offset + size + batchSize; // generous buffer beyond the slice
            int[] source = new int[arrayLength];

            int startBatch = offset / batchSize;
            int totalBatches = size / batchSize;
            BitArray bits = new BitArray(startBatch + totalBatches); // exactly enough

            Assert.DoesNotThrow(() =>
                new DirtySegmentState<int>(source, bits, offset, size, batchSize),
                $"offset={offset}, size={size}, batchSize={batchSize} should be a valid combination.");
        }

        #endregion

        #region MoveNext Dirty Tests

        [Test]
        public void MoveNext_AlternatingDirtyBits_CorrectlySkipsFalseBits()
        {
            int batchSize = 2;
            var bits = new BitArray(6);
            for (int i = 0; i < 6; i++)
            {
                bits.Set(i, i % 2 == 0);
            }

            var state = new DirtySegmentState<int>(new int[12], bits, 0, 12, batchSize, merge: false);

            // 1. Segment
            Assert.IsTrue(state.MoveNext(ref state, out var seg1), "Should find first dirty batch.");
            Assert.AreEqual(0, seg1.Start);
            Assert.AreEqual(2, seg1.Count);

            // 2. Segment
            Assert.IsTrue(state.MoveNext(ref state, out var seg2), "Should find second dirty batch.");
            Assert.AreEqual(4, seg2.Start);
            Assert.AreEqual(2, seg2.Count);

            // 3. Segment
            Assert.IsTrue(state.MoveNext(ref state, out var seg3), "Should find third dirty batch.");
            Assert.AreEqual(8, seg3.Start);
            Assert.AreEqual(2, seg3.Count);

            Assert.IsFalse(state.MoveNext(ref state, out _), "Should be exhausted after 3 segments.");
        }

        [Test]
        public void MoveNext_OnlyFirstBitTrue_ReturnsFirstSegmentOnly()
        {
            // Arrange: Array of 10, 5 batches of size 2. Set first batch as dirty.
            var items = TestUtility.CreateSampleItems<T>(10);
            var bits = new BitArray(5);
            bits.Set(0, true);

            var state = new DirtySegmentState<T>(items, bits, 0, 10, 2);

            // Act & Assert: Only the first batch should be returned.
            Assert.IsTrue(state.MoveNext(ref state, out var seg));
            Assert.AreEqual(0, seg.Start);
            Assert.AreEqual(2, seg.Count);
            Assert.IsFalse(state.MoveNext(ref state, out _));
        }

        [Test]
        public void MoveNext_OnlyLastBitTrue_ReturnsLastSegmentOnly()
        {
            // Arrange: Set last batch (index 4) as dirty.
            var items = TestUtility.CreateSampleItems<T>(10);
            var bits = new BitArray(5);
            bits.Set(4, true);

            var state = new DirtySegmentState<T>(items, bits, 0, 10, 2);

            // Act & Assert: Only the last batch should be returned.
            Assert.IsTrue(state.MoveNext(ref state, out var seg));
            Assert.AreEqual(8, seg.Start);
            Assert.AreEqual(2, seg.Count);
            Assert.IsFalse(state.MoveNext(ref state, out _));
        }

        [Test]
        public void MoveNext_OnlyMiddleBitTrue_ReturnsMiddleSegmentOnly()
        {
            // Arrange: Set middle batch (index 2) as dirty.
            var items = TestUtility.CreateSampleItems<T>(10);
            var bits = new BitArray(5);
            bits.Set(2, true);

            var state = new DirtySegmentState<T>(items, bits, 0, 10, 2);

            // Act & Assert: Only the middle batch should be returned.
            Assert.IsTrue(state.MoveNext(ref state, out var seg));
            Assert.AreEqual(4, seg.Start);
            Assert.AreEqual(2, seg.Count);
            Assert.IsFalse(state.MoveNext(ref state, out _));
        }

        [Test]
        public void MoveNext_AllBitsFalse_ReturnsNothing()
        {
            // Arrange: No dirty bits set.
            var items = TestUtility.CreateSampleItems<T>(10);
            var bits = new BitArray(5);

            var state = new DirtySegmentState<T>(items, bits, 0, 10, 2);

            // Act & Assert: Iterator should be immediately empty.
            Assert.IsFalse(state.MoveNext(ref state, out _));
        }

        [Test]
        public void MoveNext_BatchSize2_CalculatesCorrectOffsets()
        {
            // Arrange: 10 elements, batch size 2 = 5 batches.
            // Set bit 2 as dirty -> Batch 2 (indices 4-5).
            var items = TestUtility.CreateSampleItems<T>(10);
            var bits = new BitArray(5);
            bits.Set(2, true);

            var state = new DirtySegmentState<T>(items, bits, 0, 10, batchSize: 2);

            // Act & Assert: Verify correct offset calculation.
            Assert.IsTrue(state.MoveNext(ref state, out var seg));
            Assert.AreEqual(4, seg.Start);
            Assert.AreEqual(2, seg.Count);
        }

        [Test]
        public void MoveNext_BatchSize4_CalculatesCorrectOffsets()
        {
            var items = TestUtility.CreateSampleItems<T>(16);
            var bits = new BitArray(16);
            bits.Set(1, true);

            var state = new DirtySegmentState<T>(items, bits, 0, 16, batchSize: 4);

            // Act & Assert: Verify batch spanning 4 elements.
            Assert.IsTrue(state.MoveNext(ref state, out var seg));
            Assert.AreEqual(4, seg.Start);
            Assert.AreEqual(4, seg.Count);
        }

        [Test]
        public void MoveNext_BatchSizeFullArray_CalculatesCorrectOffsets()
        {
            // Arrange: Entire array is one single batch.
            var items = TestUtility.CreateSampleItems<T>(10);
            var bits = new BitArray(1);
            bits.Set(0, true);

            var state = new DirtySegmentState<T>(items, bits, 0, 10, batchSize: 10);

            // Act & Assert: Should cover the full range.
            Assert.IsTrue(state.MoveNext(ref state, out var seg));
            Assert.AreEqual(0, seg.Start);
            Assert.AreEqual(10, seg.Count);
        }

        [Test]
        public void MoveNext_MergeFirstTwoBits_ReturnsOneCombinedSegment()
        {
            // Arrange: 5 batches, batch size 2. Set first two batches as dirty.
            var items = TestUtility.CreateSampleItems<T>(10);
            var bits = new BitArray(5);
            bits.Set(0, true);
            bits.Set(1, true);

            var state = new DirtySegmentState<T>(items, bits, 0, 10, batchSize: 2, merge: true);

            // Act & Assert: Should combine index 0-1 and 2-3 into one segment of count 4.
            Assert.IsTrue(state.MoveNext(ref state, out var seg));
            Assert.AreEqual(0, seg.Start);
            Assert.AreEqual(4, seg.Count);
            Assert.IsFalse(state.MoveNext(ref state, out _));
        }

        [Test]
        public void MoveNext_MergeLastTwoBits_ReturnsOneCombinedSegment()
        {
            // Arrange: Set last two batches as dirty.
            var items = TestUtility.CreateSampleItems<T>(10);
            var bits = new BitArray(5);
            bits.Set(3, true);
            bits.Set(4, true);

            var state = new DirtySegmentState<T>(items, bits, 0, 10, batchSize: 2, merge: true);

            // Act & Assert: Should combine index 6-7 and 8-9 into one segment of count 4.
            Assert.IsTrue(state.MoveNext(ref state, out var seg));
            Assert.AreEqual(6, seg.Start);
            Assert.AreEqual(4, seg.Count);
            Assert.IsFalse(state.MoveNext(ref state, out _));
        }

        [Test]
        public void MoveNext_MergeMiddleBits_ReturnsOneCombinedSegment()
        {
            // Arrange: Set middle batches (1, 2, 3) as dirty.
            var items = TestUtility.CreateSampleItems<T>(10);
            var bits = new BitArray(5);
            bits.Set(1, true);
            bits.Set(2, true);
            bits.Set(3, true);

            var state = new DirtySegmentState<T>(items, bits, 0, 10, batchSize: 2, merge: true);

            // Act & Assert: Should combine index 2-3, 4-5, 6-7 into one segment of count 6.
            Assert.IsTrue(state.MoveNext(ref state, out var seg));
            Assert.AreEqual(2, seg.Start);
            Assert.AreEqual(6, seg.Count);
            Assert.IsFalse(state.MoveNext(ref state, out _));
        }

        [Test]
        public void MoveNext_MixedDirtyBitsWithMerge_ReturnsSeparateMergedSegments()
        {
            // Arrange: Pattern True, True, False, True, True.
            // Should result in two segments: [0-3] and [6-9].
            var items = TestUtility.CreateSampleItems<T>(10);
            var bits = new BitArray(5);
            bits.Set(0, true);
            bits.Set(1, true);
            // bit 2 is false
            bits.Set(3, true);
            bits.Set(4, true);

            var state = new DirtySegmentState<T>(items, bits, 0, 10, batchSize: 2, merge: true);

            // Act & Assert: First merged segment.
            Assert.IsTrue(state.MoveNext(ref state, out var seg1));
            Assert.AreEqual(0, seg1.Start);
            Assert.AreEqual(4, seg1.Count);

            // Act & Assert: Second merged segment.
            Assert.IsTrue(state.MoveNext(ref state, out var seg2));
            Assert.AreEqual(6, seg2.Start);
            Assert.AreEqual(4, seg2.Count);

            Assert.IsFalse(state.MoveNext(ref state, out _));
        }

        [Test]
        public void MoveNext_MergeContiguousBitsWithOffset_ReturnsCombinedSegmentWithAbsoluteStart()
        {
            // Slice: offset=6, size=8, batchSize=2 -> startBatch=3, totalBatches=4.
            // Valid slice range in elements: [6, 14).
            // Dirty batches 4 and 5 (absolute) are contiguous -> should merge into one segment.
            // Batch 4 -> elements [8,9], batch 5 -> elements [10,11].
            const int offset = 6;
            const int size = 8;
            const int batchSize = 2;

            var items = TestUtility.CreateSampleItems<T>(20);
            var bits = new BitArray(20);
            bits.Set(4, true);
            bits.Set(5, true);

            var state = new DirtySegmentState<T>(items, bits, offset, size, batchSize, merge: true);

            Assert.IsTrue(state.MoveNext(ref state, out var seg),
                "Should find the merged dirty segment.");
            Assert.AreEqual(8, seg.Start, "Result must report the absolute start, not relative to offset.");
            Assert.AreEqual(4, seg.Count, "Two contiguous batches of size 2 should merge into a count of 4.");
            Assert.IsFalse(state.MoveNext(ref state, out _), "Should be exhausted after the merged segment.");
        }

        [Test]
        public void MoveNext_DirtyBitBeforeOffsetSlice_IsIgnored()
        {
            // Slice: offset=6, size=6, batchSize=2 -> startBatch=3.
            // Dirty bit at batch 2 corresponds to elements [4,5], which lies before the slice start (element 6).
            const int offset = 6;
            const int size = 6;
            const int batchSize = 2;

            var items = TestUtility.CreateSampleItems<T>(12);
            var bits = new BitArray(6);
            bits.Set(2, true);

            var state = new DirtySegmentState<T>(items, bits, offset, size, batchSize);

            Assert.IsFalse(state.MoveNext(ref state, out _),
                "Dirty bit before the offset-relative slice must be ignored, not just clamped.");
        }

        #endregion

        #region TryPeekNext Tests

        [Test]
        public void TryPeekNext_AlternatingDirtyBits_CorrectlySkipsFalseBits()
        {
            // Arrange: 6 batches, pattern: True, False, True, False, True, False.
            int batchSize = 2;
            var bits = new BitArray(6);
            for (int i = 0; i < 6; i++)
            {
                bits.Set(i, i % 2 == 0);
            }

            var state = new DirtySegmentState<int>(new int[12], bits, 0, 12, batchSize, merge: false);

            // Act & Assert: Peek should find the first dirty batch without consuming it.
            Assert.IsTrue(state.TryPeekNext(ref state, out var peek1));
            Assert.AreEqual(0, peek1.Start);
            Assert.AreEqual(2, peek1.Count);

            // Consume it
            state.MoveNext(ref state, out _);

            // Peek second dirty batch
            Assert.IsTrue(state.TryPeekNext(ref state, out var peek2));
            Assert.AreEqual(4, peek2.Start);
            Assert.AreEqual(2, peek2.Count);
        }

        [Test]
        public void TryPeekNext_OnlyFirstBitTrue_ReturnsFirstSegmentOnly()
        {
            // Arrange: Array of 10, 5 batches of size 2. Set first batch as dirty.
            var items = TestUtility.CreateSampleItems<T>(10);
            var bits = new BitArray(5);
            bits.Set(0, true);

            var state = new DirtySegmentState<T>(items, bits, 0, 10, 2);

            // Act & Assert: Peek should return the first batch repeatedly without consuming it.
            Assert.IsTrue(state.TryPeekNext(ref state, out var peek1));
            Assert.AreEqual(0, peek1.Start);
            Assert.AreEqual(2, peek1.Count);

            // Verify it is still available after peeking
            Assert.IsTrue(state.TryPeekNext(ref state, out var peek2));
            Assert.AreEqual(peek1.Start, peek2.Start);

            // Consume it
            state.MoveNext(ref state, out _);
            Assert.IsFalse(state.TryPeekNext(ref state, out _));
        }

        [Test]
        public void TryPeekNext_OnlyLastBitTrue_ReturnsLastSegmentOnly()
        {
            // Arrange: Set last batch (index 4) as dirty.
            var items = TestUtility.CreateSampleItems<T>(10);
            var bits = new BitArray(5);
            bits.Set(4, true);

            var state = new DirtySegmentState<T>(items, bits, 0, 10, 2);

            // Act & Assert: Peek the last batch.
            Assert.IsTrue(state.TryPeekNext(ref state, out var peek));
            Assert.AreEqual(8, peek.Start);
            Assert.AreEqual(2, peek.Count);
        }

        [Test]
        public void TryPeekNext_OnlyMiddleBitTrue_ReturnsMiddleSegmentOnly()
        {
            // Arrange: Set middle batch (index 2) as dirty.
            var items = TestUtility.CreateSampleItems<T>(10);
            var bits = new BitArray(5);
            bits.Set(2, true);

            var state = new DirtySegmentState<T>(items, bits, 0, 10, 2);

            // Act & Assert: Peek the middle batch.
            Assert.IsTrue(state.TryPeekNext(ref state, out var peek));
            Assert.AreEqual(4, peek.Start);
            Assert.AreEqual(2, peek.Count);
        }

        [Test]
        public void TryPeekNext_AllBitsFalse_ReturnsNothing()
        {
            // Arrange: No dirty bits set.
            var items = TestUtility.CreateSampleItems<T>(10);
            var bits = new BitArray(5);

            var state = new DirtySegmentState<T>(items, bits, 0, 10, 2);

            // Act & Assert: Should correctly report no segments available.
            Assert.IsFalse(state.TryPeekNext(ref state, out _));
        }

        [Test]
        public void TryPeekNext_BatchSize2_CalculatesCorrectOffsets()
        {
            // Arrange: 10 elements, batch size 2 = 5 batches.
            // Set bit 2 as dirty -> Batch 2 (indices 4-5).
            var items = TestUtility.CreateSampleItems<T>(10);
            var bits = new BitArray(5);
            bits.Set(2, true);

            var state = new DirtySegmentState<T>(items, bits, 0, 10, batchSize: 2);

            // Act & Assert: Verify correct offset calculation via peek.
            Assert.IsTrue(state.TryPeekNext(ref state, out var seg));
            Assert.AreEqual(4, seg.Start);
            Assert.AreEqual(2, seg.Count);

            // Verify that the state was not consumed.
            Assert.IsTrue(state.TryPeekNext(ref state, out _));
        }

        [Test]
        public void TryPeekNext_BatchSize4_CalculatesCorrectOffsets()
        {
            // Arrange
            var items = TestUtility.CreateSampleItems<T>(16);
            var bits = new BitArray(16);
            bits.Set(1, true);

            var state = new DirtySegmentState<T>(items, bits, 0, 16, batchSize: 4);

            // Act & Assert: Verify batch spanning 4 elements without consuming.
            Assert.IsTrue(state.TryPeekNext(ref state, out var seg));
            Assert.AreEqual(4, seg.Start);
            Assert.AreEqual(4, seg.Count);
        }

        [Test]
        public void TryPeekNext_BatchSizeFullArray_CalculatesCorrectOffsets()
        {
            // Arrange: Entire array is one single batch.
            var items = TestUtility.CreateSampleItems<T>(10);
            var bits = new BitArray(1);
            bits.Set(0, true);

            var state = new DirtySegmentState<T>(items, bits, 0, 10, batchSize: 10);

            // Act & Assert: Should cover the full range without consuming.
            Assert.IsTrue(state.TryPeekNext(ref state, out var seg));
            Assert.AreEqual(0, seg.Start);
            Assert.AreEqual(10, seg.Count);
        }

        [Test]
        public void TryPeekNext_MergeFirstTwoBits_ReturnsOneCombinedSegment()
        {
            // Arrange: 5 batches, batch size 2. Set first two batches as dirty.
            var items = TestUtility.CreateSampleItems<T>(10);
            var bits = new BitArray(5);
            bits.Set(0, true);
            bits.Set(1, true);

            var state = new DirtySegmentState<T>(items, bits, 0, 10, batchSize: 2, merge: true);

            // Act & Assert: Peek should return the combined segment of count 4.
            Assert.IsTrue(state.TryPeekNext(ref state, out var seg));
            Assert.AreEqual(0, seg.Start);
            Assert.AreEqual(4, seg.Count);

            // Verify that the state is not consumed yet.
            Assert.IsTrue(state.TryPeekNext(ref state, out _));
        }

        [Test]
        public void TryPeekNext_MergeLastTwoBits_ReturnsOneCombinedSegment()
        {
            // Arrange: Set last two batches as dirty.
            var items = TestUtility.CreateSampleItems<T>(10);
            var bits = new BitArray(5);
            bits.Set(3, true);
            bits.Set(4, true);

            var state = new DirtySegmentState<T>(items, bits, 0, 10, batchSize: 2, merge: true);

            // Act & Assert: Should combine index 6-7 and 8-9 into one segment of count 4.
            Assert.IsTrue(state.TryPeekNext(ref state, out var seg));
            Assert.AreEqual(6, seg.Start);
            Assert.AreEqual(4, seg.Count);
        }

        [Test]
        public void TryPeekNext_MergeMiddleBits_ReturnsOneCombinedSegment()
        {
            // Arrange: Set middle batches (1, 2, 3) as dirty.
            var items = TestUtility.CreateSampleItems<T>(10);
            var bits = new BitArray(5);
            bits.Set(1, true);
            bits.Set(2, true);
            bits.Set(3, true);

            var state = new DirtySegmentState<T>(items, bits, 0, 10, batchSize: 2, merge: true);

            // Act & Assert: Peek should return the combined segment covering batches 1, 2, and 3.
            Assert.IsTrue(state.TryPeekNext(ref state, out var seg));
            Assert.AreEqual(2, seg.Start);
            Assert.AreEqual(6, seg.Count);
        }

        [Test]
        public void TryPeekNext_MixedDirtyBitsWithMerge_ReturnsSeparateMergedSegments()
        {
            // Arrange: Pattern True, True, False, True, True.
            // Expected segments: [0-3] and [6-9].
            var items = TestUtility.CreateSampleItems<T>(10);
            var bits = new BitArray(5);
            bits.Set(0, true);
            bits.Set(1, true);
            // bit 2 is false
            bits.Set(3, true);
            bits.Set(4, true);

            var state = new DirtySegmentState<T>(items, bits, 0, 10, batchSize: 2, merge: true);

            // Act & Assert: Peek first merged segment.
            Assert.IsTrue(state.TryPeekNext(ref state, out var seg1));
            Assert.AreEqual(0, seg1.Start);
            Assert.AreEqual(4, seg1.Count);

            // Consume first segment
            state.MoveNext(ref state, out _);

            // Peek second merged segment
            Assert.IsTrue(state.TryPeekNext(ref state, out var seg2));
            Assert.AreEqual(6, seg2.Start);
            Assert.AreEqual(4, seg2.Count);
        }

        [Test]
        public void TryPeekNext_DirtyBitWithinOffsetSlice_IsDetectedWithCorrectAbsoluteStart()
        {
            const int offset = 6;
            const int size = 6;
            const int batchSize = 2;

            var items = TestUtility.CreateSampleItems<T>(12);
            var bits = new BitArray(6);

            int startBatch = offset / batchSize;
            bits.Set(startBatch + 1, true); // dirty batch within the slice -> absolute element start 8

            var state = new DirtySegmentState<T>(items, bits, offset, size, batchSize);

            Assert.IsTrue(state.TryPeekNext(ref state, out var seg),
                "Dirty bit within the offset-relative slice should be peekable.");
            Assert.AreEqual(8, seg.Start);
            Assert.AreEqual(batchSize, seg.Count);
        }

        [Test]
        public void TryPeekNext_DirtyBitBeforeOffsetSlice_IsIgnored()
        {
            const int offset = 6;
            const int size = 6;
            const int batchSize = 2;

            var items = TestUtility.CreateSampleItems<T>(12);
            var bits = new BitArray(6);
            bits.Set(2, true);

            var state = new DirtySegmentState<T>(items, bits, offset, size, batchSize);

            Assert.IsFalse(state.TryPeekNext(ref state, out _),
                "Dirty bit before the offset-relative slice must be ignored, not just clamped.");
        }

        #endregion

        #region HasNext Tests

        [Test]
        public void HasNext_NoDirtyBitsSet_ReturnsFalse()
        {
            var items = TestUtility.CreateSampleItems<T>(10);
            var bits = new BitArray(5); // All false
            var state = new DirtySegmentState<T>(items, bits, 0, 10, batchSize: 2);

            Assert.IsFalse(state.HasNext(ref state), "Should be empty if no bits are set.");
        }

        [Test]
        public void HasNext_SingleDirtyBitSet_ReturnsTrue()
        {
            var items = TestUtility.CreateSampleItems<T>(10);
            var bits = new BitArray(5);
            bits.Set(2, true);
            var state = new DirtySegmentState<T>(items, bits, 0, 10, batchSize: 2);

            Assert.IsTrue(state.HasNext(ref state));
            state.MoveNext(ref state, out _);
            Assert.IsFalse(state.HasNext(ref state), "Should be exhausted after consuming the only dirty bit.");
        }

        [Test]
        public void HasNext_MultipleDisjointDirtyBits_ReturnsTrueUntilAllConsumed()
        {
            var items = TestUtility.CreateSampleItems<T>(10);
            var bits = new BitArray(5);
            bits.Set(0, true);
            bits.Set(4, true);
            var state = new DirtySegmentState<T>(items, bits, 0, 10, batchSize: 2);

            Assert.IsTrue(state.HasNext(ref state));
            state.MoveNext(ref state, out _); // Consumes index 0
            Assert.IsTrue(state.HasNext(ref state));
            state.MoveNext(ref state, out _); // Consumes index 4
            Assert.IsFalse(state.HasNext(ref state), "Should be exhausted after all dirty bits processed.");
        }

        [Test]
        public void HasNext_WithMerging_ReturnsTrueUntilMergedSegmentConsumed()
        {
            // 5 batches, merge enabled. Set 0 and 1 dirty -> 1 combined segment.
            var items = TestUtility.CreateSampleItems<T>(10);
            var bits = new BitArray(5);
            bits.Set(0, true);
            bits.Set(1, true);
            var state = new DirtySegmentState<T>(items, bits, 0, 10, batchSize: 2, merge: true);

            Assert.IsTrue(state.HasNext(ref state));
            state.MoveNext(ref state, out _); // Consumes both bits as 1 segment
            Assert.IsFalse(state.HasNext(ref state), "Should be exhausted after the merged segment.");
        }

        [Test]
        public void HasNext_MixedWithMerge_CorrectlyTracksMultipleMergedSegments()
        {
            // Pattern: T, T, F, T, T
            var items = TestUtility.CreateSampleItems<T>(10);
            var bits = new BitArray(5);
            bits.Set(0, true); bits.Set(1, true);
            bits.Set(3, true); bits.Set(4, true);
            var state = new DirtySegmentState<T>(items, bits, 0, 10, batchSize: 2, merge: true);

            Assert.IsTrue(state.HasNext(ref state));
            state.MoveNext(ref state, out _); // Consumes 0-3
            Assert.IsTrue(state.HasNext(ref state), "Still has the second merged segment (6-9).");
            state.MoveNext(ref state, out _); // Consumes 6-9
            Assert.IsFalse(state.HasNext(ref state), "Exhausted.");
        }

        [Test]
        public void HasNext_BitOutsideRange_ReturnsFalse()
        {
            // Set bit for batch 4, but size is only 4 elements (batches 0 and 1)
            var items = TestUtility.CreateSampleItems<T>(4);
            var bits = new BitArray(5);
            bits.Set(4, true);
            var state = new DirtySegmentState<T>(items, bits, 0, 4, batchSize: 2);

            Assert.IsFalse(state.HasNext(ref state), "Should ignore dirty bits that fall outside the defined size.");
        }

        [Test]
        public void HasNext_DirtyBitWithinOffsetSlice_ReturnsTrue()
        {
            // Slice: offset=6, size=6, batchSize=2 -> startBatch=3, totalBatches=3.
            // Valid slice range in elements: [6, 12).
            const int offset = 6;
            const int size = 6;
            const int batchSize = 2;

            var items = TestUtility.CreateSampleItems<T>(12);
            var bits = new BitArray(6);

            int startBatch = offset / batchSize;
            bits.Set(startBatch + 1, true); // dirty batch within the slice -> absolute element start 8

            var state = new DirtySegmentState<T>(items, bits, offset, size, batchSize);

            Assert.IsTrue(state.HasNext(ref state),
                "Dirty bit within the offset-relative slice should be detected.");
        }

        [Test]
        public void HasNext_DirtyBitBeforeOffsetSlice_ReturnsFalse()
        {
            // Slice: offset=6, size=6, batchSize=2 -> startBatch=3.
            // Dirty bit at batch 2 corresponds to elements [4,5], which lies before the slice start (element 6).
            const int offset = 6;
            const int size = 6;
            const int batchSize = 2;

            var items = TestUtility.CreateSampleItems<T>(12);
            var bits = new BitArray(6);
            bits.Set(2, true);

            var state = new DirtySegmentState<T>(items, bits, offset, size, batchSize);

            Assert.IsFalse(state.HasNext(ref state),
                "Dirty bit before the offset-relative slice must be ignored, not just clamped.");
        }

        #endregion
    }
}
