using NUnit.Framework;
using Rayforge.Core.Collections.Iterator;
using Rayforge.Core.Tests.Collections.Abstractions;
using System;
using UnityEngine;

namespace Rayforge.Core.Tests.Collections.Iterator
{
    [TestFixture(typeof(int))]
    [TestFixture(typeof(float))]
    [TestFixture(typeof(string))]
    public class ArrayIteratorStateTests<T> : IIterationLogicTests<T, ArrayIteratorState<T>>
    {
        #region IIterationLogic Impl

        protected override (ArrayIteratorState<T> logic, T[] expectedValues) CreateLogic(int count)
        {
            T[] items = TestDataUtility.CreateSampleItems<T>(count);
            var logic = new ArrayIteratorState<T>(items, 0, items.Length);
            return (logic, items);
        }

        #endregion

        #region Constructor Tests

        [Test]
        public void Constructor_HandlesNullAndEmptyArrays_Gracefully()
        {
            // Scenario 1: Null array
            var nullState = new ArrayIteratorState<T>(null, 0, 10);
            Assert.AreEqual(-1, GetPrivateField(nullState, "_index"));
            Assert.AreEqual(0, GetPrivateField(nullState, "_end"));

            // Scenario 2: Empty array
            var emptyState = new ArrayIteratorState<T>(Array.Empty<T>(), 0, 10);
            Assert.AreEqual(-1, GetPrivateField(emptyState, "_index"));
            Assert.AreEqual(0, GetPrivateField(emptyState, "_end"));
        }

        [Test]
        [TestCase(-10, 5, 0, 5, Description = "Start < 0 -> clamped to 0")]
        [TestCase(100, 5, 10, 0, Description = "Start > Length -> clamped to Length (Count becomes 0)")]
        [TestCase(10, 0, 10, 0, Description = "Start exactly at Length -> valid empty iteration")]
        public void Constructor_ClampsStartBoundary(int start, int count, int expectedStart, int expectedCount)
        {
            // Array length is 10
            T[] array = new T[10];
            var state = new ArrayIteratorState<T>(array, start, count);

            Assert.AreEqual(expectedStart - 1, GetPrivateField(state, "_index"), "Start index not clamped correctly.");
            Assert.AreEqual(expectedStart + expectedCount, GetPrivateField(state, "_end"), "End boundary based on clamped start is wrong.");
        }

        [Test]
        [TestCase(0, -5, 0, 0, Description = "Count < 0 -> clamped to 0")]
        [TestCase(0, 100, 0, 10, Description = "Count > Length -> clamped to remaining (Length)")]
        [TestCase(5, 10, 5, 5, Description = "Count > remaining space -> clamped to remaining")]
        public void Constructor_ClampsCountBoundary(int start, int count, int expectedStart, int expectedCount)
        {
            T[] array = new T[10];
            var state = new ArrayIteratorState<T>(array, start, count);

            Assert.AreEqual(expectedStart + expectedCount, GetPrivateField(state, "_end"), "Count not clamped to available array space.");
        }

        [Test]
        [TestCase(-50, -50, 0, 0, Description = "Both extremely negative")]
        [TestCase(100, 100, 10, 0, Description = "Both extremely positive")]
        [TestCase(int.MinValue, int.MaxValue, 0, 10, Description = "Extreme overflow potential")]
        public void Constructor_HandlesNonsensicalCombinations(int start, int count, int expectedStart, int expectedCount)
        {
            T[] array = new T[10];
            var state = new ArrayIteratorState<T>(array, start, count);

            // Verify that even with absurd inputs, the state remains within [0, 10]
            int actualIndex = (int)GetPrivateField(state, "_index");
            int actualEnd = (int)GetPrivateField(state, "_end");

            Assert.GreaterOrEqual(actualIndex, -1);
            Assert.LessOrEqual(actualEnd, array.Length);
            Assert.AreEqual(expectedStart - 1, actualIndex);
            Assert.AreEqual(expectedStart + expectedCount, actualEnd);
        }

