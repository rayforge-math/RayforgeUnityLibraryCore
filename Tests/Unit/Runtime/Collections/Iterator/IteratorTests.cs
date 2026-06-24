using NUnit.Framework;
using Rayforge.Core.Collections.Abstractions;
using Rayforge.Core.Collections.Abstractions.Tests;
using Rayforge.Core.Collections.Helpers;
using System;
using System.Collections.Generic;

namespace Rayforge.Core.Collections.Iterator.Tests
{
    #region Structs
    
    
    
    #endregion

    [TestFixture(typeof(int))]
    [TestFixture(typeof(float))]
    [TestFixture(typeof(string))]
    public class IteratorTests<T> : IIterationLogicTests<T, MockLogic<T>>
    {
        #region IIterationLogic Impl

        protected override IterationData<T, MockLogic<T>> CreateLogic(int count)
            => CreateDefaultMockLogic(count);

        #endregion

        #region Constructor & Initialization Tests

        [Test]
        public void Constructor_SetsInitializedFlag_EnablingAccess()
        {
            // Standard case: Constructor must enable access to the state logic.
            // We initialize with 1 element to ensure the logic state has valid data.
            var data = CreateLogic(1);
            var logic = data.logic;
            var it = new Iterator<T, MockLogic<T>>(logic);

            // Assert
            Assert.IsTrue(it.HasNext,
                $"{typeof(T).Name}-iterator must allow logic access after constructor initialization.");
        }

        [Test]
        public void Default_WithoutConstructor_IsSafelyDisabled()
        {
            // Verify that 'default' structs are safely uninitialized and return default values.
            Iterator<T, MockLogic<T>> it = default;

            // Check HasNext (Logic Gate)
            Assert.IsFalse(it.HasNext, $"Uninitialized {typeof(T).Name}-iterator must report HasNext as false.");

            // Check Current (Data Safety)
            Assert.AreEqual(default(T), it.Current, $"Uninitialized {typeof(T).Name}-iterator must return default(T) for Current.");
        }

        [Test]
        public void EmptyFactory_ProducesUninitializedInstance()
        {
            // Ensure Iterator.Empty() behaves exactly like an uninitialized default struct.
            var it = Iterator<T, MockLogic<T>>.Empty();

            Assert.IsFalse(it.HasNext, $"Iterator.Empty<{typeof(T).Name}>() must be disabled.");
            Assert.AreEqual(default(T), it.Current, $"Iterator.Empty<{typeof(T).Name}>() must return default(T).");
        }

        #endregion

        #region Current: Generic Property Tests

        [Test]
        public void Current_Generic_InitialValues_AreDefault()
        {
            // Test: Verify that Current returns the default value of T before MoveNext is called.
            // We initialize with 1 element so the logic is in a valid starting state.
            var data = CreateLogic(1);
            var logic = data.logic;
            var it = new Iterator<T, MockLogic<T>>(logic);

            // Assert
            Assert.AreEqual(default(T), it.Current,
                $"Generic {typeof(T).Name} must be {default(T)} initially (before first MoveNext).");
        }

        [Test]
        public void Current_Generic_OnUninitialized_IsSafe()
        {
            // Safety check: Even if the struct was never initialized via constructor, 
            // accessing Current must not throw and should return default(T).
            Iterator<T, MockLogic<T>> it = default;

            Assert.AreEqual(default(T), it.Current,
                $"Uninitialized {typeof(T).Name}-iterator must return {default(T)} safely.");
        }

        #endregion

        #region Current: IEnumerator Property Tests

        [Test]
        public void Current_IEnumerator_InitialValues_AreDefault()
        {
            // Scenario: Verify that the non-generic IEnumerator.Current property
            // returns default(T) before any calls to MoveNext() are made.
            var data = CreateLogic(1);
            var logic = data.logic;
            System.Collections.IEnumerator it = new Iterator<T, MockLogic<T>>(logic);

            // Assert: The interface implementation of Current returns 'object', 
            // which should be a boxed version of default(T).
            Assert.AreEqual(default(T), it.Current,
                $"The non-generic Current must be {default(T)} for {typeof(T).Name}-iterators initially.");
        }

