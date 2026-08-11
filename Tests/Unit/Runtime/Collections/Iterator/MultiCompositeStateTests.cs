using NUnit.Framework;
using Rayforge.Core.Collections.Abstractions;
using Rayforge.Core.Collections.Abstractions.Tests;
using Rayforge.Core.Collections.Helpers;
using Rayforge.Core.TestEnv;
using System;

namespace Rayforge.Core.Collections.Iterator.Tests
{
    [TestFixture(typeof(int))]
    [TestFixture(typeof(float))]
    [TestFixture(typeof(string))]
    public class MultiCompositeStateTests<T> : IIterationLogicTests<T, MultiCompositeState<T>>
    {
        #region Structs

        public struct ArrayLogic<TValue> : IIterationLogic<TValue, ArrayLogic<TValue>>
        {
            public TValue[] Items;
            public int Index;

            public bool HasNext(ref ArrayLogic<TValue> state) => state.Index + 1 < state.Items.Length;
            public bool TryPeekNext(ref ArrayLogic<TValue> state, out TValue result)
            {
                if (HasNext(ref state)) { result = state.Items[state.Index + 1]; return true; }
                result = default; return false;
            }
            public bool MoveNext(ref ArrayLogic<TValue> state, out TValue result)
            {
                if (HasNext(ref state)) { result = state.Items[++state.Index]; return true; }
                result = default; return false;
            }
        }

        public struct ConstantLogic<TValue> : IIterationLogic<TValue, ConstantLogic<TValue>>
        {
            public TValue Value; 
            public bool IsConsumed;

            public bool HasNext(ref ConstantLogic<TValue> state) => !state.IsConsumed;
            public bool TryPeekNext(ref ConstantLogic<TValue> state, out TValue result)
            {
                result = state.Value;
                return !state.IsConsumed;
            }
            public bool MoveNext(ref ConstantLogic<TValue> state, out TValue result)
            {
                if (!state.IsConsumed)
                {
                    state.IsConsumed = true;
                    result = state.Value;
                    return true;
                }
                result = default; return false;
            }
        }

        #endregion


        #region IIterationLogic Implementation

        protected override IterationTestData<T, MultiCompositeState<T>> CreateLogic(int count)
        {
            var samples = TestUtility.CreateSampleItems<T>(count);
            var logicSources = new IIterator<T>[count];

            for (int i = 0; i < count; ++i)
            {
                // Wrap MockLogic in an adapter that implements IIterator<T>
                var logic = new MockLogic<T> { Items = new[] { samples[i] } };
                logicSources[i] = new Iterator<T, MockLogic<T>>(logic);
            }

            return new IterationTestData<T, MultiCompositeState<T>>
            {
                logic = new MultiCompositeState<T>(logicSources),
                expected = samples
            };
        }

        #endregion

        #region Constructor & Boundary Tests

        [Test]
        public void Constructor_WithNullArray_ThrowsArgumentNullException()
        {
            // Act & Assert: Initializing with null must throw an ArgumentNullException
            Assert.Throws<ArgumentNullException>(() =>
            {
                var state = new MultiCompositeState<T>(null);
            }, "Constructor should throw ArgumentNullException when sources array is null.");
        }

        [Test]
        public void Constructor_WithDefaultStruct_HandlesGracefully()
        {
            // Act: Initialize with default struct (should be dormant)
            var state = default(MultiCompositeState<T>);

            // Assert: Should not throw and remain empty
            Assert.DoesNotThrow(() => {
                bool hasNext = state.HasNext(ref state);
                Assert.IsFalse(hasNext, "Default struct should be treated as empty and not throw.");
            });
        }

        [Test]
        public void Constructor_AcceptsMixedIterators()
        {
            var state1 = new ArrayLogic<T>
            {
                Items = TestUtility.CreateSampleItems<T>(10),
                Index = -1
            };

            var state2 = new ConstantLogic<T>
            {
                Value = TestUtility.CreateSampleItems<T>(1)[0],
                IsConsumed = false
            };

            var states = new IIterator<T>[] {
                new Iterator<T, ArrayLogic<T>>(state1),
                new Iterator<T, ConstantLogic<T>>(state2)
            };

            Assert.DoesNotThrow(() =>
            {
                var composite = new MultiCompositeState<T>(states);
            });
        }

        #endregion

        #region MoveNext Tests

        [Test]
        public void MoveNext_WithNullElementsInArray_ThrowsException()
        {
            // Arrange: Ein gültiger Iterator und ein null-Eintrag im Composite
            var item = TestUtility.CreateSampleItems<T>(1)[0];
            var mockLogic = new MockLogic<T> { Items = new[] { item } };
            var iter = new Iterator<T, MockLogic<T>>(mockLogic);

            // Composite mit einem null-Eintrag
            var state = new MultiCompositeState<T>(null!, iter);

            // Act & Assert: Muss beim ersten MoveNext knallen
            Assert.Throws<NullReferenceException>(() =>
            {
                state.MoveNext(ref state, out _);
            }, "MoveNext should throw if it encounters a null source in the array.");
        }