        #endregion

        #region HasNext Tests

        [Test]
        public void HasNext_IsTrulyNonDestructive()
        {
            // Arrange
            T[] array = TestDataUtility.CreateSampleItems<T>(2);
            var state = new ArrayIteratorState<T>(array, 0, 2);

            // Act
            bool firstCheck = state.HasNext(ref state);
            bool secondCheck = state.HasNext(ref state);
            bool thirdCheck = state.HasNext(ref state);

            // Assert
            Assert.IsTrue(firstCheck, "Should have next element.");
            Assert.IsTrue(secondCheck, "Should still have next element (non-destructive).");
            Assert.IsTrue(thirdCheck, "State must not change no matter how often we call HasNext.");

            // Verify internal index hasn't moved via MoveNext
            state.MoveNext(ref state, out T result);
            Assert.AreEqual(array[0], result, "Index should still be at the start after multiple HasNext calls.");
        }

        [Test]
        public void HasNext_ReturnsFalse_AtExactEnd()
        {
            // Arrange: Array length 2, range count 1 (only index 0 is valid)
            T[] array = TestDataUtility.CreateSampleItems<T>(2);
            var state = new ArrayIteratorState<T>(array, 0, 1);

            // Act
            state.MoveNext(ref state, out _); // Move to index 0
            bool hasMore = state.HasNext(ref state);

            // Assert
            Assert.IsFalse(hasMore, "HasNext should be false when the next index equals the clamped _end.");
        }

        [Test]
        public void HasNext_HandlesExhaustion_Repeatedly()
        {
            // Arrange: Use a single element and move past it
            T[] array = TestDataUtility.CreateSampleItems<T>(1);
            var state = new ArrayIteratorState<T>(array, 0, 1);
            state.MoveNext(ref state, out _); // Now at the end

            // Act & Assert
            Assert.IsFalse(state.HasNext(ref state));

            // Simulate someone calling MoveNext anyway
            bool moved = state.MoveNext(ref state, out _);
            Assert.IsFalse(moved);
            Assert.IsFalse(state.HasNext(ref state), "Should remain false even after invalid MoveNext calls.");
        }

        [Test]
        public void HasNext_WorksWithClampedNegativeStart()
        {
            // Arrange: Start -5 will be clamped to 0
            T[] array = TestDataUtility.CreateSampleItems<T>(2);
            var state = new ArrayIteratorState<T>(array, -5, 2);

            // Assert
            Assert.IsTrue(state.HasNext(ref state), "HasNext should work correctly even if the original start was negative (clamped to 0).");
        }

        [Test]
        public void HasNext_ReturnsFalse_ForEmptyOrNullArray()
        {
            // Scenario 1: Empty Array
            var emptyState = new ArrayIteratorState<T>(Array.Empty<T>(), 0, 0);
            Assert.IsFalse(emptyState.HasNext(ref emptyState), "Empty array must never have a next element.");

            // Scenario 2: Null Array
            var nullState = new ArrayIteratorState<T>(null, 0, 0);
            Assert.IsFalse(nullState.HasNext(ref nullState), "Null array must never have a next element.");
        }

        #endregion

        #region TryPeekNext Tests

        [Test]
        public void TryPeekNext_RetrievesValue_WithoutAdvancing()
        {
            // Arrange
            var array = new[] { 100, 200, 300 };
            var state = new ArrayIteratorState<int>(array, 0, 3);

            // Act
            bool peekSuccess = state.TryPeekNext(ref state, out int peekedValue);

            // Assert
            Assert.IsTrue(peekSuccess);
            Assert.AreEqual(100, peekedValue, "Peek should return the first element.");

            // Verify State: MoveNext must still return the same element
            state.MoveNext(ref state, out int movedValue);
            Assert.AreEqual(100, movedValue, "MoveNext should return the same value that was just peeked.");
        }

