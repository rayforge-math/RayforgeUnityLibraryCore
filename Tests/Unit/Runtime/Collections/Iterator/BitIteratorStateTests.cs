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
        public void Constructor_NullBits_IsExhaustedImmediately()
        { 
            var state = new BitIteratorState(null!, 0, 10);
            Assert.IsFalse(state.HasNext(ref state), "Iterator must be exhausted when bits is null.");
        }

        [Test]
        public void Constructor_NegativeStartIndex_ThrowsArgumentOutOfRangeException()
        {
            var bits = new BitArray(10);
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                new BitIteratorState(bits, -1, 5));
        }

        [Test]
        public void Constructor_StartIndexExceedsLength_ThrowsArgumentOutOfRangeException()
        {
            var bits = new BitArray(10);
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                new BitIteratorState(bits, 10, 1));
        }

        [Test]
        public void Constructor_CountZero_IsExhaustedImmediately()
        {
            var bits = new BitArray(10);
            var state = new BitIteratorState(bits, 0, 0);

            Assert.IsFalse(state.HasNext(ref state), "Iterator with count 0 must be exhausted immediately.");
        }

        [Test]
        public void Constructor_NegativeCount_ThrowsArgumentOutOfRangeException()
        {
            var bits = new BitArray(10);
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                new BitIteratorState(bits, 0, -1));
        }

        [Test]
        public void Constructor_RangeExceedsLength_ThrowsArgumentOutOfRangeException()
        {
            var bits = new BitArray(10);
            // 5 + 6 = 11 > 10
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                new BitIteratorState(bits, 5, 6));
        }

        [Test]
        public void Constructor_ValidInputs_InitializesCorrectly()
        {
            var bits = new BitArray(10);
            Assert.DoesNotThrow(() => new BitIteratorState(bits, 0, 5));
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
