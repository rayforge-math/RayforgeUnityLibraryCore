using NUnit.Framework;
using Rayforge.Core.Collections.Abstractions;
using System;
using UnityEngine;

namespace Rayforge.Core.Tests.Collections.Abstractions
{
    public abstract class IIterationLogicTests<T, TLogic>
        where TLogic : struct, IIterationLogic<T, TLogic>
    {
        #region Create Test Env

        protected abstract (TLogic logic, T[] expectedValues) CreateLogic(int count);

        #endregion

        #region Core Logic Contracts

        [Test]
        [TestCase(0)]
        [TestCase(1)]
        [TestCase(5)]
        public void HasNext_IsIdempotent_And_Consistent(int count)
        {
            // Arrange: Initialize the logic with the specified number of elements
            var (logic, expected) = CreateLogic(count);

            if (count > 0)
            {
                // Scenario: Elements are available
                T firstExpected = expected[0];

                // Act & Assert: Multiple HasNext calls must not advance the internal state
                Assert.IsTrue(logic.HasNext(ref logic), "HasNext must be true when elements are available.");
                Assert.IsTrue(logic.HasNext(ref logic), "Successive HasNext calls must be idempotent (non-destructive).");

                // Act & Assert: Peek must return the first element without consuming it
                bool peekSuccess = logic.TryPeekNext(ref logic, out T peeked);
                Assert.IsTrue(peekSuccess, "Peek should succeed when HasNext is true.");
                Assert.AreEqual(firstExpected, peeked, "Peeked value does not match the first expected element.");

                // HasNext must still be true after a Peek
                Assert.IsTrue(logic.HasNext(ref logic), "HasNext must remain true after a Peek operation.");

                // Final Verification: MoveNext must still return that same first element
                bool moveSuccess = logic.MoveNext(ref logic, out T moved);
                Assert.IsTrue(moveSuccess, "MoveNext must succeed for the first element.");
                Assert.AreEqual(firstExpected, moved, "MoveNext returned the wrong element after Peek/HasNext calls.");
            }
            else
            {
                // Scenario: Empty collection
                // Act & Assert
                Assert.IsFalse(logic.HasNext(ref logic), "HasNext must be false for empty iterators.");
                Assert.IsFalse(logic.HasNext(ref logic), "HasNext must stay false (idempotent) for empty iterators.");

                bool peekSuccess = logic.TryPeekNext(ref logic, out _);
                Assert.IsFalse(peekSuccess, "Peek must return false when HasNext is false.");
            }
        }

        [Test]
        [TestCase(0)]
        [TestCase(1)]
        [TestCase(5)]
        public void MoveNext_ReturnsFalse_And_Default_OnExhaustion(int count)
        {
            // Scenario: Ensure the iterator behaves correctly when it is empty or runs out of data
            // We initialize the logic with 'count' elements
            var (logic, _) = CreateLogic(count);

            // Act: Exhaust the iterator by moving through all available elements
            for (int i = 0; i < count; i++)
            {
                logic.MoveNext(ref logic, out _);
            }

            // Attempt to move one step further (or move immediately if count was 0)
            bool result = logic.MoveNext(ref logic, out T value);

            // Assert
            Assert.IsFalse(result, "MoveNext must return false when no elements are left to iterate.");
            Assert.AreEqual(default(T), value, "The output value must be default(T) when MoveNext returns false.");
        }

        [Test]
        [TestCase(0)]
        [TestCase(1)]
        [TestCase(5)]
        public void MoveNext_StaysFalse_AfterExhaustion(int count)
        {
            // Scenario: Finiteness - once the iterator returns false, it must remain false.
            // This prevents accidental wrap-around or invalid state transitions.
            var (logic, _) = CreateLogic(count);

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

        [Test]
        [TestCase(0)]
        [TestCase(1)]
        [TestCase(5)]
        public void TryPeekNext_DoesNotAdvance_And_IsIdempotent(int count)
        {
            // Scenario: Peeking multiple times must return the same element (if any) 
            // without advancing the cursor or affecting future MoveNext calls.
            var (logic, expected) = CreateLogic(count);

            if (count > 0)
            {
                T firstExpected = expected[0];

                // Act
                bool firstPeekSuccess = logic.TryPeekNext(ref logic, out T firstPeek);
                bool secondPeekSuccess = logic.TryPeekNext(ref logic, out T secondPeek);

                // Assert
                Assert.IsTrue(firstPeekSuccess, "First peek should be successful.");
                Assert.IsTrue(secondPeekSuccess, "Second peek should be successful.");
                Assert.AreEqual(firstExpected, firstPeek, "Peeked value must match the expected first element.");
                Assert.AreEqual(firstPeek, secondPeek, "Consecutive Peeks must return the same value.");

                // Verify state: Peek must not consume the element
                Assert.IsTrue(logic.HasNext(ref logic), "HasNext must remain true after Peek.");

                logic.MoveNext(ref logic, out T movedValue);
                Assert.AreEqual(firstExpected, movedValue, "MoveNext must still return the element that was peeked.");
            }
            else
            {
                // Act
                bool peekSuccess = logic.TryPeekNext(ref logic, out T peeked);

                // Assert
                Assert.IsFalse(peekSuccess, "Peek must return false for empty iterators.");
                Assert.AreEqual(default(T), peeked, "Peeked value must be default(T) for empty iterators.");
            }
        }

        [Test]
        [TestCase(0)]
        [TestCase(1)]
        [TestCase(5)]
        public void TryPeekNext_ReturnsFalse_AfterExhaustion(int count)
        {
            // Scenario: Peek behavior must be consistently false once the stream is exhausted,
            // regardless of the initial collection size.
            var (logic, _) = CreateLogic(count);

            // Act: Move to the end by consuming all available elements
            for (int i = 0; i < count; i++)
            {
                logic.MoveNext(ref logic, out _);
            }

            // Attempt to peek when no elements are left
            bool success = logic.TryPeekNext(ref logic, out T result);

            // Assert
            Assert.IsFalse(success, "TryPeekNext must return false when the iterator is exhausted.");
            Assert.AreEqual(default(T), result, "The result of a failed Peek must be default(T).");

            // Safety check: Ensure HasNext also agrees with the exhausted state
            Assert.IsFalse(logic.HasNext(ref logic), "HasNext must be false upon exhaustion.");
        }

        [Test]
        [TestCase(0)]
        [TestCase(1)]
        [TestCase(5)]
        public void Consistency_HasNext_Matches_MoveNext(int count)
        {
            // Scenario: The "Contract Agreement"
            // This ensures that HasNext and MoveNext are perfectly synchronized.
            var (logic, _) = CreateLogic(count);

            int iterations = 0;

            // Act & Assert: Loop as long as HasNext is true
            while (logic.HasNext(ref logic))
            {
                bool moveResult = logic.MoveNext(ref logic, out _);

                Assert.IsTrue(moveResult,
                    $"If HasNext is true, MoveNext MUST return true. Failed at index {iterations}.");

                iterations++;
            }

            // Act & Assert: Once HasNext is false, MoveNext must also be false
            bool finalMove = logic.MoveNext(ref logic, out _);

            Assert.IsFalse(finalMove,
                "If HasNext is false, MoveNext MUST return false.");

            Assert.AreEqual(count, iterations,
                "The number of successful MoveNext calls must match the expected count.");
        }

        [Test]
        [TestCase(0)]
        [TestCase(1)]
        [TestCase(5)]
        public void Interleaved_Peek_And_Move_Consistency(int count)
        {
            // Scenario: Mixing Peek and Move operations to ensure they always point to the same element.
            var (logic, expected) = CreateLogic(count);

            for (int i = 0; i < count; i++)
            {
                T expectedValue = expected[i];

                // Perform multiple peeks
                bool peek1Success = logic.TryPeekNext(ref logic, out T p1);
                bool peek2Success = logic.TryPeekNext(ref logic, out T p2);

                Assert.IsTrue(peek1Success, $"Peek 1 should succeed at index {i}.");
                Assert.IsTrue(peek2Success, $"Peek 2 should succeed at index {i}.");
                Assert.AreEqual(p1, p2, $"Consecutive peeks must return the same value at index {i}.");
                Assert.AreEqual(expectedValue, p1, $"Peeked value must match expected ground truth at index {i}.");

                // Move to the element we just peeked
                bool moveSuccess = logic.MoveNext(ref logic, out T m1);

                Assert.IsTrue(moveSuccess, $"MoveNext should succeed at index {i} after Peeking.");
                Assert.AreEqual(p1, m1, $"MoveNext must return the same value that was just returned by Peek at index {i}.");
            }

            // Final check after all items are consumed
            Assert.IsFalse(logic.HasNext(ref logic), "HasNext must be false after the interleaved loop is finished.");
            Assert.IsFalse(logic.TryPeekNext(ref logic, out _), "TryPeekNext must return false after exhaustion.");
        }

        #endregion
    }
}