        [Test]
        public void TryPeekNext_ReturnsFalse_AndDefault_AtEnd()
        {
            // Arrange: Only one element in range
            var array = new[] { 42 };
            var state = new ArrayIteratorState<int>(array, 0, 1);
            state.MoveNext(ref state, out _); // Index is now 0 (the end of our range)

            // Act
            bool peekSuccess = state.TryPeekNext(ref state, out int result);

            // Assert
            Assert.IsFalse(peekSuccess, "Should not be able to peek beyond the range.");
            Assert.AreEqual(0, result, "Result should be default(T) when peek fails.");
        }

        [Test]
        public void TryPeekNext_Consistency_AcrossMultipleCalls()
        {
            // Arrange
            var array = new[] { "A", "B", "C" };
            var state = new ArrayIteratorState<string>(array, 1, 2); // Starts at "B"

            // Act & Assert
            state.TryPeekNext(ref state, out string p1);
            state.TryPeekNext(ref state, out string p2);

            Assert.AreEqual("B", p1);
            Assert.AreEqual("B", p2, "Repeated peeks must return the same value if no MoveNext occurred.");
        }

        [Test]
        public void TryPeekNext_WorksAfterPartialIteration()
        {
            // Arrange
            var array = new[] { 1, 2, 3, 4 };
            var state = new ArrayIteratorState<int>(array, 0, 4);

            // Act
            state.MoveNext(ref state, out _); // At 1
            state.MoveNext(ref state, out _); // At 2

            bool canPeek = state.TryPeekNext(ref state, out int peeked);

            // Assert
            Assert.IsTrue(canPeek);
            Assert.AreEqual(3, peeked, "Should peek at the third element after moving twice.");
        }

        [Test]
        public void TryPeekNext_ReturnsFalse_AndDefault_ForEmptyOrNullArray()
        {
            // Scenario 1: Empty Array
            var emptyState = new ArrayIteratorState<string>(new string[0], 0, 0);
            bool peekEmpty = emptyState.TryPeekNext(ref emptyState, out string resultEmpty);

            Assert.IsFalse(peekEmpty);
            Assert.IsNull(resultEmpty, "Should return default(T) for empty arrays.");

            // Scenario 2: Null Array
            var nullState = new ArrayIteratorState<int>(null, 0, 0);
            bool peekNull = nullState.TryPeekNext(ref nullState, out int resultNull);

            Assert.IsFalse(peekNull);
            Assert.AreEqual(0, resultNull, "Should return default(T) for null arrays.");
        }

        #endregion

        #region MoveNext Tests

        [Test]
        public void MoveNext_StandardIteration_AdvancesAndReturnsValues()
        {
            // Arrange: Create test data based on T
            T[] items = TestDataUtility.CreateSampleItems<T>(3);
            var state = new ArrayIteratorState<T>(items, 0, 3);

            // Act & Assert: Element 1
            bool m1 = state.MoveNext(ref state, out T r1);
            Assert.IsTrue(m1);
            Assert.AreEqual(items[0], r1);
            Assert.AreEqual(0, GetPrivateField(state, "_index"), "Index should be 0 after first MoveNext.");

            // Act & Assert: Element 2
            bool m2 = state.MoveNext(ref state, out T r2);
            Assert.IsTrue(m2);
            Assert.AreEqual(items[1], r2);
            Assert.AreEqual(1, GetPrivateField(state, "_index"), "Index should be 1 after second MoveNext.");

            // Act & Assert: Element 3
            bool m3 = state.MoveNext(ref state, out T r3);
            Assert.IsTrue(m3);
            Assert.AreEqual(items[2], r3);
        }

        [Test]
        public void MoveNext_Exhaustion_ReturnsFalseAndDefault()
        {
            // Arrange: Single element
            T[] items = TestDataUtility.CreateSampleItems<T>(1);
            var state = new ArrayIteratorState<T>(items, 0, 1);

            // Act
            state.MoveNext(ref state, out _); // Index -> 0
            bool hasMore = state.MoveNext(ref state, out T result); // Index -> 1

            // Assert
            Assert.IsFalse(hasMore, "MoveNext must return false when moving past the range.");
            Assert.AreEqual(default(T), result, "Result must be default(T) on failure.");
            Assert.AreEqual(1, (int)GetPrivateField(state, "_index"), "Index should still increment even if invalid.");
        }

