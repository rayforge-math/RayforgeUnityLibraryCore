using NUnit.Framework;
using Rayforge.Core.Collections.Abstractions;
using Rayforge.Core.Collections.Abstractions.Tests;
using Rayforge.Core.TestEnv;
using System;
using System.Collections;
using UnityEngine;

namespace Rayforge.Core.Collections.Buffering.Tests
{
    [TestFixture(typeof(int), typeof(float))]
    [TestFixture(typeof(long), typeof(string))]
    public class SyncedDirtySegmentStateTests<T1, T2> : IIterationLogicTests<SyncedSegmentMeta<T1, T2>, SyncedDirtySegmentState<T1, T2>>
    {
        #region Create Test Env

        protected override IterationTestData<SyncedSegmentMeta<T1, T2>, SyncedDirtySegmentState<T1, T2>> CreateLogic(int count)
        {
            var data1 = TestUtility.CreateSampleItems<T1>(count);
            var data2 = TestUtility.CreateSampleItems<T2>(count);

            var dirtyBits1 = new BitArray(count);
            var dirtyBits2 = new BitArray(count);
            for(int i = 0; i < count; ++i)
            {
                dirtyBits1[i] = true;
                dirtyBits2[i] = true;


            }

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

            var state = new SyncedDirtySegmentState<T1, T2>(
                data1, 
                data2, 
                dirtyBits1, 
                dirtyBits2, 
                0, 
                count);

            return new IterationTestData<SyncedSegmentMeta<T1, T2>, SyncedDirtySegmentState<T1, T2>>
            {
                expected = expected,
                logic = state
            };
        }

        #endregion

        #region Constructor Tests

        [Test]
        public void Constructor_WithValidInputs_InitializesCorrectly()
        {
            int[] sourceA = new int[10];
            int[] sourceB = new int[10];
            BitArray bitsA = new BitArray(10);
            BitArray bitsB = new BitArray(10);

            bitsA[0] = true;

            var state = new SyncedDirtySegmentState<int, int>(sourceA, sourceB, bitsA, bitsB, 0, 10);

            Assert.IsTrue(state.HasNext(ref state));
            Assert.IsTrue(state.TryPeekNext(ref state, out _));
        }

        [Test]
        public void Constructor_EmptyArraysWithOffsetZero_DoesNotThrow()
        {
            int[] sourceA = new int[0];
            int[] sourceB = new int[0];
            BitArray bitsA = new BitArray(0);
            BitArray bitsB = new BitArray(0);

            Assert.DoesNotThrow(() =>
                new SyncedDirtySegmentState<int, int>(sourceA, sourceB, bitsA, bitsB, 0, 0));
        }

        [Test]
        public void Constructor_NullSourceA_ThrowsArgumentNullException()
        {
            int[] sourceA = null;
            int[] sourceB = new int[1];
            BitArray bits = new BitArray(1);

            Assert.Throws<ArgumentNullException>(() =>
                new SyncedDirtySegmentState<int, int>(sourceA, sourceB, bits, bits, 0, 1));
        }

        [Test]
        public void Constructor_NullSourceB_ThrowsArgumentNullException()
        {
            int[] sourceA = new int[1];
            int[] sourceB = null;
            BitArray bits = new BitArray(1);

            Assert.Throws<ArgumentNullException>(() =>
                new SyncedDirtySegmentState<int, int>(sourceA, sourceB, bits, bits, 0, 1));
        }

        [Test]
        public void Constructor_NullBitsA_ThrowsArgumentNullException()
        {
            int[] source = new int[1];
            BitArray bits = new BitArray(1);

            Assert.Throws<ArgumentNullException>(() =>
                new SyncedDirtySegmentState<int, int>(source, source, null, bits, 0, 1));
        }

        [Test]
        public void Constructor_NullBitsB_ThrowsArgumentNullException()
        {
            int[] source = new int[1];
            BitArray bits = new BitArray(1);

            Assert.Throws<ArgumentNullException>(() =>
                new SyncedDirtySegmentState<int, int>(source, source, bits, null, 0, 1));
        }

        [Test]
        public void Constructor_BitArrayTooShort_ThrowsArgumentOutOfRangeException()
        {
            int[] source = new int[10];
            BitArray shortBits = new BitArray(9);
            BitArray validBits = new BitArray(10);

            Assert.Throws<ArgumentOutOfRangeException>(() =>
                new SyncedDirtySegmentState<int, int>(source, source, shortBits, validBits, 0, 10));
        }