        [Test]
        public void Current_IEnumerator_OnUninitialized_IsSafe()
        {
            // Safety check: Explicitly cast default struct to IEnumerator to check interface implementation.
            System.Collections.IEnumerator it = default(Iterator<T, MockLogic<T>>);

            Assert.AreEqual(default(T), it.Current,
                $"Interface Current must return {default(T)} for uninitialized {typeof(T).Name} struct.");
        }

        [Test]
        public void Current_IEnumerator_BoxingConsistency()
        {
            // Scenario: Verify that the non-generic IEnumerator.Current correctly boxes the value T.
            var data = CreateLogic(1);
            var logic = data.logic;
            var expected = data.expected;
            var it = new Iterator<T, MockLogic<T>>(logic);

            // 1. Cast to the interface (This creates a boxed copy)
            var interfaceIt = (System.Collections.IEnumerator)it;

            // 2. Act: Advance the BOXED instance, not the local struct
            bool moveSuccess = interfaceIt.MoveNext();
            Assert.IsTrue(moveSuccess, "MoveNext should succeed via the interface.");

            // 3. Assert: Check the boxed current value
            object boxedCurrent = interfaceIt.Current;

            // Verify type safety
            Assert.IsInstanceOf<T>(boxedCurrent,
                $"The boxed Current must be an instance of {typeof(T).Name}.");

            // Verify value consistency against ground truth
            Assert.AreEqual(expected[0], (T)boxedCurrent,
                "The boxed value must match the ground truth data.");
        }

        #endregion
        
        #region HasNext Property Tests

        [Test]
        public void HasNext_ReflectsStateLogic_WithoutAdvancing()
        {
            // Scenario: Verify that HasNext correctly queries the state logic 
            // and can be called multiple times without changing the cursor or Current value.
            var data = CreateLogic(1);
            var logic = data.logic;
            var expected = data.expected;
            var it = new Iterator<T, MockLogic<T>>(logic);

            // 1. Initial State: Should be true because we have 1 element
            Assert.IsTrue(it.HasNext, $"{typeof(T).Name}-iterator should have next initially.");

            // 2. Check Stability: Calling it again shouldn't change anything
            Assert.IsTrue(it.HasNext, "HasNext must be idempotent (return the same result on multiple calls).");

            // 3. Verify No Side Effects: Current must still be default because MoveNext hasn't been called
            Assert.AreEqual(default(T), it.Current, "HasNext must not advance the iterator or change the Current property.");
        }

        [Test]
        public void HasNext_ReturnsFalse_WhenStateIsEmpty()
        {
            // Scenario: Verify that HasNext correctly reports false when 
            // the underlying logic is initialized with zero elements.
            var data = CreateLogic(0);
            var logic = data.logic;
            var it = new Iterator<T, MockLogic<T>>(logic);

            // Assert
            Assert.IsFalse(it.HasNext,
                $"Empty {typeof(T).Name}-iterator must return false for HasNext immediately.");

            // Verify Current remains default
            Assert.AreEqual(default(T), it.Current,
                "Current must be default(T) for an empty iterator.");
        }

        [Test]
        public void HasNext_ShortCircuits_WhenUninitialized()
        {
            // Critical performance/safety test: If _isInitialized is false, 
            // the state logic must NOT be called (short-circuit).
            Iterator<T, MockLogic<T>> it = default;

            Assert.IsFalse(it.HasNext, "HasNext must immediately return false if the iterator is uninitialized (default struct).");
        }

        #endregion

        #region TryPeekNext Tests

        [Test]
        public void TryPeekNext_ReturnsCorrectValue_WithoutChangingCurrent()
        {
            // Scenario: Verify that TryPeekNext provides the upcoming value 
            // but leaves the iterator's Current property and position untouched.
            var data = CreateLogic(1);
            var logic = data.logic;
            var expected = data.expected;
            var it = new Iterator<T, MockLogic<T>>(logic);

            // 1. Act: Peek the value
            bool success = it.TryPeekNext(out T result);

            // 2. Assert: Peek results
            Assert.IsTrue(success, $"TryPeekNext should succeed for {typeof(T).Name}.");
            Assert.AreEqual(expected[0], result, "Peeked value does not match the expected first item.");

            // 3. CRITICAL: State check
            // Current must still be default because MoveNext has not been called!
            Assert.AreEqual(default(T), it.Current, "Current must not change after a Peek operation.");

            // Verify that we can still MoveNext to that same value
            Assert.IsTrue(it.MoveNext(), "MoveNext should still succeed after a Peek.");
            Assert.AreEqual(result, it.Current, "The value returned by Peek must be the same as the subsequent MoveNext.");
        }

