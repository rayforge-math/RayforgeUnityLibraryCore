using NUnit.Framework;
using Rayforge.Core.Collections.Abstractions;
using Rayforge.Core.Collections.Abstractions.Tests;
using Rayforge.Core.Collections.Iterator;
using Rayforge.Core.TestEnv;
using System;
using System.Collections.Generic;

namespace Rayforge.Core.Collections.Buffering.Tests
{
    [TestFixture(typeof(int))]
    [TestFixture(typeof(float))]
    [TestFixture(typeof(bool))]
    public class BufferSegmentStateTests<T> : IIterationLogicTests<BufferSegmentMeta<T>, BufferSegmentState<T>>
        where T : unmanaged
    {
        #region IIterationLogic Impl

        protected override IterationTestData<BufferSegmentMeta<T>, BufferSegmentState<T>> CreateLogic(int count)
        {
            T[] items = TestUtility.CreateSampleItems<T>(count);
            var logic = new BufferSegmentState<T>(items, 0, items.Length, 1);

            var expected = new BufferSegmentMeta<T>[count];
            for(int i = 0; i < items.Length; ++i)
            {
                expected[i] = new BufferSegmentMeta<T>
                {
                    Source = items,
                    Start = i,
                    Count = 1
                };
            }

            return new IterationTestData<BufferSegmentMeta<T>, BufferSegmentState<T>>
            {
                logic = logic,
                expected = expected
            };
        }

        #endregion

        #region Constructor

        [Test]
        public void Constructor_NullSource_ThrowsArgumentNullException()
        {
            // Act & Assert: Should throw when passing a null array.
            Assert.Throws<ArgumentNullException>(() => new BufferSegmentState<T>(null!, 0, 5, 2));
        }

        [Test]
        public void Constructor_NegativeOffset_ThrowsArgumentOutOfRangeException()
        {
            var items = TestUtility.CreateSampleItems<T>(10);
            // Act & Assert: Negative offset is invalid.
            Assert.Throws<ArgumentOutOfRangeException>(() => new BufferSegmentState<T>(items, -1, 5, 2));
        }

        [Test]
        public void Constructor_OffsetExceedsArrayLength_ThrowsArgumentOutOfRangeException()
        {
            var items = TestUtility.CreateSampleItems<T>(10);
            // Act & Assert: Offset greater than array length is invalid.
            Assert.Throws<ArgumentOutOfRangeException>(() => new BufferSegmentState<T>(items, 11, 0, 2));
        }

        [Test]
        public void Constructor_NegativeSize_ThrowsArgumentOutOfRangeException()
        {
            var items = TestUtility.CreateSampleItems<T>(10);
            // Act & Assert: Size cannot be negative.
            Assert.Throws<ArgumentOutOfRangeException>(() => new BufferSegmentState<T>(items, 0, -1, 2));
        }

        [Test]
        public void Constructor_RangeExceedsArrayBounds_ThrowsArgumentOutOfRangeException()
        {
            var items = TestUtility.CreateSampleItems<T>(10);
            // Act & Assert: Offset 5 + Size 6 = 11, which exceeds array length of 10.
            Assert.Throws<ArgumentOutOfRangeException>(() => new BufferSegmentState<T>(items, 5, 6, 2));
        }

        [Test]
        public void Constructor_NegativeBatchSize_ThrowsArgumentOutOfRangeException()
        {
            var items = TestUtility.CreateSampleItems<T>(10);
            // Act & Assert: Batch size cannot be negative.
            Assert.Throws<ArgumentOutOfRangeException>(() => new BufferSegmentState<T>(items, 0, 5, -1));
        }

        [Test]
        public void Constructor_ValidInputs_InitializesCorrectly()
        {
            var items = TestUtility.CreateSampleItems<T>(10);
            // Act: Initialize with valid parameters.
            var state = new BufferSegmentState<T>(items, 2, 5, 2);

            // Assert: Ensure state fields are assigned correctly.
            Assert.AreEqual(2, state.Offset);
            Assert.AreEqual(5, state.Size);
            Assert.AreEqual(2, state.BatchSize);
        }

        #endregion

        #region MoveNext BatchSize Tests

        [Test]
        public void MoveNext_BatchSizeZero_ReturnsEntireBufferAsSingleSegment()
        {
            // Arrange: Batch size 0 should treat the whole size as one segment.
            var items = TestUtility.CreateSampleItems<T>(10);
            var state = new BufferSegmentState<T>(items, 0, 10, batchSize: 0);

            // Act & Assert
            Assert.IsTrue(state.MoveNext(ref state, out var seg));
            Assert.AreEqual(0, seg.Start);
            Assert.AreEqual(10, seg.Count);
            Assert.IsFalse(state.MoveNext(ref state, out _));
        }

        [Test]
        public void MoveNext_BatchSizeMatchesSize_ReturnsSingleSegment()
        {
            // Arrange: Batch size equals total size.
            var items = TestUtility.CreateSampleItems<T>(10);
            var state = new BufferSegmentState<T>(items, 0, 10, batchSize: 10);

            // Act & Assert
            Assert.IsTrue(state.MoveNext(ref state, out var seg));
            Assert.AreEqual(0, seg.Start);
            Assert.AreEqual(10, seg.Count);
            Assert.IsFalse(state.MoveNext(ref state, out _));
        }

        [Test]
        public void MoveNext_BatchSizeSmallerThanSize_ReturnsMultipleSegments()
        {
            // Arrange: 10 elements, batch size 3. Should yield 3, 3, 3, 1.
            var items = TestUtility.CreateSampleItems<T>(10);
            var state = new BufferSegmentState<T>(items, 0, 10, batchSize: 3);

            // Act & Assert: Verify sequence of segments.
            Assert.IsTrue(state.MoveNext(ref state, out var seg1));
            Assert.AreEqual(0, seg1.Start);
            Assert.AreEqual(3, seg1.Count);

            Assert.IsTrue(state.MoveNext(ref state, out var seg2));
            Assert.AreEqual(3, seg2.Start);
            Assert.AreEqual(3, seg2.Count);

            Assert.IsTrue(state.MoveNext(ref state, out var seg3));
            Assert.AreEqual(6, seg3.Start);
            Assert.AreEqual(3, seg3.Count);

            Assert.IsTrue(state.MoveNext(ref state, out var seg4));
            Assert.AreEqual(9, seg4.Start);
            Assert.AreEqual(1, seg4.Count);

            Assert.IsFalse(state.MoveNext(ref state, out _));
        }

        [Test]
        public void MoveNext_BatchSizeLargerThanSize_ReturnsRemainingAsSingleSegment()
        {
            // Arrange: 10 elements, batch size 15.
            var items = TestUtility.CreateSampleItems<T>(10);
            var state = new BufferSegmentState<T>(items, 0, 10, batchSize: 15);

            // Act & Assert: Should be handled as one segment of 10.
            Assert.IsTrue(state.MoveNext(ref state, out var seg));
            Assert.AreEqual(0, seg.Start);
            Assert.AreEqual(10, seg.Count);
            Assert.IsFalse(state.MoveNext(ref state, out _));
        }

        [Test]
        public void MoveNext_WithOffset_StartsAtCorrectPosition()
        {
            // Arrange: Array of 10, process 5 elements starting at offset 3.
            var items = TestUtility.CreateSampleItems<T>(10);
            var state = new BufferSegmentState<T>(items, offset: 3, size: 5, batchSize: 2);

            // Act & Assert: 
            // Segment 1 (indices 3-4)
            Assert.IsTrue(state.MoveNext(ref state, out var seg1));
            Assert.AreEqual(3, seg1.Start);
            Assert.AreEqual(2, seg1.Count);

            // Segment 2 (indices 5-6)
            Assert.IsTrue(state.MoveNext(ref state, out var seg2));
            Assert.AreEqual(5, seg2.Start);
            Assert.AreEqual(2, seg2.Count);

            // Segment 3 (index 7)
            Assert.IsTrue(state.MoveNext(ref state, out var seg3));
            Assert.AreEqual(7, seg3.Start);
            Assert.AreEqual(1, seg3.Count);

            Assert.IsFalse(state.MoveNext(ref state, out _));
        }

        #endregion

        #region TryPeekNext BatchSize Tests

        [Test]
        public void TryPeekNext_BatchSizeZero_ReturnsEntireBufferAsSingleSegment()
        {
            // Arrange: Batch size 0 should treat the whole size as one segment.
            var items = TestUtility.CreateSampleItems<T>(10);
            var state = new BufferSegmentState<T>(items, 0, 10, batchSize: 0);

            // Act & Assert: Peek returns the full segment and remains at start.
            Assert.IsTrue(state.TryPeekNext(ref state, out var seg));
            Assert.AreEqual(0, seg.Start);
            Assert.AreEqual(10, seg.Count);

            // Verify it didn't advance (can peek again and get the same result)
            Assert.IsTrue(state.TryPeekNext(ref state, out var seg2));
            Assert.AreEqual(seg.Start, seg2.Start);
        }

        [Test]
        public void TryPeekNext_BatchSizeMatchesSize_ReturnsSingleSegment()
        {
            var items = TestUtility.CreateSampleItems<T>(10);
            var state = new BufferSegmentState<T>(items, 0, 10, batchSize: 10);

            Assert.IsTrue(state.TryPeekNext(ref state, out var seg));
            Assert.AreEqual(0, seg.Start);
            Assert.AreEqual(10, seg.Count);
        }

        [Test]
        public void TryPeekNext_BatchSizeSmallerThanSize_ReturnsFirstSegment()
        {
            // Arrange: 10 elements, batch size 3. Should peek first 3.
            var items = TestUtility.CreateSampleItems<T>(10);
            var state = new BufferSegmentState<T>(items, 0, 10, batchSize: 3);

            // Act & Assert
            Assert.IsTrue(state.TryPeekNext(ref state, out var seg));
            Assert.AreEqual(0, seg.Start);
            Assert.AreEqual(3, seg.Count);

            // Verify it didn't consume (consume now to check next)
            state.MoveNext(ref state, out _);
            Assert.IsTrue(state.TryPeekNext(ref state, out var seg2));
            Assert.AreEqual(3, seg2.Start);
        }

        [Test]
        public void TryPeekNext_BatchSizeLargerThanSize_ReturnsRemainingAsSingleSegment()
        {
            var items = TestUtility.CreateSampleItems<T>(10);
            var state = new BufferSegmentState<T>(items, 0, 10, batchSize: 15);

            Assert.IsTrue(state.TryPeekNext(ref state, out var seg));
            Assert.AreEqual(0, seg.Start);
            Assert.AreEqual(10, seg.Count);
        }

        [Test]
        public void TryPeekNext_WithOffset_StartsAtCorrectPosition()
        {
            // Arrange: Array of 10, process 5 elements starting at offset 3.
            var items = TestUtility.CreateSampleItems<T>(10);
            var state = new BufferSegmentState<T>(items, offset: 3, size: 5, batchSize: 2);

            // Act & Assert: Should peek start at 3
            Assert.IsTrue(state.TryPeekNext(ref state, out var seg));
            Assert.AreEqual(3, seg.Start);
            Assert.AreEqual(2, seg.Count);
        }

        #endregion

        #region HasNext BatchSize Tests

        [Test]
        public void HasNext_ExactlyDivisibleBatches_ReturnsTrueUntilEnd()
        {
            var items = TestUtility.CreateSampleItems<T>(4);
            var state = new BufferSegmentState<T>(items, 0, 4, batchSize: 2);

            Assert.IsTrue(state.HasNext(ref state), "Should have data initially.");
            state.MoveNext(ref state, out _); // Consumed 0-1
            Assert.IsTrue(state.HasNext(ref state), "Should have data after first batch.");
            state.MoveNext(ref state, out _); // Consumed 2-3
            Assert.IsFalse(state.HasNext(ref state), "Should be exhausted after exactly 2 batches.");
        }

        [Test]
        public void HasNext_WithRemainderBatch_ReturnsTrueUntilLastElementProcessed()
        {
            var items = TestUtility.CreateSampleItems<T>(5);
            var state = new BufferSegmentState<T>(items, 0, 5, batchSize: 2);

            state.MoveNext(ref state, out _);
            state.MoveNext(ref state, out _);

            Assert.IsTrue(state.HasNext(ref state), "Should still have 1 element remaining.");

            state.MoveNext(ref state, out _); // Consumed 4
            Assert.IsFalse(state.HasNext(ref state), "Should be exhausted after remainder batch.");
        }

        [Test]
        public void HasNext_BatchSizeLargerThanSize_ReturnsTrueOnlyOnce()
        {
            var items = TestUtility.CreateSampleItems<T>(3);
            var state = new BufferSegmentState<T>(items, 0, 3, batchSize: 10);

            Assert.IsTrue(state.HasNext(ref state));
            state.MoveNext(ref state, out _);
            Assert.IsFalse(state.HasNext(ref state), "Should be exhausted after single large batch.");
        }

        [Test]
        public void HasNext_OffsetStartingMidArray_CorrectlyTracksRemaining()
        {
            var items = TestUtility.CreateSampleItems<T>(10);
            var state = new BufferSegmentState<T>(items, offset: 8, size: 2, batchSize: 1);

            Assert.IsTrue(state.HasNext(ref state));
            state.MoveNext(ref state, out _); // Element 8
            Assert.IsTrue(state.HasNext(ref state));
            state.MoveNext(ref state, out _); // Element 9
            Assert.IsFalse(state.HasNext(ref state), "Should be exhausted after processing size limit.");
        }

        #endregion
    }
}
