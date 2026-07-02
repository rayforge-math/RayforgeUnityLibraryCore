using NUnit.Framework;
using Rayforge.Core.Collections.Abstractions.Tests;
using System;
using System.Collections;
using System.Collections.Generic;

namespace Rayforge.Core.Collections.Iterator.Tests
{
    public class BitIteratorStateTests : IIterationLogicTests<int, BitIteratorState>
    {
        #region IIterationLogic Impl

        protected override IterationTestData<int, BitIteratorState> CreateLogic(int count)
        {
            if (count <= 0)
            {
                return new IterationTestData<int, BitIteratorState>
                {
                    logic = new BitIteratorState(new BitArray(0), 0, 0),
                    expected = Array.Empty<int>()
                };
            }

            int bitArrayLength = count * 2 + 2;
            var bits = new BitArray(bitArrayLength);
            var expectedList = new List<int>(count);

            int found = 0;
            for (int i = 0; i < bitArrayLength && found < count; i++)
            {
                bool isHit = (count + i) % 2 == 0;

                if (isHit)
                {
                    bits.Set(i, true);
                    expectedList.Add(i);
                    found++;
                }
            }

            var logic = new BitIteratorState(bits, 0, bitArrayLength, targetState: true);
            return new IterationTestData<int, BitIteratorState>
            {
                logic = logic,
                expected = expectedList.ToArray()
            };
        }

        #endregion

        #region Constructor Tests

        [Test]
        public void Constructor_HandlesNullAndEmptyBitArrays_Gracefully()
        {
            // Scenario 1: Null BitArray should be treated as empty
            var nullState = new BitIteratorState(null, 0, 10);
            bool nullMoveNext = nullState.MoveNext(ref nullState, out _);
            Assert.IsFalse(nullMoveNext, "Iterator with null BitArray should not yield any elements.");

            // Scenario 2: Empty BitArray should be treated as empty
            var emptyBits = new BitArray(0);
            var emptyState = new BitIteratorState(emptyBits, 0, 10);
            bool emptyMoveNext = emptyState.MoveNext(ref emptyState, out _);
            Assert.IsFalse(emptyMoveNext, "Iterator with empty BitArray should not yield any elements.");
        }

        [Test]
        [TestCase(-10, 5, 5, Description = "Start < 0 -> clamped to 0")]
        [TestCase(100, 5, 0, Description = "Start > Length -> clamped to Length")]
        [TestCase(10, 0, 0, Description = "Start exactly at Length -> empty iteration")]
        [TestCase(0, 5, 5, Description = "Start at 0 -> full range (5 elements)")]
        [TestCase(5, 5, 5, Description = "Start at middle -> remainder range (5 elements)")]
        [TestCase(8, 5, 2, Description = "Start near end -> partial range (2 elements)")]
        [TestCase(int.MinValue, 5, 5, Description = "Extreme negative start -> clamped to 0")]
        [TestCase(int.MaxValue, 5, 0, Description = "Extreme positive start -> clamped to length")]
        public void Constructor_ClampsStartBoundary(int start, int count, int expectedCount)
        {
            // Arrange: BitArray length is 10
            var bits = new BitArray(10);
            // Set all to true to ensure we are only testing the boundary/clamping logic
            bits.SetAll(true);

            var state = new BitIteratorState(bits, start, count, targetState: true);

            // Act: Consume the iterator to verify the actual accessible range
            int consumed = 0;
            while (state.MoveNext(ref state, out _))
            {
                consumed++;
            }

            // Assert: Verify the count matches the expected clamped range size
            Assert.AreEqual(expectedCount, consumed,
                $"Boundary test failed for start {start} and count {count}. Expected {expectedCount} elements but got {consumed}.");
        }

        [Test]
        [TestCase(0, -5, 0, Description = "Count < 0 -> clamped to 0")]
        [TestCase(0, 0, 0, Description = "Count 0 -> empty iteration")]
        [TestCase(0, 100, 10, Description = "Count > Length -> clamped to full length")]
        [TestCase(5, 10, 5, Description = "Count > remaining space -> clamped to remaining")]
        [TestCase(10, 5, 0, Description = "Start at boundary -> empty iteration")]
        [TestCase(11, 5, 0, Description = "Start beyond length -> empty iteration")]
        [TestCase(-5, 5, 5, Description = "Negative start -> clamped to 0")]
        public void Constructor_ClampsCountBoundary(int start, int count, int expectedCount)
        {
            // Arrange: BitArray length is 10
            var bits = new BitArray(10);
            // Set all bits to true so we can count them easily
            bits.SetAll(true);

            var state = new BitIteratorState(bits, start, count, targetState: true);

            // Act: Count how many elements the iterator traverses
            int consumed = 0;
            while (state.MoveNext(ref state, out _))
            {
                consumed++;
            }

            // Assert
            Assert.AreEqual(expectedCount, consumed,
                $"Failed for range [{start}, count: {count}]. Expected {expectedCount} items, but got {consumed}.");
        }

        [Test]
        [TestCase(int.MinValue, 10, 10, Description = "Extreme negative start -> clamped to 0")]
        [TestCase(0, int.MaxValue, 10, Description = "Extreme count -> clamped to length")]
        [TestCase(int.MaxValue, int.MaxValue, 0, Description = "Extreme values -> empty range")]
        public void Constructor_HandlesExtremeBoundaries(int start, int count, int expectedCount)
        {
            // Arrange: BitArray length is 10
            var bits = new BitArray(10);
            bits.SetAll(true);

            var state = new BitIteratorState(bits, start, count, targetState: true);

            // Act
            int consumed = 0;
            while (state.MoveNext(ref state, out _))
            {
                consumed++;
            }

            // Assert: Ensure no overflows or crashes occur with extreme inputs
            Assert.AreEqual(expectedCount, consumed,
                $"Extreme input failed for [{start}, count: {count}].");
        }