        [Test]
        public void MoveNext_WithEmptyIterators_ExhaustsCorrectly()
        {
            // Arrange: Prepare two empty iterators
            var empty1 = Array.Empty<T>().ToIterator();
            var empty2 = Array.Empty<T>().ToIterator();
            var state = new MultiCompositeState<T>(empty1, empty2);

            // Act & Assert: Should gracefully report no elements
            Assert.IsFalse(state.MoveNext(ref state, out _), "Should be exhausted when all sources are empty.");
        }

        [Test]
        public void MoveNext_IteratesCorrectlyOverMixedStates()
        {
            // Arrange: 
            // 1. ArrayLogic with 2 elements
            // 2. ConstantLogic with 1 element
            var items = TestUtility.CreateSampleItems<T>(2);
            var constantItem = TestUtility.CreateSampleItems<T>(1)[0];

            var state1 = new ArrayLogic<T> { Items = items, Index = -1 };
            var state2 = new ConstantLogic<T> { Value = constantItem, IsConsumed = false };

            // Create the composite with mixed iterator types
            var composite = new MultiCompositeState<T>(
                new Iterator<T, ArrayLogic<T>>(state1),
                new Iterator<T, ConstantLogic<T>>(state2)
            );

            // Act & Assert: Expected sequence: items[0], items[1], constantItem

            // 1st Element from ArrayLogic
            Assert.IsTrue(composite.MoveNext(ref composite, out T res1), "Step 1 failed.");
            Assert.AreEqual(items[0], res1);

            // 2nd Element from ArrayLogic
            Assert.IsTrue(composite.MoveNext(ref composite, out T res2), "Step 2 failed.");
            Assert.AreEqual(items[1], res2);

            // 3rd Element from ConstantLogic
            Assert.IsTrue(composite.MoveNext(ref composite, out T res3), "Step 3 failed.");
            Assert.AreEqual(constantItem, res3);

            // Verify completion
            Assert.IsFalse(composite.MoveNext(ref composite, out _), "Iterator should be exhausted after all elements.");
        }

        #endregion

        #region TryPeekNext Tests

        [Test]
        public void TryPeekNext_WithNullElementsInArray_ThrowsException()
        {
            // Arrange: Ein gültiger Iterator und ein null-Eintrag im Composite
            var item = TestUtility.CreateSampleItems<T>(1)[0];
            var mockLogic = new MockLogic<T> { Items = new[] { item } };
            var iter = new Iterator<T, MockLogic<T>>(mockLogic);

            // Composite mit einem null-Eintrag an erster Stelle
            var state = new MultiCompositeState<T>(null!, iter);

            // Act & Assert: Muss beim ersten TryPeekNext knallen
            Assert.Throws<NullReferenceException>(() =>
            {
                state.TryPeekNext(ref state, out _);
            }, "TryPeekNext should throw if it encounters a null source in the array.");
        }

        [Test]
        public void TryPeekNext_WithEmptyIterators_ExhaustsCorrectly()
        {
            // Arrange: Prepare two empty iterators
            var empty1 = Array.Empty<T>().ToIterator();
            var empty2 = Array.Empty<T>().ToIterator();
            var state = new MultiCompositeState<T>(empty1, empty2);

            // Act & Assert: Should gracefully report no elements
            Assert.IsFalse(state.TryPeekNext(ref state, out _), "Should be exhausted when all sources are empty.");
        }

        [Test]
        public void TryPeekNext_TransitionsBetweenSourcesCorrectly()
        {
            // Arrange: First iterator is empty, second contains an item
            var empty = new ConstantLogic<T> { IsConsumed = true };
            var item = TestUtility.CreateSampleItems<T>(1)[0];
            var full = new ConstantLogic<T> { Value = item, IsConsumed = false };

            var state = new MultiCompositeState<T>(
                new Iterator<T, ConstantLogic<T>>(empty),
                new Iterator<T, ConstantLogic<T>>(full)
            );

            // Act: Peek the next available element
            bool success = state.TryPeekNext(ref state, out T result);

            // Assert: Should skip the empty iterator and find the item in the second one
            Assert.IsTrue(success, "TryPeekNext should transition to the next valid source.");
            Assert.AreEqual(item, result);
        }