        [Test]
        public void Constructor_NegativeOffset_ThrowsArgumentOutOfRangeException()
        {
            int[] source = new int[1];
            BitArray bits = new BitArray(1);

            Assert.Throws<ArgumentOutOfRangeException>(() =>
                new SyncedDirtySegmentState<int, int>(source, source, bits, bits, -1, 1));
        }

        [Test]
        public void Constructor_OffsetOutOfBounds_ThrowsArgumentOutOfRangeException()
        {
            int[] source = new int[1];
            BitArray bits = new BitArray(1);

            Assert.Throws<ArgumentOutOfRangeException>(() =>
                new SyncedDirtySegmentState<int, int>(source, source, bits, bits, 1, 0));
        }

        [Test]
        public void Constructor_NegativeSize_ThrowsArgumentOutOfRangeException()
        {
            int[] source = new int[1];
            BitArray bits = new BitArray(1);

            Assert.Throws<ArgumentOutOfRangeException>(() =>
                new SyncedDirtySegmentState<int, int>(source, source, bits, bits, 0, -5));
        }

        [Test]
        public void Constructor_SizeTooLarge_ThrowsArgumentOutOfRangeException()
        {
            int[] source = new int[5];
            BitArray bits = new BitArray(5);

            // Offset 2 + Size 4 = 6
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                new SyncedDirtySegmentState<int, int>(source, source, bits, bits, 2, 4));
        }

        [Test]
        public void Constructor_InvalidBatchSize_ThrowsArgumentOutOfRangeException()
        {
            int[] source = new int[5];
            BitArray bits = new BitArray(5);

            Assert.Throws<ArgumentOutOfRangeException>(() =>
                new SyncedDirtySegmentState<int, int>(source, source, bits, bits, 0, 5, 0, 1));
        }

        [Test]
        public void Constructor_InvalidBatchesPerWindow_ThrowsArgumentOutOfRangeException()
        {
            int[] source = new int[5];
            BitArray bits = new BitArray(5);

            Assert.Throws<ArgumentOutOfRangeException>(() =>
                new SyncedDirtySegmentState<int, int>(source, source, bits, bits, 0, 5, 1, 0));
        }

        [Test]
        public void Constructor_InvalidOffsetAlignment_ThrowsArgumentException()
        {
            int[] source = new int[10];
            BitArray bits = new BitArray(10);

            // BatchSize 3, Offset 5 -> 5 ist kein Vielfaches von 3
            Assert.Throws<ArgumentException>(() =>
                new SyncedDirtySegmentState<int, int>(source, source, bits, bits, 5, 5, 3, 1));
        }

        [Test]
        public void Constructor_InvalidSizeAlignment_ThrowsArgumentException()
        {
            int[] source = new int[10];
            BitArray bits = new BitArray(10);
            // Offset 0 ist ok, aber 7 ist kein Vielfaches von 3
            Assert.Throws<ArgumentException>(() =>
                new SyncedDirtySegmentState<int, int>(source, source, bits, bits, 0, 7, 3, 1));
        }

        [Test]
        public void Constructor_ValidAlignment_DoesNotThrow()
        {
            int[] source = new int[12];
            BitArray bits = new BitArray(12);
            // Offset 3, Size 6, Batch 3 -> Alles durch 3 teilbar
            Assert.DoesNotThrow(() =>
                new SyncedDirtySegmentState<int, int>(source, source, bits, bits, 3, 6, 3, 1));
        }

        #endregion

        #region HasNext Tests

        [Test]
        public void HasNext_DirtyBitInFirstWindow_ReturnsTrue()
        {
            // Setup: Dirty bit at index 0 (in first window of size 5)
            int[] source = new int[10];
            BitArray bits = new BitArray(10);
            bits[0] = true;

            var state = new SyncedDirtySegmentState<int, int>(source, source, bits, bits, 0, 10, 1, 5);

            Assert.IsTrue(state.HasNext(ref state), "Should be true as data exists in the first window.");
        }

