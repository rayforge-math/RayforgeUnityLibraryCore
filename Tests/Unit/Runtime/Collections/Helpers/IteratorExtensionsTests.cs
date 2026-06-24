using NUnit.Framework;
using Rayforge.Core.Collections.Abstractions;
using Rayforge.Core.Collections.Iterator;
using System;
using System.Collections.Generic;

namespace Rayforge.Core.Collections.Helpers.Tests
{
    [TestFixture]
    public class IteratorExtensionsTests
    {
        #region Structs

        private struct CustomMinimalEnumerator : IEnumerator<int>
        {
            public int Current => 0;
            object System.Collections.IEnumerator.Current => 0;
            public bool MoveNext() => false;
            public void Reset() { }
            public void Dispose() { }
        }

        #endregion

        #region Base Engine

        [Test]
        public void ToIterator_BaseEngine_ReturnsCorrectConcreteType()
        {
            // We use a specific struct enumerator to see if the engine 
            // maintains the generic type without boxing.
            var source = new CustomMinimalEnumerator();

            var iterator = source.ToIterator<int, CustomMinimalEnumerator>();

            Assert.IsInstanceOf<Iterator<int, EnumeratorState<int, CustomMinimalEnumerator>>>(iterator);
        }

        [Test]
        public void ToIterator_BaseEngine_HandlesDefaultStruct_WithoutThrowing()
        {
            // A default struct is the ultimate "nonsense" input for the base engine.
            // It must be handled safely (resulting in an empty, non-crashing iterator).
            CustomMinimalEnumerator defaultEnum = default;

            Assert.DoesNotThrow(() =>
            {
                var iterator = defaultEnum.ToIterator<int, CustomMinimalEnumerator>();
                Assert.IsFalse(iterator.MoveNext(), "Default struct iterator should yield no elements.");
            });
        }

        #endregion

        #region Array Tests

        [Test]
        public void ToIterator_Array_ReturnsCorrectType()
        {
            // Verify that the specialized array overload uses the efficient 
            // internal ArraySegment.Enumerator type to avoid boxing.
            int[] source = { 10, 20, 30 };

            var iterator = source.ToIterator();

            // Verification of the exact type chain: Iterator -> EnumeratorState -> ArraySegment.Enumerator
            Assert.IsInstanceOf<Iterator<int, EnumeratorState<int, ArraySegment<int>.Enumerator>>>(iterator);
        }

        [Test]
        public void ToIterator_Array_Null_ReturnsEmptyIterator_NoThrow()
        {
            // Essential nonsense test: A null array must not cause a NullReferenceException.
            // It should instead return a valid "Empty" iterator struct.
            int[] source = null;

            Assert.DoesNotThrow(() =>
            {
                var iterator = source.ToIterator();

                Assert.IsNotNull(iterator);
                Assert.IsFalse(iterator.MoveNext(), "Iterator for null array must be empty.");
            });
        }

        [Test]
        public void ToIterator_Array_Empty_WorksCorrectly()
        {
            // An empty array is not a null array. 
            // It should return a valid iterator that yields no elements.
            int[] source = Array.Empty<int>();

            var iterator = source.ToIterator();

            Assert.IsFalse(iterator.MoveNext(), "Iterator for empty array must not have elements.");
        }

        #endregion

        #region List Tests

        [Test]
        public void ToIterator_List_ReturnsCorrectType()
        {
            // Verify that the List-specific overload correctly maps to the 
            // internal List<T>.Enumerator struct for zero-allocation iteration.
            var list = new List<int> { 1, 2, 3 };
            var iterator = list.GetEnumerator().ToIterator();

            // Verification of the exact type chain: Iterator -> EnumeratorState -> List.Enumerator
            Assert.IsInstanceOf<Iterator<int, EnumeratorState<int, List<int>.Enumerator>>>(iterator);
        }

        [Test]
        public void ToIterator_List_EmptyCollection_ReturnsEmptyIterator()
        {
            // Ensure that a correctly initialized enumerator from an empty List
            // does not throw and simply returns false on the first MoveNext call.
            var emptyList = new List<int>();
            var iterator = emptyList.GetEnumerator().ToIterator();

            Assert.DoesNotThrow(() =>
            {
                Assert.IsNotNull(iterator);
                Assert.IsFalse(iterator.MoveNext(), "An empty List enumerator must yield no elements.");
            });
        }

        #endregion

        #region HashSet Tests

        [Test]
        public void ToIterator_HashSet_ReturnsCorrectType()
        {
            // Verify that the HashSet-specific overload correctly maps to the 
            // internal HashSet<T>.Enumerator struct for high-performance iteration.
            var set = new HashSet<string> { "item1", "item2" };
            var iterator = set.GetEnumerator().ToIterator();

            // Verification of the exact type chain: Iterator -> EnumeratorState -> HashSet.Enumerator
            Assert.IsInstanceOf<Iterator<string, EnumeratorState<string, HashSet<string>.Enumerator>>>(iterator);
        }