        [Test]
        [TestCase(-50, -50, 0, Description = "Negative offset and count -> clamped to empty")]
        [TestCase(0, -50, 0, Description = "Zero offset, negative count -> clamped to empty")]
        [TestCase(100, 100, 0, Description = "Offset/count way beyond length -> clamped to empty")]
        [TestCase(int.MinValue, int.MaxValue, 10, Description = "Overflow scenario: MinValue start + MaxValue count -> clamped to full range")]
        [TestCase(int.MinValue, 5, 5, Description = "Negative start, small count -> clamped to [0, 5)")]
        [TestCase(5, int.MaxValue, 5, Description = "Valid start, huge count -> clamped to remaining")]
        public void Constructor_HandlesNonsensicalCombinations(int start, int count, int expectedCount)
        {
            // Arrange: BitArray length is 10
            var bits = new BitArray(10);
            // Set all to true to count the iterations easily
            bits.SetAll(true);

            var state = new BitIteratorState(bits, start, count, targetState: true);

            // Act: Count how many elements the iterator traverses
            int consumed = 0;
            while (state.MoveNext(ref state, out _))
            {
                consumed++;
            }

            // Assert: Verify that the state remains stable and bounded
            Assert.AreEqual(expectedCount, consumed,
                $"Extreme input [{start}, {count}] failed. Expected {expectedCount} elements but got {consumed}.");
        }

        #endregion

        #region HasNext Tests

        [Test]
        public void HasNext_WithDefaultStruct_ReturnsFalse_InsteadOfThrowing()
        {
            // default(BitIteratorState) hat ein null-BitArray
            var state = default(BitIteratorState);

            // Act & Assert
            Assert.DoesNotThrow(() => {
                bool result = state.HasNext(ref state);
                Assert.IsFalse(result, "HasNext must return false for uninitialized state.");
            });
        }

        #endregion

        #region TryPeekNext Tests

        [Test]
        public void TryPeekNext_WithDefaultStruct_ReturnsFalse_InsteadOfThrowing()
        {
            var state = default(BitIteratorState);

            // Act & Assert
            Assert.DoesNotThrow(() => {
                bool success = state.TryPeekNext(ref state, out int result);
                Assert.IsFalse(success);
                Assert.AreEqual(0, result);
            });
        }

        #endregion

        #region HasNext Tests

        [Test]
        public void HasNext_DefaultStruct_ReturnsFalse_InsteadOfThrowing()
        {
            // Arrange: default(BitIteratorState) hat null-Referenzen (BitArray)
            var state = default(BitIteratorState);

            // Act & Assert: Muss sicher false zurückgeben, statt abzustürzen
            Assert.DoesNotThrow(() => {
                bool hasNext = state.HasNext(ref state);
                Assert.IsFalse(hasNext);
            });
        }

        #endregion

        #region MoveNext Tests

        [Test]
        public void MoveNext_WithDefaultStruct_ReturnsFalse_InsteadOfThrowing()
        {
            var state = default(BitIteratorState);

            // Act & Assert
            Assert.DoesNotThrow(() => {
                bool success = state.MoveNext(ref state, out int result);
                Assert.IsFalse(success);
                Assert.AreEqual(0, result);
            });
        }

        #endregion

        #region MoveBeforeNext

        [Test]
        [TestCase(10, 3, Description = "Hit at 3: Should jump to 3")]
        [TestCase(10, 0, Description = "Hit at 0: Should jump to 0")]
        [TestCase(5, 4, Description = "Hit at end: Should jump to 4")]
        [TestCase(20, 19, Description = "Large array: Should jump to 19")]
        public void MoveBeforeNext_FastForwards_ToCorrectPosition(int length, int hitAt)
        {
            // Arrange: Set a bit at the target position
            var bits = new BitArray(length);
            bits.Set(hitAt, true);
            var state = new BitIteratorState(bits, 0, length, targetState: true);

            // Act: MoveNext internally triggers the fast-forward logic
            bool found = state.MoveNext(ref state, out int val);

            // Assert: Verify that it correctly landed on the bit we set
            Assert.IsTrue(found, "Iterator should have found the bit.");
            Assert.AreEqual(hitAt, val, $"Failed to fast-forward to bit at {hitAt}.");
        }

        [Test]
        [TestCase(10, 0, 5, Description = "Range [0,5), no hits -> Should be exhausted")]
        [TestCase(1, 0, 1, Description = "Single bit array, no hit -> Should be exhausted")]
        [TestCase(0, 0, 0, Description = "Empty array -> Should be exhausted")]
        public void MoveBeforeNext_Exhaustion_HandlesRangesCorrectly(int arrayLen, int start, int count)
        {
            // Arrange
            var bits = new BitArray(arrayLen);
            bits.SetAll(false);
            var state = new BitIteratorState(bits, start, count, targetState: true);

            // Act
            bool found = state.MoveNext(ref state, out _);

            // Assert: If no bits are set in the range, MoveNext must return false
            Assert.IsFalse(found, "Iterator should be exhausted as no matching bits were found.");
        }

        [Test]
        public void MoveBeforeNext_WithNullArray_IsExceptionSafe()
        {
            // Arrange: Null BitArray
            var state = new BitIteratorState(null, 0, 10);

            // Act & Assert
            Assert.DoesNotThrow(() => {
                bool found = state.MoveNext(ref state, out _);
                Assert.IsFalse(found, "Null BitArray must be treated as exhausted.");
            }, "MoveNext must handle null _bits safely without throwing.");
        }

        #endregion
    }
}
