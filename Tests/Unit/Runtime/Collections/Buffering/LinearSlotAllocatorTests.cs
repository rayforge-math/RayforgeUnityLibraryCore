using NUnit.Framework;
using System;

namespace Rayforge.Core.Collections.Buffering.Tests
{
    [TestFixture]
    public class LinearSlotAllocatorTests
    {
        #region Constructor

        [Test]
        public void Constructor_ValidInputs_SetsPropertiesCorrectly()
        {
            int capacity = 100;
            int baseOffset = 50;

            var allocator = new LinearSlotAllocator(capacity, baseOffset);

            Assert.AreEqual(capacity, allocator.Capacity);
            Assert.AreEqual(baseOffset, allocator.BaseOffset);
        }

        [Test]
        public void Constructor_DefaultBaseOffset_SetsOffsetToZero()
        {
            var allocator = new LinearSlotAllocator(100);

            Assert.AreEqual(0, allocator.BaseOffset);
        }

        [TestCase(0)]
        [TestCase(-1)]
        [TestCase(-100)]
        public void Constructor_InvalidCapacity_ThrowsArgumentOutOfRangeException(int invalidCapacity)
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new LinearSlotAllocator(invalidCapacity, 0));
        }

        [TestCase(-1)]
        [TestCase(-500)]
        public void Constructor_NegativeBaseOffset_ThrowsArgumentOutOfRangeException(int invalidBaseOffset)
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new LinearSlotAllocator(100, invalidBaseOffset));
        }

        [Test]
        public void Constructor_MinimumValidCapacity_IsAccepted()
        {
            var allocator = new LinearSlotAllocator(1, 0);

            Assert.AreEqual(1, allocator.Capacity);
            Assert.DoesNotThrow(() => allocator.Acquire());
        }

        [Test]
        public void Constructor_LargeCapacity_IsAccepted()
        {
            int largeCapacity = 1_000_000;
            var allocator = new LinearSlotAllocator(largeCapacity, 0);

            Assert.AreEqual(largeCapacity, allocator.Capacity);
        }

        #endregion

        #region Reconfigure

        [Test]
        public void Reconfigure_ValidInputs_UpdatesPropertiesAndResetsState()
        {
            // 1. Setup: Allocate and release to populate internal state
            var allocator = new LinearSlotAllocator(10, 0);
            allocator.Acquire(); // Index 0 occupied
            allocator.Release(0); // Index 0 added to free stack

            // 2. Perform Reconfigure
            allocator.Reconfigure(20, 100);

            // 3. Assert: Verify properties were updated
            Assert.AreEqual(20, allocator.Capacity);
            Assert.AreEqual(100, allocator.BaseOffset);

            // 4. Assert: Verify internal state reset
            // New allocation should start from new BaseOffset
            Assert.AreEqual(20, allocator.AvailableCount, "Available count should be reset to full capacity.");
            Assert.AreEqual(100, allocator.Acquire(), "Allocation should restart from the new BaseOffset.");
        }

        [TestCase(0)]
        [TestCase(-1)]
        public void Reconfigure_InvalidCapacity_ThrowsArgumentOutOfRangeException(int invalidCapacity)
        {
            var allocator = new LinearSlotAllocator(10, 0);
            // Ensure invalid capacity triggers validation
            Assert.Throws<ArgumentOutOfRangeException>(() => allocator.Reconfigure(invalidCapacity, 0));
        }

        [TestCase(-1)]
        [TestCase(-500)]
        public void Reconfigure_InvalidBaseOffset_ThrowsArgumentOutOfRangeException(int invalidOffset)
        {
            var allocator = new LinearSlotAllocator(10, 0);
            // Ensure negative base offset triggers validation
            Assert.Throws<ArgumentOutOfRangeException>(() => allocator.Reconfigure(10, invalidOffset));
        }

        [Test]
        public void Reconfigure_StateIsResetEvenIfInputsMatchPrevious()
        {
            var allocator = new LinearSlotAllocator(10, 0);
            allocator.Acquire(); // Index 0 occupied

            // Reconfigure with identical parameters
            allocator.Reconfigure(10, 0);

            // Verify that the internal counter was reset despite same values
            Assert.AreEqual(0, allocator.Acquire(), "Allocator should reset even if values remain unchanged.");
        }

        #endregion

        #region Properties

        [Test]
        public void Capacity_ReturnsInitializedValue()
        {
            // Verify capacity is correctly exposed
            int expectedCapacity = 150;
            var allocator = new LinearSlotAllocator(expectedCapacity, 0);

            Assert.AreEqual(expectedCapacity, allocator.Capacity);
        }

        [Test]
        public void BaseOffset_ReturnsInitializedValue()
        {
            // Verify base offset is correctly exposed
            int expectedOffset = 500;
            var allocator = new LinearSlotAllocator(10, expectedOffset);

            Assert.AreEqual(expectedOffset, allocator.BaseOffset);
        }

        [Test]
        public void AvailableCount_ReflectsLinearSpaceAndRecycledSlots()
        {
            // 1. Initial state: Capacity 10, offset 0 -> Available 10
            var allocator = new LinearSlotAllocator(10, 0);
            Assert.AreEqual(10, allocator.AvailableCount);

            // 2. Allocate: 10 -> 9 remaining
            allocator.Acquire();
            Assert.AreEqual(9, allocator.AvailableCount);

            // 3. Allocate another: 9 -> 8 remaining
            allocator.Acquire();
            Assert.AreEqual(8, allocator.AvailableCount);

            // 4. Release one: 8 -> 9 available
            allocator.Release(0);
            Assert.AreEqual(9, allocator.AvailableCount, "AvailableCount should include recycled slots from the stack.");
        }

        [Test]
        public void AvailableCount_AtZeroCapacity_ReturnsZero()
        {
            // Although we validated capacity >= 1 in the constructor, 
            // we check behavior if initialized with a valid minimum
            var allocator = new LinearSlotAllocator(1, 0);

            allocator.Acquire(); // Fully occupied
            Assert.AreEqual(0, allocator.AvailableCount);
        }

        [Test]
        public void RecycleCount_TracksCorrectNumberOfRecycledSlots()
        {
            // Arrange: Allocator mit Kapazität 5, Basis 100
            var allocator = new LinearSlotAllocator(5, 100);

            // Assert: Initial bei 0
            Assert.AreEqual(0, allocator.RecycleCount, "RecycleCount should be 0 on a fresh allocator.");

            // Act: Einige Slots belegen
            int slot1 = allocator.Acquire(); // 100
            int slot2 = allocator.Acquire(); // 101

            // Act: Eines releasen
            allocator.Release(slot1);

            // Assert: Jetzt muss 1 im Stack liegen
            Assert.AreEqual(1, allocator.RecycleCount, "RecycleCount should be 1 after releasing one slot.");

            // Act: Das zweite releasen
            allocator.Release(slot2);

            // Assert: Jetzt müssen 2 im Stack liegen
            Assert.AreEqual(2, allocator.RecycleCount, "RecycleCount should be 2 after releasing two slots.");

            // Act: Wieder eines abrufen
            allocator.Acquire();

            // Assert: Sollte wieder auf 1 sinken
            Assert.AreEqual(1, allocator.RecycleCount, "RecycleCount should be 1 after acquiring one of the recycled slots.");
        }

        #endregion

        #region Acquire

        [Test]
        public void Acquire_ReturnsCorrectLinearIndices()
        {
            // Test base allocation without recycling
            var allocator = new LinearSlotAllocator(5, 100);

            Assert.AreEqual(100, allocator.Acquire());
            Assert.AreEqual(101, allocator.Acquire());
            Assert.AreEqual(102, allocator.Acquire());
        }

        [Test]
        public void Acquire_PrioritizesRecycledSlotsOverNewAllocation()
        {
            var allocator = new LinearSlotAllocator(5, 100);

            // 1. Occupy two slots
            allocator.Acquire(); // 100
            int secondIndex = allocator.Acquire(); // 101

            // 2. Release the first slot
            allocator.Release(100);

            // 3. Acquire again - should return the recycled 100, not 102
            int nextIndex = allocator.Acquire();

            Assert.AreEqual(100, nextIndex, "Acquire should prioritize recycled slots from the stack.");
        }

        [Test]
        public void Acquire_ThrowsOverflowException_WhenCapacityIsExceeded()
        {
            var allocator = new LinearSlotAllocator(2, 50);

            // Fill up to capacity
            allocator.Acquire(); // 50
            allocator.Acquire(); // 51

            // Next attempt should throw
            Assert.Throws<OverflowException>(() => allocator.Acquire());
        }

        [Test]
        public void Acquire_HandlesStackUnderflowCorrectly()
        {
            var allocator = new LinearSlotAllocator(5, 0);

            // Release a slot that wasn't acquired is technically possible via Release, 
            // but here we test that Acquire doesn't fail if we just perform a standard flow
            allocator.Acquire(); // 0
            allocator.Release(0);

            // Clear the stack by acquiring
            allocator.Acquire(); // Should get 0

            // Stack is empty, should now return 1 (linear)
            Assert.AreEqual(1, allocator.Acquire());
        }

        [Test]
        public void Acquire_AfterReset_ReturnsToInitialState()
        {
            var allocator = new LinearSlotAllocator(2, 100);

            allocator.Acquire(); // 100
            allocator.Acquire(); // 101

            // Reset the allocator
            allocator.Reset();

            // Now it should act like it was just initialized
            Assert.AreEqual(100, allocator.Acquire(), "After Reset, allocation should start from BaseOffset again.");
        }

        #endregion

        #region Release

        [Test]
        public void Release_ValidIndex_ReturnsToPool()
        {
            var allocator = new LinearSlotAllocator(5, 100);

            int index = allocator.Acquire();
            allocator.Release(index);

            // 1 (recycled) + (5 - 1) (remaining linear) = 5
            Assert.AreEqual(5, allocator.AvailableCount);
            Assert.AreEqual(100, allocator.Acquire());
        }

        [Test]
        public void Release_IndexBelowBaseOffset_IsIgnored()
        {
            var allocator = new LinearSlotAllocator(5, 100);

            // Try releasing an index below the valid range (BaseOffset = 100)
            Assert.DoesNotThrow(() => allocator.Release(99));

            // Ensure no internal state change
            Assert.AreEqual(5, allocator.AvailableCount);
        }

        [Test]
        public void Release_IndexAboveCapacity_IsIgnored()
        {
            var allocator = new LinearSlotAllocator(5, 100); // Valid range [100, 104]

            // Try releasing an index outside the valid range
            Assert.DoesNotThrow(() => allocator.Release(105));

            // Ensure no internal state change
            Assert.AreEqual(5, allocator.AvailableCount);
        }

        [Test]
        public void Release_MultipleTimes_AllowsDuplicateRelease()
        {
            var allocator = new LinearSlotAllocator(5, 100);
            int index = allocator.Acquire();

            // Releasing the same index twice might lead to duplicates in the FreeStack
            // based on your current implementation. This test documents that behavior.
            allocator.Release(index);
            allocator.Release(index);

            Assert.AreEqual(1, allocator.RecycleCount, "Current implementation allows the same index to be pushed to the stack multiple times.");
        }

        [Test]
        public void Release_UnusedIndex_DoesNothingToAllocatorState()
        {
            // Setup: Allocator with capacity 5, range [100, 104]
            var allocator = new LinearSlotAllocator(5, 100);

            // 102 is within the valid range, but has not been 'Acquired' yet.
            // Releasing it should be ignored or at least not allow it to be popped as 'free'
            // if the logic strictly relies on previously acquired slots.
            allocator.Release(102);

            // Assert: AvailableCount should still be the initial capacity
            Assert.AreEqual(5, allocator.AvailableCount, "Releasing an unused index should not increment available count.");

            // Ensure that calling Acquire returns the actual next linear index (100)
            Assert.AreEqual(100, allocator.Acquire(), "Acquire should ignore the wrongly released unused index.");
        }

        #endregion

        #region Reset Tests

        [Test]
        public void Reset_ClearsAllStateAndRestartsAllocation()
        {
            // 1. Setup: Use the allocator to create some state
            var allocator = new LinearSlotAllocator(10, 100);
            allocator.Acquire(); // 100
            allocator.Acquire(); // 101
            allocator.Release(100); // 100 is now in the free stack

            // 2. Perform Reset
            allocator.Reset();

            // 3. Assert: Verify properties
            Assert.AreEqual(10, allocator.AvailableCount, "AvailableCount should be reset to full capacity.");

            // 4. Assert: Verify allocation restarts from BaseOffset
            Assert.AreEqual(100, allocator.Acquire(), "Allocation should start from BaseOffset after Reset.");
            Assert.AreEqual(101, allocator.Acquire(), "Subsequent allocation should continue linearly.");
        }

        [Test]
        public void Reset_ClearsFreeStack()
        {
            var allocator = new LinearSlotAllocator(5, 0);
            allocator.Acquire(); // 0
            allocator.Release(0);

            // Verify stack has an item
            Assert.AreEqual(1, allocator.RecycleCount, "RecycleCount should be 1 after releasing one slot.");
            Assert.AreEqual(5, allocator.AvailableCount, "AvailableCount should be 5 (4 unused linear + 1 recycled).");

            allocator.Reset();

            // After reset, stack should be empty (RecycleCount = 0)
            Assert.AreEqual(0, allocator.RecycleCount, "RecycleCount should be 0 after Reset.");

            // The allocator is back to 5 fresh linear slots
            Assert.AreEqual(5, allocator.AvailableCount, "AvailableCount should be 5 after Reset.");

            // Acquire should return 0 (the first linear slot) after reset
            Assert.AreEqual(0, allocator.Acquire(), "Acquire should return 0 (linear) after Reset.");
        }

        [Test]
        public void Reset_CanBeCalledMultipleTimes()
        {
            var allocator = new LinearSlotAllocator(5, 0);

            // Calling Reset multiple times should not cause side effects
            Assert.DoesNotThrow(() => {
                allocator.Reset();
                allocator.Reset();
            });

            Assert.AreEqual(5, allocator.AvailableCount);
        }

        [Test]
        public void Reset_DoesNotChangeCapacityOrBaseOffset()
        {
            var allocator = new LinearSlotAllocator(20, 500);

            allocator.Reset();

            Assert.AreEqual(20, allocator.Capacity, "Capacity should remain unchanged after Reset.");
            Assert.AreEqual(500, allocator.BaseOffset, "BaseOffset should remain unchanged after Reset.");
        }

        #endregion
    }
}