        [Test]
        public void MoveNext_RespectsClampedRange_NotArrayEnd()
        {
            // Arrange: Array of 5, but we only want a range of 2 starting at index 1
            T[] items = TestDataUtility.CreateSampleItems<T>(5);
            var state = new ArrayIteratorState<T>(items, 1, 2); // Expected Elements: items[1], items[2]

            // Act
            state.MoveNext(ref state, out T v1);
            state.MoveNext(ref state, out T v2);
            bool hasMore = state.MoveNext(ref state, out T v3);

            // Assert
            Assert.AreEqual(items[1], v1);
            Assert.AreEqual(items[2], v2);
            Assert.IsFalse(hasMore, "Should stop at clamped range end, not array end.");
        }

        [Test]
        public void MoveNext_HandlesEmptyAndNullArrays_WithoutCrashing()
        {
            // Empty
            var emptyState = new ArrayIteratorState<T>(Array.Empty<T>(), 0, 0);
            Assert.IsFalse(emptyState.MoveNext(ref emptyState, out T r1));
            Assert.AreEqual(default(T), r1);

            // Null
            var nullState = new ArrayIteratorState<T>(null, 0, 0);
            Assert.IsFalse(nullState.MoveNext(ref nullState, out T r2));
            Assert.AreEqual(default(T), r2);
        }

        [Test]
        public void MoveNext_AfterExhaustion_StaysFalse()
        {
            // Arrange
            T[] items = TestDataUtility.CreateSampleItems<T>(1);
            var state = new ArrayIteratorState<T>(items, 0, 1);

            // Act
            state.MoveNext(ref state, out _); // Valid (items[0])
            bool firstFalse = state.MoveNext(ref state, out _); // Invalid
            bool secondFalse = state.MoveNext(ref state, out _); // Still invalid

            // Assert
            Assert.IsFalse(firstFalse);
            Assert.IsFalse(secondFalse, "Consecutive calls to MoveNext after end must remain false.");
        }

        #endregion

        #region IsValid Tests

        [Test]
        public void IsValid_HandlesNullArray_ReturnsFalse()
        {
            // Arrange
            var state = default(ArrayIteratorState<int>);

            // Act & Assert
            bool result = InvokeIsValid(state, 0);

            Assert.IsFalse(result, "IsValid must return false if _array is null.");
        }

        [Test]
        [TestCase(0, 5, 0, true, Description = "Valid index at start")]
        [TestCase(0, 5, 4, true, Description = "Valid index at end")]
        [TestCase(0, 5, -1, false, Description = "Index below zero")]
        [TestCase(0, 5, 5, false, Description = "Index at _end (exclusive boundary)")]
        [TestCase(2, 3, 5, false, Description = "Index exceeds _end even if within array length")]
        public void IsValid_VerifiesBoundsCorrectly(int start, int count, int testIndex, bool expected)
        {
            // Arrange
            var array = new int[10];
            var state = new ArrayIteratorState<int>(array, start, count);

            // Act
            bool result = InvokeIsValid(state, testIndex);

            // Assert
            Assert.AreEqual(expected, result, $"IsValid logic failed for Index {testIndex}");
        }

        #endregion

        #region Helper Methods

        private static object GetPrivateField<T>(ArrayIteratorState<T> instance, string fieldName)
        {
            var field = typeof(ArrayIteratorState<T>).GetField(fieldName,
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            return field?.GetValue(instance);
        }

        private static bool InvokeIsValid<T>(ArrayIteratorState<T> state, int index)
        {
            var method = typeof(ArrayIteratorState<T>).GetMethod("IsValid",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);

            if (method == null) throw new System.Exception("Method IsValid not found.");

            return (bool)method.Invoke(null, new object[] { state, index });
        }

        #endregion
    }
}
