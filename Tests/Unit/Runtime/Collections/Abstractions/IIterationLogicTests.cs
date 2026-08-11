using NUnit.Framework;
using Rayforge.Core.TestEnv;

namespace Rayforge.Core.Collections.Abstractions.Tests
{
    public abstract class IIterationLogicTests<T, TLogic>
        where TLogic : struct, IIterationLogic<T, TLogic>
    {
        #region Create Test Env

        protected abstract IterationTestData<T, TLogic> CreateLogic(int count);

        public static IterationTestData<T, MockLogic<T>> CreateDefaultMockLogic(int count)
        {
            T[] items = TestUtility.CreateSampleItems<T>(count);

            var logic = new MockLogic<T>
            {
                Items = items,
                Index = 0
            };

            return new IterationTestData<T, MockLogic<T>>
            {
                logic = logic,
                expected = items
            };
        }

        #endregion

        #region HasNext Contract Tests

        [Test]
        [TestCase(0)]
        [TestCase(1)]
        [TestCase(5)]
        [TestCase(15)]
        [TestCase(20)]
        public void HasNext_InitialState_MatchesExpected(int count)
        {
            // Arrange: Create a fresh logic state
            var data = CreateLogic(count);
            var logic = data.logic;

            // Act: Check the initial availability
            bool result = logic.HasNext(ref logic);

            // Assert: Must be true if count > 0, false otherwise
            if (count > 0)
            {
                Assert.IsTrue(result, $"HasNext must be true for an initial state with {count} elements.");
            }
            else
            {
                Assert.IsFalse(result, "HasNext must be false for an initial empty state.");
            }
        }

        [Test]
        [TestCase(0)]
        [TestCase(1)]
        [TestCase(5)]
        [TestCase(15)]
        [TestCase(20)]
        public void HasNext_IsIdempotent(int count)
        {
            // Arrange: Use a state that definitely has elements
            var data = CreateLogic(count);
            var logic = data.logic;

            // Act: Call HasNext multiple times in succession
            bool firstCall = logic.HasNext(ref logic);
            bool secondCall = logic.HasNext(ref logic);
            bool thirdCall = logic.HasNext(ref logic);

            // Assert: The result must remain consistent and the state must not advance
            bool valid = count > 0;
            Assert.AreEqual(firstCall, valid, "First call to HasNext failed.");
            Assert.AreEqual(secondCall, valid, "Second call to HasNext must still be true (idempotency check).");
            Assert.AreEqual(thirdCall, valid, "Third call to HasNext must still be true (idempotency check).");
        }

        #endregion

        #region MoveNext Contract Tests

        [Test]
        [TestCase(0)]
        [TestCase(1)]
        [TestCase(5)]
        [TestCase(15)]
        [TestCase(20)]
        public void MoveNext_ReturnsCorrectInitialValue(int count)
        {
            bool valid = count > 0;

            // Arrange: Create a fresh logic state and retrieve expected values
            var data = CreateLogic(count);
            var logic = data.logic;
            var expected = data.expected;
            T expectedFirstValue = valid ? expected[0] : default;

            // Act: Perform the first move operation
            bool success = logic.MoveNext(ref logic, out T result);

            // Assert: Verify the method succeeded and returned the correct first element
            Assert.AreEqual(success, valid, "MoveNext should return true when elements are available.");
            Assert.AreEqual(expectedFirstValue, result,
                "MoveNext returned a different value than the first expected element.");
        }

        [Test]
        [TestCase(0)]
        [TestCase(1)]
        [TestCase(5)]
        [TestCase(15)]
        [TestCase(20)]
        public void MoveNext_IteratesThroughAllExpectedValues(int count)
        {
            // Arrange: Initialize the logic and the comparison data
            var data = CreateLogic(count);
            var logic = data.logic;
            var expected = data.expected;

            // Act & Assert: Traverse the entire sequence
            for (int i = 0; i < count; i++)
            {
                // We call MoveNext for every expected index
                bool success = logic.MoveNext(ref logic, out T result);

                Assert.IsTrue(success,
                    $"MoveNext failed prematurely at index {i} for a sequence of length {count}.");

                Assert.AreEqual(expected[i], result,
                    $"The value returned by MoveNext at index {i} does not match the expected source data.");
            }

            // Final Step: Ensure the logic recognizes it has reached the end
            bool exhausted = logic.MoveNext(ref logic, out _);
            Assert.IsFalse(exhausted,
                "MoveNext should return false after all elements have been consumed.");
        }

        [Test]
        [TestCase(0)]
        [TestCase(1)]
        [TestCase(5)]
        [TestCase(15)]
        [TestCase(20)]
        public void MoveNext_ReturnsFalse_And_Default_OnExhaustion(int count)
        {
            // Arrange: Initialize logic and advance it to the very end
            var data = CreateLogic(count);
            var logic = data.logic;

            // Act: Fully exhaust the sequence by consuming all 'count' elements
            for (int i = 0; i < count; i++)
            {
                logic.MoveNext(ref logic, out _);
            }

            // Attempt to move beyond the end of the sequence
            bool result = logic.MoveNext(ref logic, out T value);

            // Assert: Verify that the logic reports exhaustion correctly
            Assert.IsFalse(result,
                "MoveNext must return false once the sequence is exhausted or if it was initially empty.");

            // Safety: Verify that the output parameter is cleared to avoid leaking old data
            Assert.AreEqual(default(T), value,
                "The output value must be reset to default(T) when MoveNext returns false.");
        }

        [Test]
        [TestCase(0)]
        [TestCase(1)]
        [TestCase(5)]
        [TestCase(15)]
        [TestCase(20)]
        public void MoveNext_StaysFalse_AfterExhaustion(int count)
        {
            // Scenario: Finiteness - once the iterator returns false, it must remain false.
            // This prevents accidental wrap-around or invalid state transitions.
            var data = CreateLogic(count);
            var logic = data.logic;

            // Act: Fully exhaust the logic
            for (int i = 0; i < count; i++)
            {
                logic.MoveNext(ref logic, out _);
            }

            // First call after exhaustion
            bool firstExhaustion = logic.MoveNext(ref logic, out _);
            // Subsequent call after exhaustion
            bool subsequentExhaustion = logic.MoveNext(ref logic, out _);

            // Assert
            Assert.IsFalse(firstExhaustion,
                $"MoveNext should return false immediately after {count} elements.");
            Assert.IsFalse(subsequentExhaustion,
                "MoveNext must remain false (stay exhausted) on repeated calls.");
        }

        #endregion

        #region TryPeekNext Contract Tests

        [Test]
        [TestCase(0)]
        [TestCase(1)]
        [TestCase(5)]
        [TestCase(15)]
        [TestCase(20)]
        public void TryPeekNext_IdentifiesCorrectValue(int count)
        {
            bool valid = count > 0;

            // Arrange: Fresh logic state
            var data = CreateLogic(count);
            var logic = data.logic;
            var expected = data.expected;
            T expectedValue = valid ? expected[0] : default;

            // Act: Peek the first value
            bool success = logic.TryPeekNext(ref logic, out T result);

            // Assert
            Assert.AreEqual(success, valid, "TryPeekNext should return true when elements are available.");
            Assert.AreEqual(expectedValue, result, "The peeked value does not match the first expected element.");
        }

        [Test]
        [TestCase(0)]
        [TestCase(1)]
        [TestCase(5)]
        [TestCase(15)]
        [TestCase(20)]
        public void TryPeekNext_DoesNotAdvanceState(int count)
        {
            bool valid = count > 0;

            // Arrange
            var data = CreateLogic(count);
            var logic = data.logic;
            var expected = data.expected;
            T expectedValue = valid ? expected[0] : default;

            // Act: Peek then Move
            logic.TryPeekNext(ref logic, out T peeked);
            bool moveSuccess = logic.MoveNext(ref logic, out T moved);

            // Assert: Move must still succeed and return the same value as Peek
            Assert.AreEqual(moveSuccess, valid, "MoveNext must still succeed after a Peek operation.");
            Assert.AreEqual(peeked, moved, "MoveNext must return the same value that was just peeked.");
        }

        [Test]
        [TestCase(0)]
        [TestCase(1)]
        [TestCase(5)]
        [TestCase(15)]
        [TestCase(20)]
        public void TryPeekNext_ReturnsFalse_And_Default_OnExhaustion(int count)
        {
            // Arrange: Exhaust the logic
            var data = CreateLogic(count);
            var logic = data.logic;
            for (int i = 0; i < count; i++)
            {
                logic.MoveNext(ref logic, out _);
            }

            // Act: Attempt to peek beyond the end
            bool success = logic.TryPeekNext(ref logic, out T result);

            // Assert
            Assert.IsFalse(success, "TryPeekNext must return false when the logic is exhausted.");
            Assert.AreEqual(default(T), result, "TryPeekNext must return default(T) on failure.");
        }

        [Test]
        [TestCase(0)]
        [TestCase(1)]
        [TestCase(5)]
        [TestCase(15)]
        [TestCase(20)]
        public void TryPeekNext_IsIdempotent(int count)
        {
            // Arrange
            var data = CreateLogic(count);
            var logic = data.logic;

            // Act: Peek multiple times
            logic.TryPeekNext(ref logic, out T firstPeek);
            logic.TryPeekNext(ref logic, out T secondPeek);
            logic.TryPeekNext(ref logic, out T thirdPeek);

            // Assert: All peeks must yield the same result
            Assert.AreEqual(firstPeek, secondPeek, "Successive peeks must return the same value.");
            Assert.AreEqual(firstPeek, thirdPeek, "Successive peeks must return the same value.");
        }

        [Test]
        [TestCase(0)]
        [TestCase(1)]
        [TestCase(5)]
        [TestCase(15)]
        [TestCase(20)]
        public void TryPeekNext_FullIteration_SequenceValidation(int count)
        {
            // Arrange: Initialize logic and expected sequence
            var data = CreateLogic(count);
            var logic = data.logic;
            var expected = data.expected;

            // Act & Assert: Traverse the full chain, peeking before every move
            for (int i = 0; i < count; i++)
            {
                // 1. Validate peek at current position
                bool peekSuccess = logic.TryPeekNext(ref logic, out T peeked);
                Assert.IsTrue(peekSuccess, $"TryPeekNext failed at index {i}.");
                Assert.AreEqual(expected[i], peeked, $"Peek value mismatch at index {i}.");

                // 2. Consume the peeked element
                bool moveSuccess = logic.MoveNext(ref logic, out T moved);
                Assert.IsTrue(moveSuccess, $"MoveNext failed after Peek at index {i}.");
                Assert.AreEqual(peeked, moved, $"The value from Peek and MoveNext must be identical at index {i}.");
            }

            // 3. Final exhaustion check for Peek
            bool finalPeek = logic.TryPeekNext(ref logic, out _);
            Assert.IsFalse(finalPeek, "Peek must return false once the entire sequence is consumed.");
        }

        #endregion

        #region Full Contract Tests

        [Test]
        [TestCase(0)]
        [TestCase(1)]
        [TestCase(5)]
        [TestCase(15)]
        [TestCase(20)]
        public void FullContract_SystemStressTest(int count)
        {
            // Scenario: Comprehensive stress test of the entire IIterationLogic contract.
            // It verifies the interplay between HasNext, TryPeekNext, and MoveNext
            // throughout the entire lifecycle of the iteration, including redundant calls.
            var data = CreateLogic(count);
            var logic = data.logic;
            var expected = data.expected;

            for (int i = 0; i < count; i++)
            {
                // 1. Initial Availability Check
                Assert.IsTrue(logic.HasNext(ref logic),
                    $"Contract Violation: HasNext must be true at index {i} for count {count}.");

                // 2. Multi-Peek Stability (Interleaved with HasNext)
                bool peek1Success = logic.TryPeekNext(ref logic, out T p1);
                Assert.IsTrue(peek1Success, $"Contract Violation: TryPeekNext failed at index {i}.");

                // Redundant HasNext call to ensure Peek didn't advance the state
                Assert.IsTrue(logic.HasNext(ref logic),
                    $"State Corruption: HasNext became false after the first Peek at index {i}.");

                bool peek2Success = logic.TryPeekNext(ref logic, out T p2);
                Assert.IsTrue(peek2Success, $"Contract Violation: Second TryPeekNext failed at index {i}.");

                // Data Integrity Check
                Assert.AreEqual(p1, p2, $"Idempotency Violation: Consecutive peeks differ at index {i}.");
                Assert.AreEqual(expected[i], p1, $"Data Integrity: Peek mismatch at index {i}.");

                // 3. Final Execution Step
                bool moveSuccess = logic.MoveNext(ref logic, out T moved);

                Assert.IsTrue(moveSuccess, $"Execution Failure: MoveNext failed at index {i}.");
                Assert.AreEqual(p1, moved, $"Sync Error: MoveNext result differs from previous Peek at index {i}.");
            }

            // --- Post-Exhaustion Phase ---

            // 4. Final State Validation (All accessors must agree on exhaustion)
            Assert.IsFalse(logic.HasNext(ref logic),
                "Exhaustion Error: HasNext must be false after full consumption.");

            Assert.IsFalse(logic.TryPeekNext(ref logic, out _),
                "Exhaustion Error: TryPeekNext must return false after full consumption.");

            bool finalMove = logic.MoveNext(ref logic, out T finalVal);
            Assert.IsFalse(finalMove,
                "Exhaustion Error: MoveNext must return false after full consumption.");

            Assert.AreEqual(default(T), finalVal,
                "Safety Violation: Exhausted MoveNext must return default(T).");
        }

        #endregion
    }
}
