using NUnit.Framework;
using Rayforge.Core.Collections.Abstractions;
using Rayforge.Core.Collections.Abstractions.Tests;
using System;
using System.Collections;

namespace Rayforge.Core.Collections.Iterator.Tests
{
    public class SyncedBitIteratorStateTests : IIterationLogicTests<SyncedBitIteratorMeta, SyncedBitIteratorState>
    {
        #region Create Test Env

        protected override IterationTestData<SyncedBitIteratorMeta, SyncedBitIteratorState> CreateLogic(int count)
        {
            var bits1 = new BitArray(count);
            var bits2 = new BitArray(count);

            var expected = new SyncedBitIteratorMeta[count];
            for (int i = 0; i < count; ++i)
            {
                bits1[i] = true;

                expected[i] = new SyncedBitIteratorMeta
                {
                    Index = i,
                    BitA = true,
                    BitB = false
                };
            }

            var state = new SyncedBitIteratorState(
                bits1,
                bits2,
                0,
                count);

            return new IterationTestData<SyncedBitIteratorMeta, SyncedBitIteratorState>
            {
                expected = expected,
                logic = state
            };
        }

        #endregion

        #region Constructor Tests

        [Test]
        public void Constructor_ValidInputs_InitializesCorrectly()
        {
            var bitsA = new BitArray(10);
            var bitsB = new BitArray(10);

            // Ensure valid parameters do not throw and initialize correctly
            Assert.DoesNotThrow(() => new SyncedBitIteratorState(bitsA, bitsB, 0, 5));
        }

        [Test]
        public void Constructor_NullBits_IsExhaustedImmediately()
        {
            var bitsA = new BitArray(10);

            // Graceful handling: Null inputs should mark the state as exhausted instead of throwing
            SyncedBitIteratorState state = default;
            Assert.DoesNotThrow(() => state = new SyncedBitIteratorState(null!, bitsA, 0, 5));
            Assert.IsFalse(state.HasNext(ref state), "Synced state must be exhausted if one input is null.");
        }

        [Test]
        public void Constructor_CountZero_IsExhaustedImmediately()
        {
            var bitsA = new BitArray(10);
            var bitsB = new BitArray(10);

            // Count 0 is considered a valid but empty range
            var state = new SyncedBitIteratorState(bitsA, bitsB, 0, 0);
            Assert.IsFalse(state.HasNext(ref state), "Synced state must be exhausted if count is 0.");
        }

        [Test]
        public void Constructor_NegativeCount_ThrowsArgumentOutOfRangeException()
        {
            var bitsA = new BitArray(10);
            var bitsB = new BitArray(10);

            // Negative count is a logical error, expect an exception
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                new SyncedBitIteratorState(bitsA, bitsB, 0, -1), "Negative count must throw.");
        }