        [Test]
        public void TryPeekNext_ReturnsFalse_WhenEmpty()
        {
            // Scenario: Verify that TryPeekNext returns false and provides the 
            // default(T) value if the iterator has no elements or is exhausted.
            var data = CreateLogic(0);
            var logic = data.logic;
            var it = new Iterator<T, MockLogic<T>>(logic);

            // Act
            bool success = it.TryPeekNext(out T result);

            // Assert
            Assert.IsFalse(success,
                $"TryPeekNext must return false for an empty {typeof(T).Name}-iterator.");

            Assert.AreEqual(default(T), result,
                "The 'out' result must be default(T) when TryPeekNext fails.");

            // Verify State: HasNext should also be false
            Assert.IsFalse(it.HasNext, "HasNext must be false for empty iterators.");
        }

        [Test]
        public void TryPeekNext_ShortCircuits_WhenUninitialized()
        {
            // Safety check: Uninitialized iterators must not call the state logic.
            Iterator<T, MockLogic<T>> it = default;

            bool success = it.TryPeekNext(out T result);

            Assert.IsFalse(success, "Peek must return false for uninitialized (default) iterators.");
            Assert.AreEqual(default(T), result, "Out result must be default(T) for uninitialized iterators.");
        }

        #endregion

        #region MoveNext Tests

        [Test]
        public void MoveNext_UpdatesCurrent_And_AdvancesState()
        {
            // Scenario: Verify that MoveNext correctly updates the Current property 
            // and successfully advances the internal state of the logic.
            var data = CreateLogic(2);
            var logic = data.logic;
            var expected = data.expected;
            var it = new Iterator<T, MockLogic<T>>(logic);

            // 1. First Move
            bool move1Success = it.MoveNext();
            Assert.IsTrue(move1Success, $"First MoveNext should return true for {typeof(T).Name}.");
            Assert.AreEqual(expected[0], it.Current, "Current must update to the first element after the first MoveNext.");

            // 2. Second Move
            bool move2Success = it.MoveNext();
            Assert.IsTrue(move2Success, "Second MoveNext should return true for the second element.");
            Assert.AreEqual(expected[1], it.Current, "Current must update to the second element after the second MoveNext.");

            // 3. Post-Condition: Verify no more items
            Assert.IsFalse(it.HasNext, "HasNext must be false after consuming all items.");
        }

        [Test]
        public void MoveNext_Exhaustion_IdentifiesAllElementsAndResetsCurrent()
        {
            // Scenario: Iterate through a full set and verify behavior both 
            // during iteration and after the sequence is exhausted.
            int count = 3;
            var data = CreateLogic(count);
            var logic = data.logic;
            var expected = data.expected;
            var it = new Iterator<T, MockLogic<T>>(logic);

            // Act & Assert 1: Iteration through all existing items
            for (int i = 0; i < expected.Length; i++)
            {
                Assert.IsTrue(it.MoveNext(), $"Should return true at index {i} for {typeof(T).Name}.");
                Assert.AreEqual(expected[i], it.Current, $"Value mismatch at index {i}.");
            }

            // Act & Assert 2: Behavior after the end (Exhaustion)
            bool canMoveFurther = it.MoveNext();

            Assert.IsFalse(canMoveFurther, "MoveNext must return false after the last element has been passed.");

            // Critical: Ensure the iterator doesn't "leak" the last value
            Assert.AreEqual(default(T), it.Current,
                "Current must be reset to default(T) after the iterator is exhausted.");

            // Verify HasNext reflects this state
            Assert.IsFalse(it.HasNext, "HasNext must stay false once exhausted.");
        }

        [Test]
        public void MoveNext_OnEmpty_ReturnsFalseImmediately()
        {
            // Scenario: Verify that MoveNext returns false immediately when 
            // the iterator is initialized with an empty dataset.
            var data = CreateLogic(0);
            var logic = data.logic;
            var it = new Iterator<T, MockLogic<T>>(logic);

            // Act
            bool result = it.MoveNext();

            // Assert
            Assert.IsFalse(result, $"MoveNext must return false immediately for an empty {typeof(T).Name}-iterator.");

            // Ensure Current is in a safe state
            Assert.AreEqual(default(T), it.Current,
                "Current must be default(T) when the iterator is empty.");
        }