        [Test]
        public void ToIterator_HashSet_EmptyCollection_ReturnsEmptyIterator()
        {
            // Verify that an empty HashSet produces a valid iterator 
            // that safely reports no elements.
            var emptySet = new HashSet<string>();
            var iterator = emptySet.GetEnumerator().ToIterator();

            Assert.DoesNotThrow(() =>
            {
                Assert.IsFalse(iterator.MoveNext(), "An empty HashSet enumerator must yield no elements.");
            });
        }

        #endregion

        #region Queue Tests

        [Test]
        public void ToIterator_Queue_ReturnsCorrectType()
        {
            // Verify that the Queue-specific overload correctly maps to the 
            // internal Queue<T>.Enumerator struct to maintain stack-only allocation.
            var queue = new Queue<int>();
            queue.Enqueue(1);
            var iterator = queue.GetEnumerator().ToIterator();

            // Verification of the exact type chain: Iterator -> EnumeratorState -> Queue.Enumerator
            Assert.IsInstanceOf<Iterator<int, EnumeratorState<int, Queue<int>.Enumerator>>>(iterator);
        }

        [Test]
        public void ToIterator_Queue_EmptyCollection_ReturnsEmptyIterator()
        {
            // Verify that an empty Queue produces a valid iterator 
            // that safely reports no elements.
            var emptyQueue = new Queue<float>();
            var iterator = emptyQueue.GetEnumerator().ToIterator();

            Assert.DoesNotThrow(() =>
            {
                Assert.IsFalse(iterator.MoveNext(), "An empty Queue enumerator must yield no elements.");
            });
        }

        #endregion

        #region Stack Tests

        [Test]
        public void ToIterator_Stack_ReturnsCorrectType()
        {
            // Verify that the Stack-specific overload correctly maps to the 
            // internal Stack<T>.Enumerator struct for high-performance stack traversal.
            var stack = new Stack<int>();
            stack.Push(100);
            var iterator = stack.GetEnumerator().ToIterator();

            // Verification of the exact type chain: Iterator -> EnumeratorState -> Stack.Enumerator
            Assert.IsInstanceOf<Iterator<int, EnumeratorState<int, Stack<int>.Enumerator>>>(iterator);
        }

        [Test]
        public void ToIterator_Stack_EmptyCollection_ReturnsEmptyIterator()
        {
            // Verify that an empty Stack produces a valid iterator 
            // that safely reports no elements.
            var emptyStack = new Stack<byte>();
            var iterator = emptyStack.GetEnumerator().ToIterator();

            Assert.DoesNotThrow(() =>
            {
                Assert.IsFalse(iterator.MoveNext(), "An empty Stack enumerator must yield no elements.");
            });
        }

        #endregion

        #region Dictionary Overloads Tests

        // --- Full Dictionary (KeyValuePair) ---

        [Test]
        public void ToIterator_Dictionary_ReturnsCorrectType()
        {
            // Verify that the Dictionary enumerator correctly maps to the 
            // KeyValuePair struct iterator.
            var dict = new Dictionary<int, string> { { 1, "One" } };
            var iterator = dict.GetEnumerator().ToIterator();

            Assert.IsInstanceOf<Iterator<KeyValuePair<int, string>, EnumeratorState<KeyValuePair<int, string>, Dictionary<int, string>.Enumerator>>>(iterator);
        }

        [Test]
        public void ToIterator_Dictionary_EmptyCollection_ReturnsEmptyIterator()
        {
            // Verify that an empty Dictionary produces a valid iterator 
            // that safely reports no elements.
            var emptyDict = new Dictionary<int, int>();
            var iterator = emptyDict.GetEnumerator().ToIterator();

            Assert.DoesNotThrow(() =>
            {
                Assert.IsFalse(iterator.MoveNext(), "An empty Dictionary enumerator must yield no elements.");
            });
        }

        // --- Key Collection ---

        [Test]
        public void ToIterator_DictionaryKeys_ReturnsCorrectType()
        {
            // Verify that the Dictionary KeyCollection enumerator maintains its specific struct type.
            var dict = new Dictionary<int, string> { { 1, "One" } };
            var iterator = dict.Keys.GetEnumerator().ToIterator();

            Assert.IsInstanceOf<Iterator<int, EnumeratorState<int, Dictionary<int, string>.KeyCollection.Enumerator>>>(iterator);
        }

