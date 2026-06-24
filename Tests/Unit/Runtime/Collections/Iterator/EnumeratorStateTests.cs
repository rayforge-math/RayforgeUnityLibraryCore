using Rayforge.Core.Collections.Abstractions.Tests;
using NUnit.Framework;
using System.Collections.Generic;
using System.Linq;
using Rayforge.Core.TestEnv;

namespace Rayforge.Core.Collections.Iterator.Tests
{
    [TestFixture(typeof(int))]
    [TestFixture(typeof(float))]
    [TestFixture(typeof(string))]
    public class EnumeratorStateTests<T> : IIterationLogicTests<T, EnumeratorState<T, List<T>.Enumerator>>
    {
        #region IIterationLogic Impl

        protected override IterationData<T, EnumeratorState<T, List<T>.Enumerator>> CreateLogic(int count)
        {
            T[] items = TestUtility.CreateSampleItems<T>(count);
            var list = items.ToList();
            var logic = new EnumeratorState<T, List<T>.Enumerator>(list.GetEnumerator());
            return new IterationData<T, EnumeratorState<T, List<T>.Enumerator>>
            {
                logic = logic,
                expected = items
            };
        }

        #endregion

        #region Constructor

        [Test]
        public void Constructor_WithValidListEnumerator_InitializesCorrectly()
        {
            // Arrange: Create an empty list
            var list = new List<int>();
            var state = new EnumeratorState<int, List<int>.Enumerator>(list.GetEnumerator());

            // Act: Check availability
            // Even if it was just created, it should correctly report that it has no items.
            bool hasNext = state.HasNext(ref state);

            // Assert: Contract check
            Assert.IsFalse(hasNext, "A valid but empty enumerator should not have next elements.");
        }

        [Test]
        public void Constructor_WithEmptyEnumerator_StartsInReadyState()
        {
            var list = new List<int>();
            var enumerator = list.GetEnumerator();

            var state = new EnumeratorState<int, List<int>.Enumerator>(enumerator);

            // Even for an empty list, the state itself is valid; 
            // the first MoveNext/HasNext will trigger exhaustion.
            Assert.IsFalse(state.HasNext(ref state), "State should be initialized even if the source is empty.");
        }

        // Test for potential "null" enumerator scenario (if TEnumerator is a struct that can be default)
        [Test]
        public void Constructor_WithDefaultStructEnumerator_HandlesGracefully()
        {
            // default(List<int>.Enumerator) is a valid struct, 
            // but typically throws on MoveNext/Current. 
            // This tests if our state handles a "broken" internal struct safely.
            var state = new EnumeratorState<int, List<int>.Enumerator>(default);

            // Act & Assert: Should not throw on simple initialization
            Assert.DoesNotThrow(() => {
                bool hasNext = state.HasNext(ref state);
            }, "Initialization with default struct should not throw immediately.");
        }

        #endregion
    }
}