        [Test]
        public void TryPeekNext_PeeksCorrectlyOverMixedStates()
        {
            // Arrange:
            // 1. ArrayLogic with 2 elements
            // 2. ConstantLogic with 1 element
            var items = TestUtility.CreateSampleItems<T>(2);
            var constantItem = TestUtility.CreateSampleItems<T>(1)[0];

            var state1 = new ArrayLogic<T> { Items = items, Index = -1 };
            var state2 = new ConstantLogic<T> { Value = constantItem, IsConsumed = false };

            var composite = new MultiCompositeState<T>(
                new Iterator<T, ArrayLogic<T>>(state1),
                new Iterator<T, ConstantLogic<T>>(state2)
            );

            // Act & Assert: Verify that peaking does not consume elements

            // 1. Peek first element from ArrayLogic
            Assert.IsTrue(composite.TryPeekNext(ref composite, out T res1), "Peek 1 failed.");
            Assert.AreEqual(items[0], res1);

            // Consume first element
            composite.MoveNext(ref composite, out _);

            // 2. Peek second element from ArrayLogic
            Assert.IsTrue(composite.TryPeekNext(ref composite, out T res2), "Peek 2 failed.");
            Assert.AreEqual(items[1], res2);

            // Consume second element
            composite.MoveNext(ref composite, out _);

            // 3. Peek third element from ConstantLogic
            Assert.IsTrue(composite.TryPeekNext(ref composite, out T res3), "Peek 3 failed.");
            Assert.AreEqual(constantItem, res3);
        }

        #endregion

        #region HasNext Tests

        [Test]
        public void HasNext_WithNullElementsInArray_ThrowsException()
        {
            // Arrange
            var item = TestUtility.CreateSampleItems<T>(1)[0];
            var mockLogic = new MockLogic<T> { Items = new[] { item } };
            var iter = new Iterator<T, MockLogic<T>>(mockLogic);

            var state = new MultiCompositeState<T>(null!, iter);

            // Act & Assert
            Assert.Throws<NullReferenceException>(() =>
            {
                state.HasNext(ref state);
            }, "HasNext should throw if it encounters a null source in the array.");
        }

        [Test]
        public void HasNext_WithEmptyIterators_ReturnsFalse()
        {
            // Arrange: Prepare two empty iterators
            var empty1 = Array.Empty<T>().ToIterator();
            var empty2 = Array.Empty<T>().ToIterator();
            var state = new MultiCompositeState<T>(empty1, empty2);

            // Act & Assert: Should report no elements available
            Assert.IsFalse(state.HasNext(ref state), "HasNext should return false when all sources are empty.");
        }

        [Test]
        public void HasNext_TransitionsBetweenSourcesCorrectly()
        {
            // Arrange: First iterator is empty, second contains one item
            var empty = new ConstantLogic<T> { IsConsumed = true };
            var item = TestUtility.CreateSampleItems<T>(1)[0];
            var full = new ConstantLogic<T> { Value = item, IsConsumed = false };

            var state = new MultiCompositeState<T>(
                new Iterator<T, ConstantLogic<T>>(empty),
                new Iterator<T, ConstantLogic<T>>(full)
            );

            // Act & Assert: Should skip the empty iterator and return true for the second one
            Assert.IsTrue(state.HasNext(ref state), "HasNext should correctly identify the next available source.");
        }

        [Test]
        public void HasNext_TracksMixedStatesCorrectly()
        {
            // Arrange:
            // 1. ArrayLogic with 2 elements
            // 2. ConstantLogic with 1 element
            var items = TestUtility.CreateSampleItems<T>(2);
            var constantItem = TestUtility.CreateSampleItems<T>(1)[0];

            var state1 = new ArrayLogic<T> { Items = items, Index = -1 };
            var state2 = new ConstantLogic<T> { Value = constantItem, IsConsumed = false };

            var composite = new MultiCompositeState<T>(
                new Iterator<T, ArrayLogic<T>>(state1),
                new Iterator<T, ConstantLogic<T>>(state2)
            );

            // Act & Assert: Verify HasNext status through the iteration lifecycle

            // Initial state: HasNext should be true
            Assert.IsTrue(composite.HasNext(ref composite), "Step 1: HasNext should be true.");
            composite.MoveNext(ref composite, out _); // Consume first item

            // After consuming 1st item: HasNext should still be true
            Assert.IsTrue(composite.HasNext(ref composite), "Step 2: HasNext should be true.");
            composite.MoveNext(ref composite, out _); // Consume 2nd item

            // After consuming 2nd item: HasNext should still be true (for the constant logic)
            Assert.IsTrue(composite.HasNext(ref composite), "Step 3: HasNext should be true.");
            composite.MoveNext(ref composite, out _); // Consume 3rd item

            // Finally exhausted
            Assert.IsFalse(composite.HasNext(ref composite), "Step 4: HasNext should be false after all items consumed.");
        }

        #endregion
    }
}
