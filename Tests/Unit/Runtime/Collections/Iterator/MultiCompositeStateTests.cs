using NUnit.Framework;
using Rayforge.Core.Collections.Abstractions;
using Rayforge.Core.Collections.Helpers;
using Rayforge.Core.Collections.Abstractions.Tests;
using System;

using Rayforge.Core.TestEnv;

namespace Rayforge.Core.Collections.Iterator.Tests
{
    [TestFixture(typeof(int))]
    [TestFixture(typeof(float))]
    [TestFixture(typeof(string))]
    public class MultiCompositeStateTests<T> : IIterationLogicTests<T, MultiCompositeState<T>>
    {
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
        public void Constructor_WithNullArray_HandlesGracefully()
        {
            // Act: Initialize with null should result in an empty state
            var state = new MultiCompositeState<T>(null);

            // Assert: HasNext should return false immediately
            Assert.IsFalse(state.HasNext(ref state), "A null source array should result in an exhausted iterator.");
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
        public void MoveNext_WithNullElementsInArray_SkipsToValidSource()
        {
            // Arrange: Create a valid MockLogic item and inject it into the composite
            var item = TestUtility.CreateSampleItems<T>(1)[0];
            var mockLogic = new MockLogic<T> { Items = new[] { item } };
            var iter = new Iterator<T, MockLogic<T>>(mockLogic);

            var state = new MultiCompositeState<T>(null, iter);

            // Act: Consume the first available element
            bool hasNext = state.MoveNext(ref state, out T result);

            // Assert: Should skip the null and find the item
            Assert.IsTrue(hasNext, "Should have found the valid item after skipping null.");
            Assert.AreEqual(item, result);
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

        #endregion
    }
}