        [Test]
        public void Constructor_InvalidStartIndex_ThrowsArgumentOutOfRangeException()
        {
            var bitsA = new BitArray(10);
            var bitsB = new BitArray(10);

            // Start index must be within array bounds
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                new SyncedBitIteratorState(bitsA, bitsB, 10, 5));
        }

        [Test]
        public void Constructor_RangeExceedsLength_ThrowsArgumentOutOfRangeException()
        {
            var bitsA = new BitArray(10);
            var bitsB = new BitArray(10);

            // The requested range cannot exceed the underlying BitArray length
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                new SyncedBitIteratorState(bitsA, bitsB, 5, 6));
        }

        #endregion

        #region MoveNext Tests

        [Test]
        public void MoveNext_AlternatingAndAllFalse_ReturnsCorrectState()
        {
            // Arrange: 
            // BitsA: [T, F, T, F, T, F, T, F, T, F] (Matches at 0, 2, 4, 6, 8)
            // BitsB: [F, F, F, F, F, F, F, F, F, F] (No matches)
            // Result should be 0, 2, 4, 6, 8 (from BitsA)
            var bitsA = new BitArray(10);
            var bitsB = new BitArray(10);

            for (int i = 0; i < 10; i++)
            {
                bitsA[i] = (i % 2 == 0);
                bitsB[i] = false;
            }

            var state = new SyncedBitIteratorState(bitsA, bitsB, 0, 10, targetState: true);

            // Act: Iterate through and collect indices
            int[] expectedIndices = { 0, 2, 4, 6, 8 };
            int count = 0;

            while (state.MoveNext(ref state, out var result))
            {
                Assert.IsTrue(result.BitA);
                Assert.IsFalse(result.BitB);
                Assert.AreEqual(expectedIndices[count], result.Index, $"Mismatch at iteration {count}.");
                count++;
            }

            // Assert: Verify all 5 matches from BitsA were found
            Assert.AreEqual(expectedIndices.Length, count, "Iterator should return matches found in either array.");
        }

        [Test]
        public void MoveNext_AllFalseAndAlternating_ReturnsCorrectState()
        {
            // Arrange: 
            // BitsA: [F, F, F, F, F, F, F, F, F, F] (No matches)
            // BitsB: [T, F, T, F, T, F, T, F, T, F] (Matches at 0, 2, 4, 6, 8)
            var bitsA = new BitArray(10);
            var bitsB = new BitArray(10);

            for (int i = 0; i < 10; i++)
            {
                bitsA[i] = false;
                bitsB[i] = (i % 2 == 0);
            }

            var state = new SyncedBitIteratorState(bitsA, bitsB, 0, 10, targetState: true);

            // Act: Iterate through and collect indices
            int[] expectedIndices = { 0, 2, 4, 6, 8 };
            int count = 0;

            // We assume the result of MoveNext provides context on which source matched
            while (state.MoveNext(ref state, out var result))
            {
                // Assert: Verify the source mapping is swapped
                Assert.IsFalse(result.BitA, "BitA should be false in this test case.");
                Assert.IsTrue(result.BitB, "BitB should be true in this test case.");
                Assert.AreEqual(expectedIndices[count], result.Index, $"Mismatch at iteration {count}.");
                count++;
            }

            // Assert: Verify all 5 matches from BitsB were found
            Assert.AreEqual(expectedIndices.Length, count, "Iterator should return matches found in either array.");
        }

        #endregion

        #region TryPeekNext Tests

        [Test]
        public void TryPeekNext_AlternatingAndAllFalse_ReturnsCorrectState()
        {
            // Arrange: BitsA alternating, BitsB all false
            var bitsA = new BitArray(10);
            var bitsB = new BitArray(10);
            for (int i = 0; i < 10; i++)
            {
                bitsA[i] = (i % 2 == 0);
                bitsB[i] = false;
            }

            var state = new SyncedBitIteratorState(bitsA, bitsB, 0, 10, targetState: true);

            // Act & Assert: Peek should return the first match (0) without consuming it
            Assert.IsTrue(state.TryPeekNext(ref state, out var firstPeek));
            Assert.IsTrue(firstPeek.BitA);
            Assert.IsFalse(firstPeek.BitB);
            Assert.AreEqual(0, firstPeek.Index);

            // Verify that MoveNext still returns the same first element
            Assert.IsTrue(state.MoveNext(ref state, out var firstMove));
            Assert.AreEqual(firstPeek.Index, firstMove.Index);
        }

        [Test]
        public void TryPeekNext_AllFalseAndAlternating_ReturnsCorrectState()
        {
            // Arrange: BitsA all false, BitsB alternating
            var bitsA = new BitArray(10);
            var bitsB = new BitArray(10);
            for (int i = 0; i < 10; i++)
            {
                bitsA[i] = false;
                bitsB[i] = (i % 2 == 0);
            }

            var state = new SyncedBitIteratorState(bitsA, bitsB, 0, 10, targetState: true);

            // Act & Assert: Verify look-ahead symmetry
            Assert.IsTrue(state.TryPeekNext(ref state, out var firstPeek));
            Assert.IsFalse(firstPeek.BitA);
            Assert.IsTrue(firstPeek.BitB);
            Assert.AreEqual(0, firstPeek.Index);

            // Ensure state is not advanced
            Assert.IsTrue(state.TryPeekNext(ref state, out var secondPeek));
            Assert.AreEqual(firstPeek.Index, secondPeek.Index, "TryPeekNext should not advance the iterator.");
        }

        #endregion

        #region HasNext Tests

        [Test]
        public void HasNext_AlternatingAndAllFalse_ReturnsTrueAsLongAsMatchesExist()
        {
            // Arrange: BitsA alternating, BitsB all false
            var bitsA = new BitArray(10);
            var bitsB = new BitArray(10);
            for (int i = 0; i < 10; i++)
            {
                bitsA[i] = (i % 2 == 0); // Matches at 0, 2, 4, 6, 8
                bitsB[i] = false;
            }

            var state = new SyncedBitIteratorState(bitsA, bitsB, 0, 10, targetState: true);

            // Act & Assert: HasNext should be true initially
            Assert.IsTrue(state.HasNext(ref state), "HasNext should be true when matches exist.");

            // Consume all matches
            while (state.MoveNext(ref state, out _)) { }

            // Assert: Exhausted
            Assert.IsFalse(state.HasNext(ref state), "HasNext should be false after all matches are consumed.");
        }

        [Test]
        public void HasNext_AllFalseAndAlternating_ReflectsAvailableMatches()
        {
            // Arrange: BitsA all false, BitsB alternating
            var bitsA = new BitArray(10);
            var bitsB = new BitArray(10);
            for (int i = 0; i < 10; i++)
            {
                bitsA[i] = false;
                bitsB[i] = (i % 2 == 0);
            }

            var state = new SyncedBitIteratorState(bitsA, bitsB, 0, 10, targetState: true);

            // Act & Assert: Should be true as long as BitsB provides matches
            int count = 0;
            while (state.HasNext(ref state))
            {
                Assert.IsTrue(state.MoveNext(ref state, out _), "MoveNext should succeed if HasNext is true.");
                count++;
            }

            Assert.AreEqual(5, count, "Iterator should have found 5 matches in BitsB.");
            Assert.IsFalse(state.HasNext(ref state), "HasNext should return false after exhaustion.");
        }

        #endregion
    }
}