        [Test]
        public void MoveNext_ShortCircuits_WhenUninitialized()
        {
            // Safety check: Uninitialized iterators must immediately return false 
            // without attempting to access the state.
            Iterator<T, MockLogic<T>> it = default;

            bool result = it.MoveNext();

            Assert.IsFalse(result, "MoveNext must return false for default structs.");
            Assert.AreEqual(default(T), it.Current, "Current must remain default.");
        }

        [Test]
        public void MoveNext_IsPersistent_ThroughStateMutation()
        {
            // Scenario: Verify that the internal state mutation is persistent 
            // and the iterator tracks its position correctly across multiple steps.
            var data = CreateLogic(3);
            var logic = data.logic;
            var expected = data.expected;
            var it = new Iterator<T, MockLogic<T>>(logic);

            // Act: Advance two steps
            it.MoveNext(); // Element 0
            it.MoveNext(); // Element 1

            // Assert: Check current position and remaining availability
            Assert.AreEqual(expected[1], it.Current,
                $"Iterator must persistently hold the second element for {typeof(T).Name}.");

            Assert.IsTrue(it.HasNext,
                "Iterator should still report HasNext true when elements remain in the sequence.");
        }

        #endregion
        
        #region Enumerator Pattern & Compiler Integration

        [Test]
        public void GetEnumerator_ConcreteStruct_ReturnsCopyOfSelf()
        {
            // Arrange: Create an iterator and advance it partially
            var data = CreateLogic(3);
            var logic = data.logic;
            var expected = data.expected;
            var it = new Iterator<T, MockLogic<T>>(logic);

            it.MoveNext(); // Current = expected[0]

            // Act: Get a copy of the current state
            // Since Iterator is a struct, GetEnumerator() returns a value copy.
            var snapshot = it.GetEnumerator();

            // Assert: Initial states must be identical but separate
            Assert.AreEqual(expected[0], snapshot.Current, "Snapshot should start at the same position.");
            Assert.AreEqual(expected[0], it.Current, "Original should remain at its position.");

            // Deep State Change: Advance both independently
            snapshot.MoveNext(); // snapshot -> expected[1]
            it.MoveNext();       // it -> expected[1]
            snapshot.MoveNext(); // snapshot -> expected[2]

            // Final Validation
            Assert.AreEqual(expected[2], snapshot.Current,
                "Snapshot should have reached the third element.");

            Assert.AreEqual(expected[1], it.Current,
                "Original should be unaffected by snapshot advancement and stay at the second element.");

            Assert.IsTrue(it.HasNext,
                "Original should still report HasNext true even if the snapshot is finished.");
        }

        [Test]
        public void GetEnumerator_InterfaceFallback_SupportsForeach_WithEarlyExit()
        {
            // Arrange: Use the interface to force execution of explicit implementation.
            // We use 4 items to have enough room for an early exit test.
            var data = CreateLogic(4);
            var logic = data.logic;
            var expected = data.expected;
            IIterator<T> it = new Iterator<T, MockLogic<T>>(logic);

            T lastValue = default;
            int count = 0;

            // Act: Verify that 'foreach' correctly consumes the interface-based iterator
            // and that a 'break' statement stops the process as expected.
            foreach (var val in it)
            {
                lastValue = val;
                count++;

                // Exit early after the second element
                if (count == 2) break;
            }

            // Assert
            Assert.AreEqual(expected[1], lastValue,
                "Foreach should have captured the second element before breaking.");

            Assert.AreEqual(2, count,
                "The iteration count must exactly match the point of the early exit.");
        }

