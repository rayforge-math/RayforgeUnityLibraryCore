using NUnit.Framework;
using Rayforge.Core.Collections.Abstractions.Tests;
using System;
using Rayforge.Core.TestEnv;

namespace Rayforge.Core.Collections.Iterator.Tests
{
    [TestFixture(typeof(int))]
    [TestFixture(typeof(float))]
    [TestFixture(typeof(string))]
    public class ArrayIteratorStateTests<T> : IIterationLogicTests<T, ArrayIteratorState<T>>
    {
        #region IIterationLogic Impl

        protected override IterationData<T, ArrayIteratorState<T>> CreateLogic(int count)
        {
            T[] items = TestUtility.CreateSampleItems<T>(count);
            var logic = new ArrayIteratorState<T>(items, 0, items.Length);
            return new IterationData<T, ArrayIteratorState<T>>
            {
                logic = logic,
                expected = items
            };
        }

        #endregion

        #region Constructor Tests

        [Test]
        public void Constructor_HandlesNullAndEmptyArrays_Gracefully()
        {
            // Scenario 1: Null array should act as an empty iterator
            var nullState = new ArrayIteratorState<T>(null, 0, 10);
            Assert.IsFalse(nullState.HasNext(ref nullState), "Null array should be treated as empty.");

            // Scenario 2: Empty array should act as an empty iterator
            var emptyState = new ArrayIteratorState<T>(Array.Empty<T>(), 0, 10);
            Assert.IsFalse(emptyState.HasNext(ref emptyState), "Empty array should have no next elements.");
        }

        [Test]
        [TestCase(-10, 5, 5, Description = "Start < 0 -> clamped to 0, count remains 5")]
        [TestCase(100, 5, 0, Description = "Start > Length -> clamped to Length, count becomes 0")]
        [TestCase(10, 0, 0, Description = "Start exactly at Length -> empty iteration")]
        public void Constructor_ClampsStartBoundary(int start, int count, int expectedCount)
        {
            T[] array = new T[10];
            var state = new ArrayIteratorState<T>(array, start, count);

            // Verify count by consuming the iterator
            int consumed = 0;
            while (state.MoveNext(ref state, out _)) consumed++;

            Assert.AreEqual(expectedCount, consumed, "The number of consumed elements does not match the clamped range.");
        }

        [Test]
        [TestCase(0, -5, 0, Description = "Count < 0 -> clamped to 0")]
        [TestCase(0, 100, 10, Description = "Count > Length -> clamped to remaining")]
        public void Constructor_ClampsCountBoundary(int start, int count, int expectedCount)
        {
            T[] array = new T[10];
            var state = new ArrayIteratorState<T>(array, start, count);

            int consumed = 0;
            while (state.MoveNext(ref state, out _)) consumed++;

            Assert.AreEqual(expectedCount, consumed, "Count not clamped correctly to available array space.");
        }

        [Test]
        [TestCase(-50, -50, 0, Description = "Both extremely negative -> Clamped to empty")]
        [TestCase(100, 100, 0, Description = "Both extremely positive -> Clamped to empty")]
        [TestCase(int.MinValue, int.MaxValue, 10, Description = "Extreme overflow potential -> Should cover full array")]
        public void Constructor_HandlesNonsensicalCombinations(int start, int count, int expectedCount)
        {
            // Arrange: Array length is 10
            T[] array = new T[10];
            var state = new ArrayIteratorState<T>(array, start, count);

            // Act: Count how many elements the iterator actually yields
            int consumed = 0;
            while (state.MoveNext(ref state, out _))
            {
                consumed++;
            }

            // Assert: Verify that the state behaves as expected despite nonsense input
            Assert.AreEqual(expectedCount, consumed,
                $"Iterator yielded {consumed} elements, but expected {expectedCount} based on clamping logic.");
        }

        #endregion

        #region HasNext Tests

        [Test]
        public void HasNext_WorksWithClampedNegativeStart()
        {
            // Arrange: Start -5 will be clamped to 0
            T[] array = TestUtility.CreateSampleItems<T>(2);
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
        public void MoveNext_Exhaustion_ReturnsFalseAndDefault()
        {
            // Arrange: Single element array
            T[] items = TestUtility.CreateSampleItems<T>(1);
            var state = new ArrayIteratorState<T>(items, 0, 1);

            // Act: Consume the single element
            state.MoveNext(ref state, out _);

            // Attempt to move past the range
            bool hasMore = state.MoveNext(ref state, out T result);

            // Assert: Verify standard exhaustion contract
            Assert.IsFalse(hasMore, "MoveNext must return false when moving past the range.");
            Assert.AreEqual(default(T), result, "Result must be default(T) on failure.");

            // Verify consistency: The iterator must remain exhausted even after repeated calls
            bool stillExhausted = state.MoveNext(ref state, out T subsequentResult);
            Assert.IsFalse(stillExhausted, "MoveNext must remain false after full exhaustion.");
            Assert.AreEqual(default(T), subsequentResult, "Result must remain default(T) after full exhaustion.");
        }

        [Test]
        public void MoveNext_RespectsClampedRange_NotArrayEnd()
        {
            // Arrange: Array of 5, but we only want a range of 2 starting at index 1
            T[] items = TestUtility.CreateSampleItems<T>(5);
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

        private static bool InvokeIsValid<TType>(ArrayIteratorState<TType> state, int index)
        {
            var method = typeof(ArrayIteratorState<TType>).GetMethod("IsValid",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);

            if (method == null) throw new System.Exception("Method IsValid not found.");

            return (bool)method.Invoke(null, new object[] { state, index });
        }

        #endregion
    }
}
