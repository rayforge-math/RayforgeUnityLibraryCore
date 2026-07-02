using NUnit.Framework;
using Rayforge.Core.Collections.Abstractions.Tests;
using Rayforge.Core.Collections.Iterator;
using Rayforge.Core.TestEnv;
using System.Collections.Generic;
using Rayforge.Core.Collections.Abstractions;

namespace Rayforge.Core.Collections.Buffering.Tests
{
    [TestFixture(typeof(int))]
    [TestFixture(typeof(float))]
    [TestFixture(typeof(bool))]
    public class BufferSegmentStateTests<T> : IIterationLogicTests<BufferSegmentMeta<T>, BufferSegmentState<T>>
        where T : unmanaged
    {
        #region IIterationLogic Impl

        protected override IterationData<BufferSegmentMeta<T>, BufferSegmentState<T>> CreateLogic(int count)
        {
            T[] items = TestUtility.CreateSampleItems<T>(count);
            var logic = new BufferSegmentState<T>(items, 0, items.Length, 1);

            var expected = new BufferSegmentMeta<T>[count];
            for(int i = 0; i < items.Length; ++i)
            {
                expected[i] = new BufferSegmentMeta<T>
                {
                    Source = items,
                    Start = i,
                    Count = 1
                };
            }

            return new IterationData<BufferSegmentMeta<T>, BufferSegmentState<T>>
            {
                logic = logic,
                expected = expected
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
            // This tests if the state handles a "broken" internal struct safely.
            var state = new EnumeratorState<int, List<int>.Enumerator>(default);

            // Act & Assert: Should not throw on simple initialization
            Assert.DoesNotThrow(() => {
                bool hasNext = state.HasNext(ref state);
            }, "Initialization with default struct should not throw immediately.");
        }

        #endregion
    }
}