        [Test]
        public void GetEnumerator_IsCompatibleWithDuckTyping_NestedLoops()
        {
            // Arrange: Verify that the compiler's duck-typing (foreach) correctly 
            // copies the struct, allowing independent nested iteration.
            var data = CreateLogic(2);
            var logic = data.logic;
            var expected = data.expected;
            var it = new Iterator<T, MockLogic<T>>(logic);

            var outerResults = new List<T>();
            var innerResults = new List<T>();

            // Act: Execute nested loops over the same struct instance.
            // Each 'foreach' calls the public GetEnumerator(), which returns a new copy.
            foreach (var outer in it)
            {
                outerResults.Add(outer);
                foreach (var inner in it)
                {
                    innerResults.Add(inner);
                }
            }

            // Assert
            // 1. Check counts: Outer should run 2 times, Inner 2 times per outer loop (2 * 2 = 4).
            Assert.AreEqual(2, outerResults.Count, "Outer loop should have processed 2 elements.");
            Assert.AreEqual(4, innerResults.Count, "Inner loop should have processed 4 elements total (2x2).");

            // 2. Check value sequence for the first inner pass
            Assert.AreEqual(expected[0], innerResults[0], "First inner element should match first item.");
            Assert.AreEqual(expected[1], innerResults[1], "Second inner element should match second item.");

            // 3. Check value sequence for the second inner pass (proving it restarted/copied)
            Assert.AreEqual(expected[0], innerResults[2], "Inner loop must start fresh for the second outer element.");
            Assert.AreEqual(expected[1], innerResults[3], "Inner loop must complete second pass correctly.");
        }

        #endregion

        #region System.Collections Interface Support (LINQ & Legacy)

        [Test]
        public void IEnumerable_Generic_AdvancesStateAndPreservesIntegrity()
        {
            // Arrange: Create the iterator and cast it to the generic interface.
            // This assignment causes boxing, creating a separate instance on the heap.
            var data = CreateLogic(3);
            var logic = data.logic;
            var expected = data.expected;
            var it = new Iterator<T, MockLogic<T>>(logic);
            IEnumerable<T> enumerable = it;

            // Act: Get the enumerator from the interface and advance it.
            using var enumerator = enumerable.GetEnumerator();

            // Advance the boxed enumerator two steps.
            Assert.IsTrue(enumerator.MoveNext()); // -> expected[0]
            Assert.IsTrue(enumerator.MoveNext()); // -> expected[1]

            // Assert
            // 1. Interface Check: The boxed copy must reflect the advancement.
            Assert.AreEqual(expected[1], enumerator.Current,
                "The interface-based enumerator should be at the second element.");

            // 2. CRITICAL STRUCT CHECK: 
            // The original 'it' struct must remain at the start (Current = default).
            // Because 'it' was boxed when assigned to 'enumerable', 'it' is NOT 
            // the same instance as the one being advanced by the interface.
            Assert.AreEqual(default(T), it.Current,
                "The original local struct must remain untouched when the boxed interface copy moves.");

            // 3. Verify original is still 'ready'
            Assert.IsTrue(it.HasNext, "The original struct should still report true for HasNext.");
        }

        [Test]
        public void IEnumerable_NonGeneric_AdvancesState()
        {
            // Arrange: Cast the struct to the legacy non-generic IEnumerable.
            // This tests the explicit implementation of System.Collections.IEnumerable.
            var data = CreateLogic(2);
            var logic = data.logic;
            var expected = data.expected;
            System.Collections.IEnumerable enumerable = new Iterator<T, MockLogic<T>>(logic);
            var enumerator = enumerable.GetEnumerator();

            // Act: Advance the state through the non-generic MoveNext()
            bool step1 = enumerator.MoveNext(); // Advances to expected[0]
            bool step2 = enumerator.MoveNext(); // Advances to expected[1]

            // Assert
            Assert.IsTrue(step1, "First MoveNext should return true on non-generic enumerator.");
            Assert.IsTrue(step2, "Second MoveNext should return true on non-generic enumerator.");

            // Check boxed Current value
            Assert.AreEqual(expected[1], enumerator.Current,
                "The legacy IEnumerator.Current must return the correct boxed value.");

            // Act: Move past the end
            bool step3 = enumerator.MoveNext();

            // Assert: Exhaustion
            Assert.IsFalse(step3, "Non-generic enumerator must return false when exhausted.");
        }

