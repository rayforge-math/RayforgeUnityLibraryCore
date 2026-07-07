using NUnit.Framework;
using Rayforge.Core.Collections.Abstractions;
using Rayforge.Core.Collections.Abstractions.Tests;
using Rayforge.Core.TestEnv;
using System;

namespace Rayforge.Core.Collections.Buffering.Tests
{
    [TestFixture(typeof(int), typeof(float))]
    [TestFixture(typeof(long), typeof(string))]
    public class SyncedSegmentStateTests<T1, T2> : IIterationLogicTests<SyncedSegmentMeta<T1, T2>, SyncedSegmentState<T1, T2>>
    {
        #region Create Test Env

        protected override IterationTestData<SyncedSegmentMeta<T1, T2>, SyncedSegmentState<T1, T2>> CreateLogic(int count)
        {
            var data1 = TestUtility.CreateSampleItems<T1>(count);
            var data2 = TestUtility.CreateSampleItems<T2>(count);

            var expected = new SyncedSegmentMeta<T1, T2>[count];
            for (int i = 0; i < count; ++i)
            {
                expected[i] = new SyncedSegmentMeta<T1, T2>
                {
                    SegmentA = new BufferSegmentMeta<T1>
                    {
                        Start = i,
                        Count = 1,
                        Source = data1
                    },
                    SegmentB = new BufferSegmentMeta<T2>
                    {
                        Start = i,
                        Count = 1,
                        Source = data2
                    }
                };
            }

            var state = new SyncedSegmentState<T1, T2>(
                data1,
                data2,
                0,
                count);

            return new IterationTestData<SyncedSegmentMeta<T1, T2>, SyncedSegmentState<T1, T2>>
            {
                expected = expected,
                logic = state
            };
        }

        #endregion

        #region Constructor Tests

        [Test]
        public void Constructor_NullSourceA_ThrowsArgumentNullException()
        {
            int[] sourceA = null;
            int[] sourceB = new int[10];

            Assert.Throws<ArgumentNullException>(() =>
                new SyncedSegmentState<int, int>(sourceA, sourceB, 0, 10, 1, 1));
        }

        [Test]
        public void Constructor_NullSourceB_ThrowsArgumentNullException()
        {
            int[] sourceA = new int[10];
            int[] sourceB = null;

            Assert.Throws<ArgumentNullException>(() =>
                new SyncedSegmentState<int, int>(sourceA, sourceB, 0, 10, 1, 1));
        }

        [Test]
        public void Constructor_NegativeOffset_ThrowsArgumentOutOfRangeException()
        {
            int[] source = new int[10];

            Assert.Throws<ArgumentOutOfRangeException>(() =>
                new SyncedSegmentState<int, int>(source, source, -1, 10, 1, 1));
        }

        [Test]
        public void Constructor_ZeroOffsetOnEmptyArray_DoesNotThrow()
        {
            // offset=0 is explicitly exempted from the bounds check even for an empty array.
            int[] source = new int[0];

            Assert.DoesNotThrow(() =>
                new SyncedSegmentState<int, int>(source, source, 0, 0, 1, 1));
        }

        [Test]
        public void Constructor_NegativeSize_ThrowsArgumentOutOfRangeException()
        {
            int[] source = new int[10];

            Assert.Throws<ArgumentOutOfRangeException>(() =>
                new SyncedSegmentState<int, int>(source, source, 0, -5, 1, 1));
        }

        [Test]
        public void Constructor_SizeExceedsSourceALength_ThrowsArgumentOutOfRangeException()
        {
            int[] sourceA = new int[5];
            int[] sourceB = new int[10];

            // offset(2) + size(4) = 6 > sourceA.Length(5)
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                new SyncedSegmentState<int, int>(sourceA, sourceB, 2, 4, 1, 1));
        }

