using NUnit.Framework;
using System;

namespace Rayforge.Core.Collections.Buffering.Tests
{
    [TestFixture]
    public class KeyedSlotMapperTests
    {
        #region Constructor Tests

        [Test]
        public void DefaultConstructor_IsInitiallyNotInitialized()
        {
            var mapper = new KeyedSlotMapper<int>();

            Assert.IsNotNull(mapper, "Mapper should not be null after default construction.");
            Assert.IsFalse(mapper.IsInitialized, "Mapper should not be initialized by default.");
        }

        [Test]
        public void CapacityConstructor_ValidCapacity_IsInitialized()
        {
            var mapper = new KeyedSlotMapper<int>(10);

            Assert.IsTrue(mapper.IsInitialized, "Mapper should be initialized after construction with capacity.");
            Assert.AreEqual(10, mapper.Capacity);
        }

        [TestCase(0)]
        [TestCase(-1)]
        public void Constructor_InvalidCapacity_ThrowsArgumentOutOfRangeException(int invalidCapacity)
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new KeyedSlotMapper<int>(invalidCapacity));
        }

        #endregion

        #region Property Tests

        [Test]
        public void Capacity_ReturnsInitializedValue()
        {
            int capacity = 42;
            var mapper = new KeyedSlotMapper<int>(capacity);
            Assert.AreEqual(capacity, mapper.Capacity);
        }

        [Test]
        public void Capacity_WithExtremeValue_ReturnsValue()
        {
            int extremeCapacity = int.MaxValue;
            var mapper = new KeyedSlotMapper<int>(extremeCapacity);
            Assert.AreEqual(extremeCapacity, mapper.Capacity);
        }

        [Test]
        public void IsInitialized_IsFalseByDefault()
        {
            var mapper = new KeyedSlotMapper<int>();
            Assert.IsFalse(mapper.IsInitialized);
        }

        [Test]
        public void IsInitialized_IsTrueAfterInitialization()
        {
            var mapper = new KeyedSlotMapper<int>();
            mapper.Initialize(10);
            Assert.IsTrue(mapper.IsInitialized);
        }

        [Test]
        public void IsInitialized_RemainsTrueAfterReset()
        {
            var mapper = new KeyedSlotMapper<int>(10);
            mapper.Reset();
            Assert.IsTrue(mapper.IsInitialized);
        }

        [Test]
        public void Count_IsZeroInitially()
        {
            var mapper = new KeyedSlotMapper<int>(10);
            Assert.AreEqual(0, mapper.Count);
        }

        [Test]
        public void Count_IncrementsOnAllocation()
        {
            var mapper = new KeyedSlotMapper<int>(10);
            mapper.GetOrAllocate(1);
            Assert.AreEqual(1, mapper.Count);
        }

        [Test]
        public void Count_DecrementsOnRelease()
        {
            var mapper = new KeyedSlotMapper<int>(10);
            mapper.GetOrAllocate(1);
            mapper.Release(1);
            Assert.AreEqual(0, mapper.Count);
        }

        [Test]
        public void HighestActiveIndex_IsNegativeOneInitially()
        {
            var mapper = new KeyedSlotMapper<int>(10);
            Assert.AreEqual(-1, mapper.HighestActiveIndex);
        }

        [Test]
        public void HighestActiveIndex_IncrementsOnNewAllocation()
        {
            var mapper = new KeyedSlotMapper<int>(10);
            mapper.GetOrAllocate(1);
            Assert.AreEqual(0, mapper.HighestActiveIndex);
        }

        [Test]
        public void HighestActiveIndex_DoesNotChangeOnRelease()
        {
            var mapper = new KeyedSlotMapper<int>(10);
            mapper.GetOrAllocate(1);
            mapper.Release(1);
            Assert.AreEqual(0, mapper.HighestActiveIndex);
        }

        [Test]
        public void HighestActiveIndex_ResetsOnResetCall()
        {
            var mapper = new KeyedSlotMapper<int>(10);
            mapper.GetOrAllocate(1);
            mapper.Reset();
            Assert.AreEqual(-1, mapper.HighestActiveIndex);
        }

        #endregion

        #region Initialize Tests

        [Test]
        public void Initialize_ValidCapacity_SetsCapacityCorrectly()
        {
            var mapper = new KeyedSlotMapper<int>();
            int capacity = 100;

            mapper.Initialize(capacity);

            Assert.AreEqual(capacity, mapper.Capacity);
            Assert.IsTrue(mapper.IsInitialized);
        }

        [TestCase(0)]
        [TestCase(-1)]
        [TestCase(-100)]
        public void Initialize_InvalidCapacity_ThrowsArgumentOutOfRangeException(int invalidCapacity)
        {
            var mapper = new KeyedSlotMapper<int>();

            Assert.Throws<ArgumentOutOfRangeException>(() => mapper.Initialize(invalidCapacity));
        }

        [Test]
        public void Initialize_ExtremeCapacity_SetsCapacityCorrectly()
        {
            var mapper = new KeyedSlotMapper<int>();
            int extremeCapacity = 1_000_000;

            mapper.Initialize(extremeCapacity);

            Assert.AreEqual(extremeCapacity, mapper.Capacity);
        }

        #endregion

        #region Reset Tests

        [Test]
        public void Reset_OnFreshlyInitializedMapper_DoesNothingUnexpected()
        {
            var mapper = new KeyedSlotMapper<int>(10);

            Assert.DoesNotThrow(() => mapper.Reset());
            Assert.AreEqual(10, mapper.Capacity, "Capacity should persist after Reset.");
            Assert.AreEqual(0, mapper.Count);
        }

        [Test]
        public void Reset_WithActiveMappings_ClearsAllState()
        {
            var mapper = new KeyedSlotMapper<int>(10);
            mapper.GetOrAllocate(1);
            mapper.GetOrAllocate(2);

            // Act
            mapper.Reset();

            // Assert
            Assert.AreEqual(0, mapper.Count, "Count should be zero after Reset.");
            Assert.AreEqual(-1, mapper.HighestActiveIndex, "NextAvailableIndex should be reset to zero.");

            Assert.AreEqual(0, mapper.GetOrAllocate(3), "Mapper should start allocating from 0 again.");
        }

        [Test]
        public void Reset_WithReuseStack_ClearsStack()
        {
            var mapper = new KeyedSlotMapper<int>(10);
            mapper.GetOrAllocate(1);
            mapper.Release(1);

            mapper.Reset();

            Assert.AreEqual(0, mapper.Count);

            mapper.GetOrAllocate(2);
            Assert.AreEqual(1, mapper.Count);
        }

        [Test]
        public void Reset_OnUninitializedMapper_DoesNotThrow()
        {
            var mapper = new KeyedSlotMapper<int>();

            Assert.DoesNotThrow(() => mapper.Reset());
            Assert.IsFalse(mapper.IsInitialized, "Mapper should still be uninitialized.");
        }

        #endregion

        #region GetOrAllocate Tests

        [Test]
        public void GetOrAllocate_NewKey_ReturnsNewIndex()
        {
            var mapper = new KeyedSlotMapper<int>(10);
            int index = mapper.GetOrAllocate(1);

            Assert.AreEqual(0, index);
        }

        [Test]
        public void GetOrAllocate_SameKeyMultipleTimes_ReturnsSameIndex()
        {
            var mapper = new KeyedSlotMapper<int>(10);
            int index1 = mapper.GetOrAllocate(1);
            int index2 = mapper.GetOrAllocate(1);

            Assert.AreEqual(index1, index2);
        }

        [Test]
        public void GetOrAllocate_UsesReuseStack_WhenAvailable()
        {
            var mapper = new KeyedSlotMapper<int>(10);
            mapper.GetOrAllocate(1);
            mapper.GetOrAllocate(2);

            mapper.Release(1);

            int index = mapper.GetOrAllocate(3);
            Assert.AreEqual(0, index, "Should reuse the index from the stack.");
        }

        [Test]
        public void GetOrAllocate_UsesNextAvailableIndex_WhenStackIsEmpty()
        {
            var mapper = new KeyedSlotMapper<int>(10);
            mapper.GetOrAllocate(1);

            int index = mapper.GetOrAllocate(2);
            Assert.AreEqual(1, index, "Should increment to the next available index.");
        }

        [Test]
        public void GetOrAllocate_ThrowsException_WhenNotInitialized()
        {
            var mapper = new KeyedSlotMapper<int>();

            Assert.Throws<InvalidOperationException>(() => mapper.GetOrAllocate(1));
        }

        [Test]
        public void GetOrAllocate_ThrowsException_WhenCapacityReached()
        {
            var mapper = new KeyedSlotMapper<int>(1);
            mapper.GetOrAllocate(1);

            Assert.Throws<InvalidOperationException>(() => mapper.GetOrAllocate(2));
        }

        [Test]
        public void GetOrAllocate_HandlesExtremeCapacityCorrectly()
        {
            int capacity = 1000;
            var mapper = new KeyedSlotMapper<int>(capacity);

            int lastIndex = -1;
            for (int i = 0; i < capacity; i++)
            {
                lastIndex = mapper.GetOrAllocate(i);
            }

            Assert.AreEqual(capacity - 1, lastIndex);
        }

        #endregion

        #region Release Tests

        [Test]
        public void Release_ExistingKey_RemovesMappingAndAddsToReuseStack()
        {
            var mapper = new KeyedSlotMapper<int>(10);
            mapper.GetOrAllocate(1); // Index 0

            mapper.Release(1);

            Assert.AreEqual(0, mapper.Count, "Mapping should be removed.");

            int nextIndex = mapper.GetOrAllocate(2);
            Assert.AreEqual(0, nextIndex, "Released index 0 should be reused.");
        }

        [Test]
        public void Release_NonExistentKey_DoesNothing()
        {
            var mapper = new KeyedSlotMapper<int>(10);
            mapper.GetOrAllocate(1); // Index 0

            mapper.Release(999);

            Assert.AreEqual(1, mapper.Count, "Count should remain unchanged.");

            int nextIndex = mapper.GetOrAllocate(2);
            Assert.AreEqual(1, nextIndex, "Next available index should be 1, not 0.");
        }

        [Test]
        public void Release_OnUninitializedMapper_DoesNotThrow()
        {
            var mapper = new KeyedSlotMapper<int>();

            Assert.DoesNotThrow(() => mapper.Release(1));
        }

        [Test]
        public void Release_MultipleTimesSameKey_HandlesGracefully()
        {
            var mapper = new KeyedSlotMapper<int>(10);
            mapper.GetOrAllocate(1);

            mapper.Release(1);
            Assert.DoesNotThrow(() => mapper.Release(1), "Releasing the same key twice should not throw.");
            Assert.AreEqual(0, mapper.Count);
        }

        #endregion

        #region TryGetIndex Tests

        [Test]
        public void TryGetIndex_ExistingKey_ReturnsTrueAndCorrectIndex()
        {
            var mapper = new KeyedSlotMapper<int>(10);
            mapper.GetOrAllocate(123);

            bool found = mapper.TryGetIndex(123, out int index);

            Assert.IsTrue(found);
            Assert.AreEqual(0, index);
        }

        [Test]
        public void TryGetIndex_NonExistentKey_ReturnsFalseAndZero()
        {
            var mapper = new KeyedSlotMapper<int>(10);

            bool found = mapper.TryGetIndex(404, out int index);

            Assert.IsFalse(found);
            Assert.AreEqual(0, index);
        }

        [Test]
        public void TryGetIndex_UninitializedMapper_ReturnsFalseAndZero()
        {
            var mapper = new KeyedSlotMapper<int>();

            bool found = mapper.TryGetIndex(1, out int index);

            Assert.IsFalse(found);
            Assert.AreEqual(0, index);
        }

        [Test]
        public void TryGetIndex_AfterRelease_ReturnsFalseAndZero()
        {
            var mapper = new KeyedSlotMapper<int>(10);
            mapper.GetOrAllocate(1);
            mapper.Release(1);

            bool found = mapper.TryGetIndex(1, out int index);

            Assert.IsFalse(found);
            Assert.AreEqual(0, index);
        }

        #endregion
    }
}