        [Test]
        public void IEnumerable_MultipleEnumerators_AreIndependent()
        {
            // Arrange: Create the iterator and box it into an IEnumerable.
            var data = CreateLogic(3);
            var logic = data.logic;
            var expected = data.expected;
            var it = new Iterator<T, MockLogic<T>>(logic);
            var enumerable = (IEnumerable<T>)it;

            // Act: Spawn two separate enumerators from the same enumerable source.
            using var enum1 = enumerable.GetEnumerator();
            using var enum2 = enumerable.GetEnumerator();

            // Advance the first enumerator
            bool move1Success = enum1.MoveNext(); // Advances enum1 to expected[0]

            // Assert
            Assert.IsTrue(move1Success, "First enumerator should advance successfully.");
            Assert.AreEqual(expected[0], enum1.Current, "First enumerator should point to the first element.");

            // Verify Independence:
            // We cast the second enumerator back to the concrete struct to inspect its internal state.
            // Because enum1 and enum2 are separate boxes created from the original 'it' struct,
            // advancing enum1 must have zero impact on enum2.
            var it2 = (Iterator<T, MockLogic<T>>)enum2;

            Assert.AreEqual(default(T), it2.Current,
                "The second enumerator must be a fresh, independent copy with default state.");

            // Final proof: Advance enum2 and ensure it starts from the beginning
            Assert.IsTrue(enum2.MoveNext());
            Assert.AreEqual(expected[0], enum2.Current,
                "The second enumerator should still be able to access the first element independently.");
        }

        #endregion

        #region Interface Contract Compliance (Exceptions)

        [Test]
        public void Reset_ThrowsNotSupportedException()
        {
            // Arrange: Initialize with a small sample via the factory.
            var data = CreateLogic(1);
            var logic = data.logic;
            var it = new Iterator<T, MockLogic<T>>(logic);

            // Reset is an explicit IEnumerator implementation, 
            // so we must cast to the non-generic interface to access it.
            var enumerator = (System.Collections.IEnumerator)it;

            // Act & Assert
            // We verify that calling Reset() triggers the expected exception.
            var ex = Assert.Throws<NotSupportedException>(() => enumerator.Reset(),
                $"Reset() should throw NotSupportedException for Iterator<{typeof(T).Name}>.");

            // Verify that the message is descriptive.
            Assert.That(ex.Message.ToLower(), Does.Contain("not supported"),
                "The exception message should clarify that Reset is unavailable for this iterator type.");
        }

        #endregion

        #region Lifecycle & Cleanup

        [Test]
        public void Dispose_IsPassive_And_DoesNotAlterState()
        {
            // Scenario: Verify that calling Dispose does not unexpectedly 
            // reset or clear the iterator's current position and state.
            var data = CreateLogic(2);
            var logic = data.logic;
            var expected = data.expected;
            var it = new Iterator<T, MockLogic<T>>(logic);

            // 1. Advance to a known state
            it.MoveNext();
            T stateBefore = it.Current;
            bool hasNextBefore = it.HasNext;

            // 2. Act: Dispose the iterator
            // Note: Since Iterator is a struct, this is a direct call.
            it.Dispose();

            // 3. Assert: Integrity check
            Assert.AreEqual(expected[0], it.Current,
                "Dispose should not clear the Current property for this iterator type.");

            Assert.AreEqual(stateBefore, it.Current,
                "The value of Current must remain identical after a Dispose call.");

            Assert.AreEqual(hasNextBefore, it.HasNext,
                "Dispose should not alter the internal HasNext state machine.");
        }

        [Test]
        public void Dispose_CanBeCalledMultipleTimes_WithoutException()
        {
            // Scenario: Verify that calling Dispose does not throw, even if called multiple times.
            // This ensures adherence to the standard IDisposable contract requirements.
            var data = CreateLogic(1);
            var logic = data.logic;
            var it = new Iterator<T, MockLogic<T>>(logic);

            // Act & Assert
            // First call should be safe
            Assert.DoesNotThrow(() => it.Dispose(), "Initial Dispose call should not throw.");

            // Subsequent calls should also be safe (Idempotency)
            Assert.DoesNotThrow(() => it.Dispose(), "Subsequent Dispose calls must not throw or cause side effects.");
        }