        [Test]
        public void ToIterator_DictionaryKeys_EmptyCollection_ReturnsEmptyIterator()
        {
            // Verify that the Keys collection of an empty Dictionary yields an empty iterator.
            var emptyDict = new Dictionary<string, int>();
            var iterator = emptyDict.Keys.GetEnumerator().ToIterator();

            Assert.DoesNotThrow(() =>
            {
                Assert.IsFalse(iterator.MoveNext(), "An empty Dictionary Keys enumerator must yield no elements.");
            });
        }

        // --- Value Collection ---

        [Test]
        public void ToIterator_DictionaryValues_ReturnsCorrectType()
        {
            // Verify that the Dictionary ValueCollection enumerator maintains its specific struct type.
            var dict = new Dictionary<int, string> { { 1, "One" } };
            var iterator = dict.Values.GetEnumerator().ToIterator();

            Assert.IsInstanceOf<Iterator<string, EnumeratorState<string, Dictionary<int, string>.ValueCollection.Enumerator>>>(iterator);
        }

        [Test]
        public void ToIterator_DictionaryValues_EmptyCollection_ReturnsEmptyIterator()
        {
            // Verify that the Values collection of an empty Dictionary yields an empty iterator.
            var emptyDict = new Dictionary<int, float>();
            var iterator = emptyDict.Values.GetEnumerator().ToIterator();

            Assert.DoesNotThrow(() =>
            {
                Assert.IsFalse(iterator.MoveNext(), "An empty Dictionary Values enumerator must yield no elements.");
            });
        }

        #endregion

        #region Specialized Overloads Tests

        // --- LinkedList ---

        [Test]
        public void ToIterator_LinkedList_ReturnsCorrectType()
        {
            // Verify that the LinkedList-specific overload correctly maps to the 
            // internal LinkedList<T>.Enumerator struct.
            var list = new LinkedList<int>();
            list.AddFirst(1);
            var iterator = list.GetEnumerator().ToIterator();

            // Verification of the exact type chain: Iterator -> EnumeratorState -> LinkedList.Enumerator
            Assert.IsInstanceOf<Iterator<int, EnumeratorState<int, LinkedList<int>.Enumerator>>>(iterator);
        }

        [Test]
        public void ToIterator_LinkedList_EmptyCollection_ReturnsEmptyIterator()
        {
            // Verify that an empty LinkedList produces a valid iterator 
            // that safely reports no elements.
            var emptyList = new LinkedList<string>();
            var iterator = emptyList.GetEnumerator().ToIterator();

            Assert.DoesNotThrow(() =>
            {
                Assert.IsFalse(iterator.MoveNext(), "An empty LinkedList enumerator must yield no elements.");
            });
        }

        // --- SortedSet ---

        [Test]
        public void ToIterator_SortedSet_ReturnsCorrectType()
        {
            // Verify that the SortedSet-specific overload correctly maps to the 
            // internal SortedSet<T>.Enumerator struct.
            var set = new SortedSet<int> { 1, 2, 3 };
            var iterator = set.GetEnumerator().ToIterator();

            // Verification of the exact type chain: Iterator -> EnumeratorState -> SortedSet.Enumerator
            Assert.IsInstanceOf<Iterator<int, EnumeratorState<int, SortedSet<int>.Enumerator>>>(iterator);
        }

        [Test]
        public void ToIterator_SortedSet_EmptyCollection_ReturnsEmptyIterator()
        {
            // Verify that an empty SortedSet produces a valid iterator 
            // that safely reports no elements.
            var emptySet = new SortedSet<float>();
            var iterator = emptySet.GetEnumerator().ToIterator();

            Assert.DoesNotThrow(() =>
            {
                Assert.IsFalse(iterator.MoveNext(), "An empty SortedSet enumerator must yield no elements.");
            });
        }

        #endregion

        #region Composite & Utility Tests

        [Test]
        public void Combine_NullInput_YieldsDormantIterator()
        {
            // Verifies that a null array results in a safe, non-throwing iterator
            var iterator = IteratorExtensions.Combine<int>(null);

            Assert.IsFalse(iterator.MoveNext(), "Iterator created from null sources must be immediately exhausted.");
        }

        [Test]
        public void Combine_EmptyInput_YieldsDormantIterator()
        {
            // Verifies that an empty array results in a safe, non-throwing iterator
            var iterator = IteratorExtensions.Combine(new IIterator<int>[0]);

            Assert.IsFalse(iterator.MoveNext(), "Iterator created from empty sources must be immediately exhausted.");
        }

        [Test]
        public void Combine_AllInvalidSources_IsSafeToIterate()
        {
            // Verify that nulls and Empty-iterators are effectively ignored
            var iterator = IteratorExtensions.Combine<int>(null, IIterator<int>.Empty(), null);

            Assert.IsFalse(iterator.MoveNext(), "Iterator with only invalid sources must be empty.");
        }

        #endregion
    }
}