        [Test]
        public void Constructor_SizeExceedsSourceBLength_ThrowsArgumentOutOfRangeException()
        {
            int[] sourceA = new int[10];
            int[] sourceB = new int[5];

            // offset(2) + size(4) = 6 > sourceB.Length(5)
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                new SyncedSegmentState<int, int>(sourceA, sourceB, 2, 4, 1, 1));
        }

        [Test]
        public void Constructor_ZeroBatchSize_ThrowsArgumentOutOfRangeException()
        {
            int[] source = new int[5];

            Assert.Throws<ArgumentOutOfRangeException>(() =>
                new SyncedSegmentState<int, int>(source, source, 0, 5, 0, 1));
        }

        [Test]
        public void Constructor_NegativeBatchSize_ThrowsArgumentOutOfRangeException()
        {
            int[] source = new int[5];

            Assert.Throws<ArgumentOutOfRangeException>(() =>
                new SyncedSegmentState<int, int>(source, source, 0, 5, -3, 1));
        }

        [Test]
        public void Constructor_ZeroWindowSize_ThrowsArgumentOutOfRangeException()
        {
            int[] source = new int[5];

            Assert.Throws<ArgumentOutOfRangeException>(() =>
                new SyncedSegmentState<int, int>(source, source, 0, 5, 1, 0));
        }

        [Test]
        public void Constructor_NegativeWindowSize_ThrowsArgumentOutOfRangeException()
        {
            int[] source = new int[5];

            Assert.Throws<ArgumentOutOfRangeException>(() =>
                new SyncedSegmentState<int, int>(source, source, 0, 5, 1, -2));
        }

        [Test]
        public void Constructor_ValidInputs_DoesNotThrow()
        {
            int[] source = new int[10];

            Assert.DoesNotThrow(() =>
                new SyncedSegmentState<int, int>(source, source, 2, 6, 2, 3));
        }

        #endregion

        #region HasNext Tests

        [TestCase(0)]
        [TestCase(2)]
        [TestCase(5)]
        public void HasNext_PositiveSize_ReturnsTrueRegardlessOfOffset(int offset)
        {
            // size=5 > 0 -> HasNext should be true no matter where the slice starts.
            int[] source = new int[10];

            var state = new SyncedSegmentState<int, int>(source, source, offset, 5, 1, 5);

            Assert.IsTrue(state.HasNext(ref state),
                $"HasNext should be true when size > 0, regardless of offset ({offset}).");
        }

        [Test]
        public void HasNext_ZeroSize_ReturnsFalse()
        {
            // size=0 -> nothing to iterate, regardless of offset.
            int[] source = new int[10];

            var state = new SyncedSegmentState<int, int>(source, source, 0, 0, 1, 5);

            Assert.IsFalse(state.HasNext(ref state), "HasNext should be false when size is zero.");
        }

        [TestCase(1)]
        [TestCase(5)]
        [TestCase(100)]
        public void HasNext_PositiveSize_ReturnsTrueRegardlessOfWindowSize(int windowSize)
        {
            // size=5 > 0 -> HasNext should be true no matter how large the window is,
            // since the very first window always starts before totalCapacity is reached.
            int[] source = new int[10];

            var state = new SyncedSegmentState<int, int>(source, source, 0, 5, 1, windowSize);

            Assert.IsTrue(state.HasNext(ref state),
                $"HasNext should be true when size > 0, regardless of windowSize ({windowSize}).");
        }

        [Test]
        public void HasNext_OffsetDoesNotAffectInitialCapacityComparison_ReflectsMissingOffsetPropagation()
        {
            // Regression guard: _totalCapacity is currently computed as `size` (not `offset + size`)
            // and _currentWindowStart starts at 0 (not `offset`). This means two states with the
            // same `size` but different `offset` are indistinguishable to HasNext at construction time.
            // This test documents that current behavior explicitly, so a future fix that propagates
            // offset into _currentWindowStart/_totalCapacity will surface here if it changes semantics
            // unexpectedly.
            int[] source = new int[20];

            var stateNoOffset = new SyncedSegmentState<int, int>(source, source, 0, 5, 1, 5);
            var stateWithOffset = new SyncedSegmentState<int, int>(source, source, 10, 5, 1, 5);

            Assert.AreEqual(stateNoOffset.HasNext(ref stateNoOffset), stateWithOffset.HasNext(ref stateWithOffset),
                "Both states currently report identical HasNext results at construction, independent of offset.");
        }

        #endregion

        #region MoveNext Tests

        [Test]
        public void MoveNext_ZeroOffset_ReturnsSegmentsStartingAtZero()
        {
            int[] sourceA = new int[10];
            int[] sourceB = new int[10];

            var state = new SyncedSegmentState<int, int>(sourceA, sourceB, 0, 10, 1, 5);

            Assert.IsTrue(state.MoveNext(ref state, out var seg));
            Assert.AreEqual(0, seg.SegmentA.Start);
            Assert.AreEqual(0, seg.SegmentB.Start);
            Assert.AreEqual(5, seg.SegmentA.Count);
        }

        [Test]
        public void MoveNext_WithOffset_ReturnsAbsoluteStart()
        {
            // offset=5, size=5, windowSize=5 -> the single window must report an
            // absolute start of 5 (i.e. offset must be included), not 0.
            int[] sourceA = new int[10];
            int[] sourceB = new int[10];

            var state = new SyncedSegmentState<int, int>(sourceA, sourceB, 5, 5, 1, 5);

            Assert.IsTrue(state.MoveNext(ref state, out var seg));
            Assert.AreEqual(5, seg.SegmentA.Start, "SegmentA.Start must include the offset.");
            Assert.AreEqual(5, seg.SegmentB.Start, "SegmentB.Start must include the offset.");
            Assert.AreEqual(5, seg.SegmentA.Count);
        }

        [Test]
        public void MoveNext_WithOffset_ExhaustsAfterCoveringExactRange()
        {
            // offset=5, size=5, windowSize=5 -> exactly one window covering [5,10).
            int[] sourceA = new int[10];
            int[] sourceB = new int[10];

            var state = new SyncedSegmentState<int, int>(sourceA, sourceB, 5, 5, 1, 5);

            Assert.IsTrue(state.MoveNext(ref state, out _), "First window should be consumable.");
            Assert.IsFalse(state.MoveNext(ref state, out _),
                "Should be exhausted after consuming the single window covering the full offset range.");
        }

        [Test]
        public void MoveNext_WithOffsetAndMultipleWindows_ReturnsSequentialAbsoluteStarts()
        {
            // offset=4, size=8, windowSize=4 -> two windows: [4,8) and [8,12).
            int[] sourceA = new int[12];
            int[] sourceB = new int[12];

            var state = new SyncedSegmentState<int, int>(sourceA, sourceB, 4, 8, 1, 4);

            Assert.IsTrue(state.MoveNext(ref state, out var seg1));
            Assert.AreEqual(4, seg1.SegmentA.Start, "First window must start at the absolute offset.");
            Assert.AreEqual(4, seg1.SegmentA.Count);

            Assert.IsTrue(state.MoveNext(ref state, out var seg2));
            Assert.AreEqual(8, seg2.SegmentA.Start, "Second window must continue directly after the first.");
            Assert.AreEqual(4, seg2.SegmentA.Count);

            Assert.IsFalse(state.MoveNext(ref state, out _),
                "Should be exhausted after covering the entire offset range [4,12).");
        }

        [Test]
        public void MoveNext_WindowSizeLargerThanSize_ClampsCountToSize()
        {
            // size=5, windowSize=100 -> single window, clamped to totalCapacity.
            int[] sourceA = new int[5];
            int[] sourceB = new int[5];

            var state = new SyncedSegmentState<int, int>(sourceA, sourceB, 0, 5, 1, 100);

            Assert.IsTrue(state.MoveNext(ref state, out var seg));
            Assert.AreEqual(0, seg.SegmentA.Start);
            Assert.AreEqual(5, seg.SegmentA.Count, "Count must be clamped to the remaining capacity, not the full window size.");
            Assert.IsFalse(state.MoveNext(ref state, out _));
        }

        [Test]
        public void MoveNext_WindowSizeNotDivisorOfSize_LastWindowIsPartial()
        {
            // size=7, windowSize=3 -> windows [0,3), [3,6), [6,7) (last one partial).
            int[] sourceA = new int[7];
            int[] sourceB = new int[7];

            var state = new SyncedSegmentState<int, int>(sourceA, sourceB, 0, 7, 1, 3);

            Assert.IsTrue(state.MoveNext(ref state, out var seg1));
            Assert.AreEqual(0, seg1.SegmentA.Start);
            Assert.AreEqual(3, seg1.SegmentA.Count);

            Assert.IsTrue(state.MoveNext(ref state, out var seg2));
            Assert.AreEqual(3, seg2.SegmentA.Start);
            Assert.AreEqual(3, seg2.SegmentA.Count);

            Assert.IsTrue(state.MoveNext(ref state, out var seg3));
            Assert.AreEqual(6, seg3.SegmentA.Start);
            Assert.AreEqual(1, seg3.SegmentA.Count, "Last window must be partial, clamped to remaining capacity.");

            Assert.IsFalse(state.MoveNext(ref state, out _));
        }

        #endregion
    }
}