        [Test]
        public void Dispose_Foreach_EvenIfManuallyDisposedBeforehand()
        {
            // Scenario: Manual disposal before iteration starts. 
            // Since the Iterator is a struct and Dispose is passive (non-destructive), 
            // the subsequent foreach loop must still function correctly.
            var data = CreateLogic(2);
            var logic = data.logic;
            var expected = data.expected;
            var it = new Iterator<T, MockLogic<T>>(logic);
            int count = 0;

            // Act: Dispose early
            it.Dispose();

            // The foreach loop calls it.GetEnumerator(), creating a fresh copy of 
            // the current struct state.
            foreach (var item in it)
            {
                Assert.AreEqual(expected[count], item, $"Value mismatch at index {count}.");
                count++;
            }

            // Assert
            Assert.AreEqual(2, count, "The loop should complete normally even if Dispose was called beforehand.");
        }

        #endregion

        #region Create Factory

        [Test]
        public void Create_WithValidLogic_ReturnsCorrectType()
        {
            var logic = new MockLogic<int> { Items = new[] { 10, 20 } };
            var iterator = new Iterator<int, MockLogic<int>>(logic);

            Assert.IsInstanceOf<Iterator<int, MockLogic<int>>>(iterator);

            Assert.IsTrue(iterator.MoveNext());
            Assert.AreEqual(10, iterator.Current);
        }

        [Test]
        public void Create_WithDefaultLogic_ReturnsDormantIterator()
        {
            MockLogic<int> defaultLogic = default;

            // Act
            var iterator = new Iterator<int, MockLogic<int>>(defaultLogic);

            Assert.DoesNotThrow(() => {
                Assert.IsFalse(iterator.MoveNext(), "Iterator from default logic must be dormant.");
            });
        }

        #endregion

        #region Full Iterator Tests

        [Test]
        [TestCase(0)]
        [TestCase(1)]
        [TestCase(5)]
        [TestCase(20)]
        public void Iterator_Wrapper_SystemStressTest_WithFullDataValidation(int count)
        {
            // Arrange: Use the generic factory to get logic and ground truth
            var data = CreateLogic(count);
            var logic = data.logic;
            var expected = data.expected;
            var iterator = new Iterator<T, MockLogic<T>>(logic);

            T lastMovedValue = default;
            bool hasLastValue = false;

            // Act & Assert: Execute the full interleaved contract
            for (int i = 0; i < count; i++)
            {
                T expectedValue = expected[i];

                // 1. Availability Check
                Assert.IsTrue(iterator.HasNext,
                    $"Contract Violation: HasNext must be true at index {i}.");

                // 2. Peek & Progress Validation
                bool peekSuccess = iterator.TryPeekNext(out T currentPeek);
                Assert.IsTrue(peekSuccess, $"Wrapper Error: TryPeekNext failed at index {i}.");
                Assert.AreEqual(expectedValue, currentPeek, $"Data Error: Peek mismatch at index {i}.");

                if (hasLastValue)
                {
                    Assert.AreNotEqual(lastMovedValue, currentPeek,
                        $"Sequence Error: Peek at index {i} returned the same value as the previous MoveNext. State advancement failed.");
                }

                // 3. Consistency (HasNext stays true after non-destructive Peek)
                Assert.IsTrue(iterator.HasNext,
                    "State Error: Wrapper's HasNext became false after a Peek.");

                // 4. Execution & Final Data Integrity Check
                bool moveSuccess = iterator.MoveNext();
                T moved = iterator.Current;

                Assert.IsTrue(moveSuccess, $"Wrapper Execution Error: MoveNext failed at index {i}.");

                // Final Triple-Check: Peek == Moved == Expected
                Assert.AreEqual(currentPeek, moved,
                    $"Sync Error: Current value ({moved}) differs from Peeked value ({currentPeek}) at index {i}.");
                Assert.AreEqual(expectedValue, moved,
                    $"Data Integrity Error: Current value ({moved}) does not match expected ground truth ({expectedValue}) at index {i}.");

                // Update cache
                lastMovedValue = moved;
                hasLastValue = true;
            }

            // --- Post-Exhaustion Phase ---
            Assert.IsFalse(iterator.HasNext, "Exhaustion Error: HasNext must be false.");
            Assert.IsFalse(iterator.TryPeekNext(out _), "Exhaustion Error: TryPeekNext must return false.");

            bool finalMove = iterator.MoveNext();
            Assert.IsFalse(finalMove, "Exhaustion Error: MoveNext must return false.");
            Assert.AreEqual(default(T), iterator.Current, "Safety Error: Current must be default(T) after exhaustion.");
        }

        #endregion
    }
}