        [Test]
        public void HasNext_DirtyBitInLaterWindow_ReturnsTrue()
        {
            // Setup: Dirty bit at index 7 (in second window of size 5)
            int[] source = new int[10];
            BitArray bits = new BitArray(10);
            bits[7] = true;

            var state = new SyncedDirtySegmentState<int, int>(source, source, bits, bits, 0, 10, 1, 5);

            // Even if the first window is empty, HasNext must be true because 
            // the scanner will find the data in the second window.
            Assert.IsTrue(state.HasNext(ref state), "Should be true as data exists in a later window.");
        }

        [Test]
        public void HasNext_NoDirtyBits_ReturnsFalse()
        {
            // Setup: No dirty bits set.
            int[] source = new int[10];
            BitArray bits = new BitArray(10);

            var state = new SyncedDirtySegmentState<int, int>(source, source, bits, bits, 0, 10, 1, 5);

            Assert.IsFalse(state.HasNext(ref state), "Should be false when no dirty data exists in any window.");
        }

        [TestCase(5)]
        [TestCase(6)]
        [TestCase(7)]
        [TestCase(8)]
        [TestCase(9)]
        public void HasNext_DirtyBitWithinOffsetRange_ReturnsTrue(int dirtyBitIndex)
        {
            // Setup: Offset 5, size 5 -> valid range is [5, 10).
            // Every index in {5,6,7,8,9} lies within this range and must be detected.
            int[] source = new int[10];
            BitArray bits = new BitArray(10);
            bits[dirtyBitIndex] = true;

            var state = new SyncedDirtySegmentState<int, int>(source, source, bits, bits, 5, 5, 1, 5);

            Assert.IsTrue(state.HasNext(ref state),
                $"Should be true as dirty bit at index {dirtyBitIndex} lies within the offset range [5,10).");
        }

        [Test]
        public void HasNext_DirtyBitAtEndOfCapacity_ReturnsTrue()
        {
            // Setup: Last index is dirty.
            int[] source = new int[10];
            BitArray bits = new BitArray(10);
            bits[9] = true;

            var state = new SyncedDirtySegmentState<int, int>(source, source, bits, bits, 0, 10, 1, 5);

            Assert.IsTrue(state.HasNext(ref state), "Should be true as data exists at the very end of capacity.");
        }

        [Test]
        public void HasNext_DirtyBitJustBeforeOffset_IsIgnored()
        {
            // Setup: Offset 5, size 5 -> valid range is [5, 10).
            // Dirty bit at index 4 lies just before the offset and must not be detected.
            int[] source = new int[10];
            BitArray bits = new BitArray(10);
            bits[4] = true;

            var state = new SyncedDirtySegmentState<int, int>(source, source, bits, bits, 5, 5, 1, 5);

            Assert.IsFalse(state.HasNext(ref state),
                "Dirty bit just before the offset must be ignored, not treated as within range.");
        }

        #endregion

        #region TryPeekNext Tests

        [Test]
        public void TryPeekNext_DirtyBitInFirstWindow_ReturnsTrue()
        {
            // Setup: Dirty bit at index 0 (in first window of size 5)
            int[] source = new int[10];
            BitArray bits = new BitArray(10);
            bits[0] = true;

            var state = new SyncedDirtySegmentState<int, int>(source, source, bits, bits, 0, 10, 1, 5);

            Assert.IsTrue(state.TryPeekNext(ref state, out _), "Should be true as data exists in the first window.");
        }

        [Test]
        public void TryPeekNext_DirtyBitInLaterWindow_ReturnsTrue()
        {
            // Setup: Dirty bit at index 7 (in second window of size 5)
            int[] source = new int[10];
            BitArray bits = new BitArray(10);
            bits[7] = true;

            var state = new SyncedDirtySegmentState<int, int>(source, source, bits, bits, 0, 10, 1, 5);

            // Even if the first window is empty, TryPeekNext must be true because
            // the scanner will find the data in the second window.
            Assert.IsTrue(state.TryPeekNext(ref state, out _), "Should be true as data exists in a later window.");
        }

        [Test]
        public void TryPeekNext_NoDirtyBits_ReturnsFalse()
        {
            // Setup: No dirty bits set.
            int[] source = new int[10];
            BitArray bits = new BitArray(10);

            var state = new SyncedDirtySegmentState<int, int>(source, source, bits, bits, 0, 10, 1, 5);

            Assert.IsFalse(state.TryPeekNext(ref state, out _), "Should be false when no dirty data exists in any window.");
        }

