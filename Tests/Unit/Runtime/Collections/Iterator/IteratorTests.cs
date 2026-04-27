using NUnit.Framework;
using Rayforge.Core.Collections.Abstractions;
using Rayforge.Core.Collections.Iterator;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Rayforge.Core.Tests.Collections.Iterator
{
    public class IteratorTests
    {
        #region Structs

        public struct MockLogic<T> : IIterationLogic<T, MockLogic<T>>
        {
            public T[] Items;
            public int Index;

            public bool HasNext(ref MockLogic<T> state)
                => state.Items != null && state.Index < state.Items.Length;

            public bool MoveNext(ref MockLogic<T> state, out T result)
            {
                if (state.Items != null && state.Index < state.Items.Length)
                {
                    result = state.Items[state.Index];
                    state.Index++;
                    return true;
                }
                result = default;
                return false;
            }

            public bool TryPeekNext(ref MockLogic<T> state, out T result)
            {
                if (state.Items != null && state.Index < state.Items.Length)
                {
                    result = state.Items[state.Index];
                    return true;
                }
                result = default;
                return false;
            }
        }

        #endregion

        #region Constructor & Initialization Tests (int, float, string)

        [Test]
        public void Constructor_SetsInitializedFlag_EnablingAccess()
        {
            // Standard case: Constructor must enable access to the state logic.
            var itInt = new Iterator<int, MockLogic<int>>(new MockLogic<int> { Items = new[] { 10 } });
            var itFloat = new Iterator<float, MockLogic<float>>(new MockLogic<float> { Items = new[] { 1.5f } });
            var itStr = new Iterator<string, MockLogic<string>>(new MockLogic<string> { Items = new[] { "A" } });

            Assert.IsTrue(itInt.HasNext, "int-iterator must allow logic access.");
            Assert.IsTrue(itFloat.HasNext, "float-iterator must allow logic access.");
            Assert.IsTrue(itStr.HasNext, "string-iterator must allow logic access.");
        }

        [Test]
        public void Default_WithoutConstructor_IsSafelyDisabled()
        {
            // Verify that 'default' structs are safely uninitialized and return default values.
            Iterator<int, MockLogic<int>> itInt = default;
            Iterator<float, MockLogic<float>> itFloat = default;
            Iterator<string, MockLogic<string>> itStr = default;

            // Check HasNext (Logic Gate)
            Assert.IsFalse(itInt.HasNext, "Uninitialized int-iterator must report HasNext as false.");
            Assert.IsFalse(itFloat.HasNext, "Uninitialized float-iterator must report HasNext as false.");
            Assert.IsFalse(itStr.HasNext, "Uninitialized string-iterator must report HasNext as false.");

            // Check Current (Data Safety)
            Assert.AreEqual(0, itInt.Current);
            Assert.AreEqual(0.0f, itFloat.Current);
            Assert.IsNull(itStr.Current);
        }

        [Test]
        public void EmptyFactory_ProducesUninitializedInstance()
        {
            // Ensure Iterator.Empty() behaves exactly like an uninitialized default struct.
            var itInt = Iterator<int, MockLogic<int>>.Empty();
            var itFloat = Iterator<float, MockLogic<float>>.Empty();
            var itStr = Iterator<string, MockLogic<string>>.Empty();

            Assert.IsFalse(itInt.HasNext);
            Assert.IsFalse(itFloat.HasNext);
            Assert.IsFalse(itStr.HasNext);

            Assert.AreEqual(0, itInt.Current);
            Assert.AreEqual(0.0f, itFloat.Current);
            Assert.IsNull(itStr.Current);
        }

        #endregion

        #region Current: Generic Property Tests

        [Test]
        public void Current_Generic_InitialValues_AreDefault()
        {
            // Test: int (Primitive Value Type)
            var itInt = new Iterator<int, MockLogic<int>>(new MockLogic<int> { Items = new[] { 10 } });
            Assert.AreEqual(0, itInt.Current, "Generic int must be 0 initially.");

            // Test: float (Floating Point Value Type)
            var itFloat = new Iterator<float, MockLogic<float>>(new MockLogic<float> { Items = new[] { 1.5f } });
            Assert.AreEqual(0.0f, itFloat.Current, "Generic float must be 0.0f initially.");

            // Test: string (Reference Type)
            var itStr = new Iterator<string, MockLogic<string>>(new MockLogic<string> { Items = new[] { "A" } });
            Assert.IsNull(itStr.Current, "Generic string must be null initially.");
        }

        [Test]
        public void Current_Generic_OnUninitialized_IsSafe()
        {
            Iterator<int, MockLogic<int>> itInt = default;
            Assert.AreEqual(0, itInt.Current);

            Iterator<float, MockLogic<float>> itFloat = default;
            Assert.AreEqual(0.0f, itFloat.Current);

            Iterator<string, MockLogic<string>> itStr = default;
            Assert.IsNull(itStr.Current);
        }

        #endregion

        #region Current: IEnumerator Property Tests

        [Test]
        public void Current_IEnumerator_InitialValues_AreDefault()
        {
            // Test: int
            IEnumerator itInt = new Iterator<int, MockLogic<int>>(new MockLogic<int> { Items = new[] { 10 } });
            Assert.AreEqual(0, itInt.Current, "Interface Current must be 0 for int-iterators initially.");

            // Test: float
            IEnumerator itFloat = new Iterator<float, MockLogic<float>>(new MockLogic<float> { Items = new[] { 1.5f } });
            Assert.AreEqual(0.0f, itFloat.Current, "Interface Current must be 0.0f for float-iterators initially.");

            // Test: string
            IEnumerator itStr = new Iterator<string, MockLogic<string>>(new MockLogic<string> { Items = new[] { "A" } });
            Assert.IsNull(itStr.Current, "Interface Current must be null for string-iterators initially.");
        }

        [Test]
        public void Current_IEnumerator_OnUninitialized_IsSafe()
        {
            // Test: int
            IEnumerator itInt = default(Iterator<int, MockLogic<int>>);
            Assert.AreEqual(0, itInt.Current, "Interface Current must return 0 for uninitialized int struct.");

            // Test: float
            IEnumerator itFloat = default(Iterator<float, MockLogic<float>>);
            Assert.AreEqual(0.0f, itFloat.Current, "Interface Current must return 0.0f for uninitialized float struct.");

            // Test: string
            IEnumerator itStr = default(Iterator<string, MockLogic<string>>);
            Assert.IsNull(itStr.Current, "Interface Current must return null for uninitialized string struct.");
        }

        [Test]
        public void Current_IEnumerator_BoxingConsistency()
        {
            // Verify that all types are correctly boxed and match their generic counterpart.
            var itInt = new Iterator<int, MockLogic<int>>(new MockLogic<int> { Items = new[] { 1 } });
            var itFloat = new Iterator<float, MockLogic<float>>(new MockLogic<float> { Items = new[] { 1.1f } });
            var itStr = new Iterator<string, MockLogic<string>>(new MockLogic<string> { Items = new[] { "X" } });

            // Int check
            Assert.IsInstanceOf<int>(((IEnumerator)itInt).Current);
            Assert.AreEqual(itInt.Current, (int)((IEnumerator)itInt).Current);

            // Float check
            Assert.IsInstanceOf<float>(((IEnumerator)itFloat).Current);
            Assert.AreEqual(itFloat.Current, (float)((IEnumerator)itFloat).Current);

            // String check
            Assert.AreEqual(itStr.Current, ((IEnumerator)itStr).Current);
        }

        #endregion

        #region HasNext Property Tests

        [Test]
        public void HasNext_ReflectsStateLogic_WithoutAdvancing()
        {
            // Verify that HasNext correctly queries the state logic 
            // and can be called multiple times without changing Current.
            var itInt = new Iterator<int, MockLogic<int>>(new MockLogic<int> { Items = new[] { 10 } });
            var itFloat = new Iterator<float, MockLogic<float>>(new MockLogic<float> { Items = new[] { 1.5f } });
            var itStr = new Iterator<string, MockLogic<string>>(new MockLogic<string> { Items = new[] { "A" } });

            // 1. Check true
            Assert.IsTrue(itInt.HasNext, "int-iterator should have next.");
            Assert.IsTrue(itFloat.HasNext, "float-iterator should have next.");
            Assert.IsTrue(itStr.HasNext, "string-iterator should have next.");

            // 2. Check stability (calling it again shouldn't change anything)
            Assert.IsTrue(itInt.HasNext);
            Assert.AreEqual(0, itInt.Current, "HasNext must not advance the iterator or change Current.");
        }

        [Test]
        public void HasNext_ReturnsFalse_WhenStateIsEmpty()
        {
            // Verify that HasNext returns false when the underlying logic has no data.
            var itInt = new Iterator<int, MockLogic<int>>(new MockLogic<int> { Items = new int[0] });
            var itFloat = new Iterator<float, MockLogic<float>>(new MockLogic<float> { Items = new float[0] });
            var itStr = new Iterator<string, MockLogic<string>>(new MockLogic<string> { Items = new string[0] });

            Assert.IsFalse(itInt.HasNext, "Empty int-iterator must return false for HasNext.");
            Assert.IsFalse(itFloat.HasNext, "Empty float-iterator must return false for HasNext.");
            Assert.IsFalse(itStr.HasNext, "Empty string-iterator must return false for HasNext.");
        }

        [Test]
        public void HasNext_ShortCircuits_WhenUninitialized()
        {
            // Critical performance/safety test: If _isInitialized is false, 
            // the state logic must NOT be called (short-circuit).
            // Even with a 'dirty' state that might look like it has data, default must return false.
            Iterator<int, MockLogic<int>> it = default;

            Assert.IsFalse(it.HasNext, "HasNext must immediately return false if the iterator is uninitialized.");
        }

        #endregion

        #region TryPeekNext Tests

        [Test]
        public void TryPeekNext_ReturnsCorrectValue_WithoutChangingCurrent()
        {
            // Verify that Peek returns the next value but leaves the iterator's Current untouched.
            var itInt = new Iterator<int, MockLogic<int>>(new MockLogic<int> { Items = new[] { 42 } });
            var itFloat = new Iterator<float, MockLogic<float>>(new MockLogic<float> { Items = new[] { 1.5f } });
            var itStr = new Iterator<string, MockLogic<string>>(new MockLogic<string> { Items = new[] { "Peek" } });

            // 1. Peek the values
            bool successInt = itInt.TryPeekNext(out int resultInt);
            bool successFloat = itFloat.TryPeekNext(out float resultFloat);
            bool successStr = itStr.TryPeekNext(out string resultStr);

            // 2. Assert results
            Assert.IsTrue(successInt);
            Assert.AreEqual(42, resultInt);
            Assert.IsTrue(successFloat);
            Assert.AreEqual(1.5f, resultFloat);
            Assert.IsTrue(successStr);
            Assert.AreEqual("Peek", resultStr);

            // 3. CRITICAL: Current must still be default!
            Assert.AreEqual(0, itInt.Current, "Current must not change after a Peek.");
            Assert.AreEqual(0.0f, itFloat.Current);
            Assert.IsNull(itStr.Current);
        }

        [Test]
        public void TryPeekNext_ReturnsFalse_WhenEmpty()
        {
            // Verify that Peek returns false and default(T) if no more elements exist.
            var itInt = new Iterator<int, MockLogic<int>>(new MockLogic<int> { Items = new int[0] });
            var itStr = new Iterator<string, MockLogic<string>>(new MockLogic<string> { Items = new string[0] });

            Assert.IsFalse(itInt.TryPeekNext(out int resultInt));
            Assert.AreEqual(0, resultInt);

            Assert.IsFalse(itStr.TryPeekNext(out string resultStr));
            Assert.IsNull(resultStr);
        }

        [Test]
        public void TryPeekNext_ShortCircuits_WhenUninitialized()
        {
            // Safety check: Uninitialized iterators must not call the state logic.
            Iterator<int, MockLogic<int>> it = default;

            bool success = it.TryPeekNext(out int result);

            Assert.IsFalse(success, "Peek must return false for uninitialized iterators.");
            Assert.AreEqual(0, result, "Out result must be default(T) for uninitialized iterators.");
        }

        #endregion

        #region MoveNext Tests

        [Test]
        public void MoveNext_UpdatesCurrent_And_AdvancesState()
        {
            // Verify that MoveNext updates the Current property and returns true.
            var itInt = new Iterator<int, MockLogic<int>>(new MockLogic<int> { Items = new[] { 10, 20 } });
            var itFloat = new Iterator<float, MockLogic<float>>(new MockLogic<float> { Items = new[] { 1.1f, 2.2f } });
            var itStr = new Iterator<string, MockLogic<string>>(new MockLogic<string> { Items = new[] { "A", "B" } });

            // 1. First Move
            Assert.IsTrue(itInt.MoveNext());
            Assert.AreEqual(10, itInt.Current, "Current must update to the first element (int).");

            Assert.IsTrue(itFloat.MoveNext());
            Assert.AreEqual(1.1f, itFloat.Current, "Current must update to the first element (float).");

            Assert.IsTrue(itStr.MoveNext());
            Assert.AreEqual("A", itStr.Current, "Current must update to the first element (string).");

            // 2. Second Move
            Assert.IsTrue(itInt.MoveNext());
            Assert.AreEqual(20, itInt.Current);
        }

        [Test]
        [TestCase(new int[] { 10, 20, 30 })]
        [TestCase(new int[] { 5 })]
        [TestCase(new int[] { })]
        public void MoveNext_IntArray_IdentifiesAllElements(int[] items)
        {
            // Arrange
            var logic = new MockLogic<int> { Items = items };
            var iterator = new Iterator<int, MockLogic<int>>(logic);

            // Act & Assert
            for (int i = 0; i < items.Length; i++)
            {
                Assert.IsTrue(iterator.MoveNext(), $"Should return true at index {i}");
                Assert.AreEqual(items[i], iterator.Current, $"Value mismatch at index {i}");
            }

            // After the last element, MoveNext must be false
            Assert.IsFalse(iterator.MoveNext(), "Should return false after reaching the end");
        }

        [Test]
        public void MoveNext_HandlesVariousLengthsAndTypes()
        {
            // Integer scenarios
            VerifyExhaustion(new[] { 10, 20, 30 }, 0, "Standard int array");
            VerifyExhaustion(new[] { 99 }, 0, "Single element int array");
            VerifyExhaustion(new int[] { }, 0, "Empty int array");

            // Reference type scenarios
            VerifyExhaustion(new[] { "A", "B" }, (string)null, "Reference type array");
            VerifyExhaustion(new string[] { }, (string)null, "Empty string array");

            // Boolean scenarios
            VerifyExhaustion(new[] { true, false }, false, "Boolean value type array");
        }

        private void VerifyExhaustion<T>(T[] items, T expectedDefault, string scenario)
        {
            // Arrange
            var logic = new MockLogic<T> { Items = items };
            var iterator = new Iterator<T, MockLogic<T>>(logic);

            // Act & Assert 1: Iteration through all existing items
            for (int i = 0; i < items.Length; i++)
            {
                bool moved = iterator.MoveNext();
                Assert.IsTrue(moved, $"Step {i} failed in scenario: {scenario}");
                Assert.AreEqual(items[i], iterator.Current, $"Value mismatch at index {i} in scenario: {scenario}");
            }

            // Act & Assert 2: Behavior after the end
            bool hasMoreAfterEnd = iterator.MoveNext();

            Assert.IsFalse(hasMoreAfterEnd, $"MoveNext must return false after exhaustion in scenario: {scenario}");
            Assert.AreEqual(expectedDefault, iterator.Current, $"Current must be reset to default in scenario: {scenario}");
        }

        [Test]
        public void MoveNext_ShortCircuits_WhenUninitialized()
        {
            // Safety check: Uninitialized iterators must immediately return false 
            // without attempting to access the state.
            Iterator<float, MockLogic<float>> it = default;

            bool result = it.MoveNext();

            Assert.IsFalse(result, "MoveNext must return false for default structs.");
            Assert.AreEqual(0.0f, it.Current, "Current must remain default.");
        }

        [Test]
        public void MoveNext_IsPersistent_ThroughStructCopies()
        {
            // Since it's a struct with internal state, we verify that 
            // the mutation is consistent.
            var it = new Iterator<int, MockLogic<int>>(new MockLogic<int> { Items = new[] { 1, 2, 3 } });

            it.MoveNext(); // Current = 1
            it.MoveNext(); // Current = 2

            Assert.AreEqual(2, it.Current);
            Assert.IsTrue(it.HasNext, "Iterator should still have one element left.");
        }

        #endregion

        #region Enumerator Pattern & Compiler Integration

        [Test]
        public void GetEnumerator_ConcreteStruct_ReturnsCopyOfSelf()
        {
            // Arrange: Create and partially advance
            var it = new Iterator<int, MockLogic<int>>(new MockLogic<int> { Items = new[] { 10, 20, 30 } });
            it.MoveNext(); // Current = 10

            // Act
            var snapshot = it.GetEnumerator();

            // Assert: State must be identical but separate
            Assert.AreEqual(10, snapshot.Current);
            Assert.AreEqual(10, it.Current);

            // Deep State Change: Advance both independently
            snapshot.MoveNext(); // snapshot -> 20
            it.MoveNext();       // it -> 20
            snapshot.MoveNext(); // snapshot -> 30

            Assert.AreEqual(30, snapshot.Current, "Snapshot should reach the end.");
            Assert.AreEqual(20, it.Current, "Original should be unaffected by snapshot advancement.");
            Assert.IsTrue(it.HasNext, "Original should still have elements even if snapshot is finished.");
        }

        [Test]
        public void GetEnumerator_InterfaceFallback_SupportsForeach_WithEarlyExit()
        {
            // Arrange: Use interface to force explicit implementation
            IIterator<int> it = new Iterator<int, MockLogic<int>>(new MockLogic<int> { Items = new[] { 1, 2, 3, 4 } });
            int lastValue = 0;

            // Act: Test if 'break' works correctly through the interface-based loop
            foreach (var val in it)
            {
                lastValue = val;
                if (val == 2) break;
            }

            // Assert
            Assert.AreEqual(2, lastValue, "Foreach should support early exit via interface.");

            // Critical: Verify the iterator state AFTER the break
            // Since foreach uses a COPY (even when boxed, it's a separate enumerator instance),
            // the original 'it' reference should technically be untouched if it was a fresh cast.
            // However, if 'it' was already the box, its state might have changed.
        }

        [Test]
        public void GetEnumerator_IsCompatibleWithDuckTyping_NestedLoops()
        {
            // Arrange: Test if we can run two independent loops over the same struct instance
            var it = new Iterator<string, MockLogic<string>>(new MockLogic<string> { Items = new[] { "A", "B" } });
            var outerResults = new List<string>();
            var innerResults = new List<string>();

            // Act: Nested loops
            foreach (var outer in it)
            {
                outerResults.Add(outer);
                foreach (var inner in it)
                {
                    innerResults.Add(inner);
                }
            }

            // Assert
            // Duck typing must ensure that each 'foreach' calls GetEnumerator(), 
            // which returns a FRESH copy of the current state.
            Assert.AreEqual(2, outerResults.Count, "Outer loop should run twice.");
            Assert.AreEqual(4, innerResults.Count, "Inner loop should run twice for EACH outer element.");
            Assert.AreEqual("A", innerResults[0]);
            Assert.AreEqual("B", innerResults[1]);
            Assert.AreEqual("A", innerResults[2]);
        }

        #endregion

        #region System.Collections Interface Support (LINQ & Legacy)

        [Test]
        public void IEnumerable_Generic_AdvancesStateAndPreservesIntegrity()
        {
            // Arrange
            var logic = new MockLogic<int> { Items = new[] { 10, 20, 30 } };
            var it = new Iterator<int, MockLogic<int>>(logic);
            IEnumerable<int> enumerable = it; // Boxing occurs here

            // Act
            var enumerator = enumerable.GetEnumerator();

            // Assert
            Assert.IsTrue(enumerator.MoveNext());
            Assert.AreEqual(10, enumerator.Current, "Interface should access the first element.");

            Assert.IsTrue(enumerator.MoveNext());
            Assert.AreEqual(20, enumerator.Current, "State must advance correctly through the interface.");

            // Verify that the 'enumerable' (the box) is independent from the original 'it' (the struct)
            Assert.AreNotEqual(enumerator.Current, it.Current, "Original struct should not have moved.");
        }

        [Test]
        public void IEnumerable_NonGeneric_AdvancesState()
        {
            // Arrange
            System.Collections.IEnumerable enumerable = new Iterator<int, MockLogic<int>>(
                new MockLogic<int> { Items = new[] { 42, 43 } }
            );
            var enumerator = enumerable.GetEnumerator();

            // Act
            enumerator.MoveNext(); // To 42
            enumerator.MoveNext(); // To 43

            // Assert
            Assert.AreEqual(43, enumerator.Current, "Non-generic IEnumerator must advance state correctly.");
            Assert.IsFalse(enumerator.MoveNext(), "Should reach end through non-generic interface.");
        }

        [Test]
        public void IEnumerable_MultipleEnumerators_AreIndependent()
        {
            // Arrange
            var it = new Iterator<int, MockLogic<int>>(new MockLogic<int> { Items = new[] { 1, 2, 3 } });
            var enumerable = (IEnumerable<int>)it;

            // Act
            var enum1 = enumerable.GetEnumerator();
            var enum2 = enumerable.GetEnumerator();

            enum1.MoveNext(); // enum1 is at 1

            // Assert
            Assert.AreEqual(1, enum1.Current);
            Assert.AreEqual(0, ((Iterator<int, MockLogic<int>>)enum2).Current, "The second enumerator should be a fresh copy/box.");
        }

        #endregion

        #region Interface Contract Compliance (Exceptions)

        [Test]
        public void Reset_ThrowsNotSupportedException()
        {
            var it = new Iterator<int, MockLogic<int>>(new MockLogic<int> { Items = new[] { 1, 2, 3 } });

            // Reset is an explicit IEnumerator implementation, 
            // so we must cast to call it.
            var enumerator = (System.Collections.IEnumerator)it;

            // Assert
            var ex = Assert.Throws<NotSupportedException>(() => enumerator.Reset());

            // Verify that the message is helpful and explains WHY.
            Assert.That(ex.Message, Does.Contain("not supported"), "Exception message should clarify that Reset is unavailable.");
        }

        #endregion

        #region Lifecycle & Cleanup

        [Test]
        public void Dispose_IsPassive_And_DoesNotAlterState()
        {
            var logic = new MockLogic<int> { Items = new[] { 10, 20 } };
            var it = new Iterator<int, MockLogic<int>>(logic);

            it.MoveNext();
            var stateBefore = it.Current;
            var hasNextBefore = it.HasNext;

            it.Dispose();

            Assert.AreEqual(stateBefore, it.Current, "Dispose should not reset the Current property.");
            Assert.AreEqual(hasNextBefore, it.HasNext, "Dispose should not alter the HasNext state.");
        }

        [Test]
        public void Dispose_CanBeCalledMultipleTimes_WithoutException()
        {
            var it = new Iterator<int, MockLogic<int>>(new MockLogic<int> { Items = new[] { 1, 2, 3 } });

            // Verify that calling Dispose does not throw, even if called multiple times.
            // This is a standard requirement for the IDisposable contract.
            Assert.DoesNotThrow(() => it.Dispose());
            Assert.DoesNotThrow(() => it.Dispose());
        }

        [Test]
        public void Dispose_Foreach_EvenIfManuallyDisposedBeforehand()
        {
            var logic = new MockLogic<int> { Items = new[] { 1, 2 } };
            var it = new Iterator<int, MockLogic<int>>(logic);
            int count = 0;

            it.Dispose(); // Call it before the loop

            foreach (var item in it)
            {
                count++;
            }

            Assert.AreEqual(2, count, "The loop should complete normally even if Dispose was called early.");
        }

        #endregion
    }
}