        [TestCase(5)]
        [TestCase(6)]
        [TestCase(7)]
        [TestCase(8)]
        [TestCase(9)]
        public void TryPeekNext_DirtyBitWithinOffsetRange_ReturnsTrue(int dirtyBitIndex)
        {
            // Setup: Offset 5, size 5 -> valid range is [5, 10).
            // Every index in {5,6,7,8,9} lies within this range and must be detected.
            int[] source = new int[10];
            BitArray bits = new BitArray(10);
            bits[dirtyBitIndex] = true;

            var state = new SyncedDirtySegmentState<int, int>(source, source, bits, bits, 5, 5, 1, 5);

            Assert.IsTrue(state.TryPeekNext(ref state, out _),
                $"Should be true as dirty bit at index {dirtyBitIndex} lies within the offset range [5,10).");
        }

        [Test]
        public void TryPeekNext_DirtyBitJustBeforeOffset_IsIgnored()
        {
            // Setup: Offset 5, size 5 -> valid range is [5, 10).
            // Dirty bit at index 4 lies just before the offset and must not be peekable.
            int[] source = new int[10];
            BitArray bits = new BitArray(10);
            bits[4] = true;

            var state = new SyncedDirtySegmentState<int, int>(source, source, bits, bits, 5, 5, 1, 5);

            Assert.IsFalse(state.TryPeekNext(ref state, out _),
                "Dirty bit just before the offset must be ignored, not treated as within range.");
        }

        #endregion

        #region MoveNext Tests

        [Test]
        public void MoveNext_DirtyBitInFirstWindow_ReturnsTrue()
        {
            // Setup: Dirty bit at index 0 (in first window of size 5)
            int[] source = new int[10];
            BitArray bits = new BitArray(10);
            bits[0] = true;

            var state = new SyncedDirtySegmentState<int, int>(source, source, bits, bits, 0, 10, 1, 5);

            Assert.IsTrue(state.MoveNext(ref state, out _), "Should be true as data exists in the first window.");
        }

        [Test]
        public void MoveNext_DirtyBitInLaterWindow_ReturnsTrue()
        {
            // Setup: Dirty bit at index 7 (in second window of size 5)
            int[] source = new int[10];
            BitArray bits = new BitArray(10);
            bits[7] = true;

            var state = new SyncedDirtySegmentState<int, int>(source, source, bits, bits, 0, 10, 1, 5);

            // Even if the first window is empty, MoveNext must be true because
            // the scanner will find the data in the second window.
            Assert.IsTrue(state.MoveNext(ref state, out _), "Should be true as data exists in a later window.");
        }

        [Test]
        public void MoveNext_NoDirtyBits_ReturnsFalse()
        {
            // Setup: No dirty bits set.
            int[] source = new int[10];
            BitArray bits = new BitArray(10);

            var state = new SyncedDirtySegmentState<int, int>(source, source, bits, bits, 0, 10, 1, 5);

            Assert.IsFalse(state.MoveNext(ref state, out _), "Should be false when no dirty data exists in any window.");
        }

        [TestCase(5)]
        [TestCase(6)]
        [TestCase(7)]
        [TestCase(8)]
        [TestCase(9)]
        public void MoveNext_DirtyBitWithinOffsetRange_ReturnsTrue(int dirtyBitIndex)
        {
            // Setup: Offset 5, size 5 -> valid range is [5, 10).
            // Every index in {5,6,7,8,9} lies within this range and must be detected.
            int[] source = new int[10];
            BitArray bits = new BitArray(10);
            bits[dirtyBitIndex] = true;

            var state = new SyncedDirtySegmentState<int, int>(source, source, bits, bits, 5, 5, 1, 5);

            Assert.IsTrue(state.MoveNext(ref state, out _),
                $"Should be true as dirty bit at index {dirtyBitIndex} lies within the offset range [5,10).");
        }

        [Test]
        public void MoveNext_DirtyBitJustBeforeOffset_IsIgnored()
        {
            // Setup: Offset 5, size 5 -> valid range is [5, 10).
            // Dirty bit at index 4 lies just before the offset and must not be consumable.
            int[] source = new int[10];
            BitArray bits = new BitArray(10);
            bits[4] = true;

            var state = new SyncedDirtySegmentState<int, int>(source, source, bits, bits, 5, 5, 1, 5);

            Assert.IsFalse(state.MoveNext(ref state, out _),
                "Dirty bit just before the offset must be ignored, not treated as within range.");
        }

        #endregion
    }
}
