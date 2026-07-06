using NUnit.Framework;
using Rayforge.Core.TestEnv;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Rayforge.Core.Collections.Abstractions.Tests
{
    public abstract class IGpuDataProviderTests
    {
        #region Create Test Env

        protected abstract IGpuDataProvider<Vector2Int> CreateProvider(Dictionary<Type, Array> expected, Vector2Int[] keys, int length, int batchSize);
        protected abstract IGpuDataProvider<Vector2Int> CreateProvider(int length, int batchSize);

        private GpuDataProviderTestData CreateTestData(int length, int batchSize, bool empty = false)
        {
            var expected = new Dictionary<Type, Array>();

            IGpuDataProvider<Vector2Int> registry;

            if (empty)
            {
                registry = CreateProvider(length, batchSize);

                return new GpuDataProviderTestData
                {
                    keys = null,
                    provider = registry,
                    expected = null
                };
            }
            else
            {
                expected[typeof(int)] = TestUtility.CreateSampleItems<int>(length);
                expected[typeof(float)] = TestUtility.CreateSampleItems<float>(length);
                expected[typeof(long)] = TestUtility.CreateSampleItems<long>(length);
                expected[typeof(double)] = TestUtility.CreateSampleItems<double>(length);

                var keys = new Vector2Int[length];

                for (int i = 0; i < length; ++i)
                {
                    keys[i] = new Vector2Int(i, 0);
                }

                registry = CreateProvider(expected, keys, length, batchSize);

                return new GpuDataProviderTestData
                {
                    keys = keys,
                    provider = registry,
                    expected = expected
                };
            }
        }

        #endregion

        #region IGpuDataProvider

        #region GetRawBuffer Tests

        [Test]
        public void GetRawBuffer_ReturnsCorrectDataForAllRegisteredStores()
        {
            // Arrange
            int length = 10;
            int batchSize = 4;
            var testData = CreateTestData(length, batchSize);
            var provider = testData.provider;

            // Act & Assert for int
            var bufferInt = provider.GetRawBuffer<int>().TypedBuffer;
            Assert.IsNotNull(bufferInt);
            var expectedInt = (int[])testData.expected[typeof(int)];
            for (int i = 0; i < length; i++) Assert.AreEqual(expectedInt[i], bufferInt[i]);

            // Act & Assert for float
            var bufferFloat = provider.GetRawBuffer<float>().TypedBuffer;
            Assert.IsNotNull(bufferFloat);
            var expectedFloat = (float[])testData.expected[typeof(float)];
            for (int i = 0; i < length; i++) Assert.AreEqual(expectedFloat[i], bufferFloat[i]);

            // Act & Assert for long
            var bufferLong = provider.GetRawBuffer<long>().TypedBuffer;
            Assert.IsNotNull(bufferLong);
            var expectedLong = (long[])testData.expected[typeof(long)];
            for (int i = 0; i < length; i++) Assert.AreEqual(expectedLong[i], bufferLong[i]);

            // Act & Assert for double
            var bufferDouble = provider.GetRawBuffer<double>().TypedBuffer;
            Assert.IsNotNull(bufferDouble);
            var expectedDouble = (double[])testData.expected[typeof(double)];
            for (int i = 0; i < length; i++) Assert.AreEqual(expectedDouble[i], bufferDouble[i]);
        }

        [Test]
        public void GetRawBuffer_UnregisteredType_ThrowsInvalidOperationException()
        {
            // Arrange
            int length = 10;
            int batchSize = 4;
            var testData = CreateTestData(length, batchSize);
            var provider = testData.provider;

            // Act & Assert
            Assert.Throws<InvalidOperationException>(() =>
            {
                provider.GetRawBuffer<byte>();
            }, "Calling GetRawBuffer for an unregistered type should throw an InvalidOperationException.");
        }

        #endregion   

        #region Set Tests

        [Test]
        public void Set_ShouldStoreValue_WhenStoreIsRegistered()
        {
            // Arrange
            int length = 10;
            int batchSize = 4;
            var testData = CreateTestData(length, batchSize, true);
            var provider = testData.provider;

            // Using Vector2 as the key type
            var key = new Vector2Int(0, 0);
            var value = 42.0f; // float is one of the pre-registered types

            // Act
            int index = provider.Set(key, value);

            // Assert
            Assert.GreaterOrEqual(index, 0, "Set should return a valid index for Vector2 key.");
            Assert.AreEqual(value, provider.Get<float>(key), "The stored value must match the input value for the given Vector2 key.");
        }

        [Test]
        public void Set_ShouldUpdateValue_WhenSettingSameKeyMultipleTimes()
        {
            // Arrange
            var testData = CreateTestData(10, 4, true);
            var provider = testData.provider;
            var key = new Vector2Int(1, 1);
            float initialValue = 10.0f;
            float updatedValue = 20.0f;

            // Act
            provider.Set(key, initialValue);
            int indexAfterUpdate = provider.Set(key, updatedValue);

            // Assert
            // Index sollte gleich bleiben, da der Key existiert
            Assert.AreEqual(updatedValue, provider.Get<float>(key),
                "The value should be updated to the new value when setting the same key again.");
        }

        [Test]
        public void Set_ShouldStoreAllValues_WhenSettingMultipleDifferentKeys()
        {
            // Arrange
            var testData = CreateTestData(10, 4, true);
            var provider = testData.provider;

            var key1 = new Vector2Int(0, 0);
            var key2 = new Vector2Int(1, 2);
            var key3 = new Vector2Int(5, 5);

            float val1 = 1.1f;
            float val2 = 2.2f;
            float val3 = 3.3f;

            // Act
            provider.Set(key1, val1);
            provider.Set(key2, val2);
            provider.Set(key3, val3);

            // Assert
            Assert.AreEqual(val1, provider.Get<float>(key1), "Value for key1 should be stored correctly.");
            Assert.AreEqual(val2, provider.Get<float>(key2), "Value for key2 should be stored correctly.");
            Assert.AreEqual(val3, provider.Get<float>(key3), "Value for key3 should be stored correctly.");
        }

        [Test]
        public void Set_ShouldHandleDifferentTypesForDifferentKeys()
        {
            // Arrange
            // Angenommen, int und float sind registriert
            var testData = CreateTestData(10, 4, true);
            var provider = testData.provider;

            var keyInt = new Vector2Int(0, 0);
            var keyFloat = new Vector2Int(0, 1);

            int valInt = 42;
            float valFloat = 3.14f;

            // Act
            provider.Set(keyInt, valInt);
            provider.Set(keyFloat, valFloat);

            // Assert
            Assert.AreEqual(valInt, provider.Get<int>(keyInt), "The provider should support storing int for a specific key.");
            Assert.AreEqual(valFloat, provider.Get<float>(keyFloat), "The provider should support storing float for a different key simultaneously.");
        }

        [Test]
        public void Set_ShouldThrowInvalidOperationException_WhenStoreIsNotRegistered()
        {
            // Arrange
            int length = 10;
            int batchSize = 4;
            var testData = CreateTestData(length, batchSize);
            var provider = testData.provider;

            // 'byte' is not in the pre-populated set of int, long, float, double
            var key = new Vector2Int(0, 0);
            var value = (byte)1;

            // Act & Assert
            var ex = Assert.Throws<InvalidOperationException>(() => provider.Set(key, value));

            Assert.That(ex.Message, Does.Contain(typeof(byte).Name),
                "Exception should clearly indicate that the store for 'byte' was not registered.");
        }

        #endregion

        #region Clear Tests

        [Test]
        public void Clear_ResetsAllStoresToDefaultValues()
        {
            // Arrange
            int length = 10;
            int batchSize = 4;
            var testData = CreateTestData(length, batchSize);
            var provider = testData.provider;

            // Act
            provider.Clear();

            // Check int
            var bufferInt = provider.GetRawBuffer<int>().TypedBuffer;
            for (int i = 0; i < length; i++)
                Assert.AreEqual(0, bufferInt[i], $"int buffer index {i} not cleared.");

            // Check float
            var bufferFloat = provider.GetRawBuffer<float>().TypedBuffer;
            for (int i = 0; i < length; i++)
                Assert.AreEqual(0f, bufferFloat[i], $"float buffer index {i} not cleared.");

            // Check long
            var bufferLong = provider.GetRawBuffer<long>().TypedBuffer;
            for (int i = 0; i < length; i++)
                Assert.AreEqual(0L, bufferLong[i], $"long buffer index {i} not cleared.");

            // Check double
            var bufferDouble = provider.GetRawBuffer<double>().TypedBuffer;
            for (int i = 0; i < length; i++)
                Assert.AreEqual(0.0, bufferDouble[i], $"double buffer index {i} not cleared.");
        }

        [Test]
        public void Clear_ResetsProviderStateProperties()
        {
            // Arrange
            var testData = CreateTestData(10, 4, true);
            var provider = testData.provider;

            provider.Set(new Vector2Int(0, 0), 10.0f);
            provider.Set(new Vector2Int(1, 1), 20.0f);

            Assert.Greater(provider.Count, 0, "Count should be greater than 0 before clearing.");
            Assert.GreaterOrEqual(provider.HighestIndex, 0, "HighestIndex should be greater than or equal to 0 before clearing.");

            // Act
            provider.Clear();

            // Assert
            Assert.AreEqual(0, provider.Count, "Count should be reset to 0 after Clear.");
            Assert.AreEqual(-1, provider.HighestIndex, "HighestIndex should be reset to -1 after Clear.");
        }

        [Test]
        public void Clear_AllowsReusageOfPreviouslyUsedKeys()
        {
            // Arrange
            var testData = CreateTestData(10, 4, true);
            var provider = testData.provider;
            var key = new Vector2Int(5, 5);

            // Act
            provider.Set(key, 100.0f);
            provider.Clear();

            Assert.Throws<KeyNotFoundException>(() => provider.Get<float>(key),
                "Get should throw KeyNotFoundException for a key that was cleared.");
            Assert.IsFalse(provider.TryGet<float>(key, out _),
                "TryGet should return false after the key was cleared.");

            int newIndex = provider.Set(key, 200.0f);

            // Assert
            Assert.GreaterOrEqual(newIndex, 0, "Should be able to reuse the registry after Clear.");
            Assert.AreEqual(200.0f, provider.Get<float>(key), "Value should be correctly stored after re-setting the key post-Clear.");
        }

        [Test]
        public void Clear_DoesNotAffectCapacityOrBatchSize()
        {
            // Arrange
            int length = 10;
            int batchSize = 4;
            var testData = CreateTestData(length, batchSize, true);
            var provider = testData.provider;

            // Act
            provider.Clear();

            // Assert
            Assert.AreEqual(length, provider.Capacity, "Capacity should remain unchanged after Clear.");
            Assert.AreEqual(batchSize, provider.BatchSize, "BatchSize should remain unchanged after Clear.");
        }

        [Test]
        public void Clear_ShouldResetDirtyStateForAllRegisteredStores()
        {
            // Arrange
            int length = 10;
            int batchSize = 4;
            var testData = CreateTestData(length, batchSize, true);
            var provider = testData.provider;

            provider.Set(new Vector2Int(0, 0), 42.0f);   // float
            provider.Set(new Vector2Int(0, 1), 100);     // int

            Assert.IsTrue(provider.IsDirty<float>(), "Float store should be dirty after set.");
            Assert.IsTrue(provider.IsDirty<int>(), "Int store should be dirty after set.");

            // Act
            provider.Clear();

            // Assert
            Assert.IsFalse(provider.IsDirty<float>(), "Float store should NOT be dirty after Clear.");
            Assert.IsFalse(provider.IsDirty<int>(), "Int store should NOT be dirty after Clear.");
        }

        #endregion

        #region ClearDirtyState Tests

        [Test]
        public void ClearDirtyState_ShouldResetAllStoresToClean()
        {
            // Arrange
            var testData = CreateTestData(10, 4, true);
            var provider = testData.provider;

            provider.Set(new Vector2Int(0, 0), 1.0f);   // float store
            provider.Set(new Vector2Int(0, 1), 10);     // int store

            Assert.IsTrue(provider.IsDirty<float>(), "Float store should be dirty.");
            Assert.IsTrue(provider.IsDirty<int>(), "Int store should be dirty.");

            Assert.IsTrue(provider.AnyDirty, "Provider should report AnyDirty = true.");

            // Act
            provider.ClearDirtyState();

            // Assert
            Assert.IsFalse(provider.IsDirty<float>(), "Float store should be clean after ClearDirtyState.");
            Assert.IsFalse(provider.IsDirty<int>(), "Int store should be clean after ClearDirtyState.");

            Assert.IsFalse(provider.AnyDirty, "Provider should report AnyDirty = false after ClearDirtyState.");
        }

        [Test]
        public void ClearDirtyState_ShouldNotThrow_WhenEverythingIsAlreadyClean()
        {
            // Arrange
            var testData = CreateTestData(10, 4, true);
            var provider = testData.provider;

            // Act & Assert
            Assert.DoesNotThrow(() => provider.ClearDirtyState(),
                "ClearDirtyState should be idempotent and not throw if already clean.");
        }

        #endregion

        #region ClearDirty Tests

        [Test]
        public void ClearDirty_SpecificType_ShouldOnlyResetThatStore()
        {
            // Arrange
            var testData = CreateTestData(10, 4, true);
            var provider = testData.provider;

            provider.Set(new Vector2Int(0, 0), 1.0f); // Float dirty
            provider.Set(new Vector2Int(0, 1), 10);   // Int dirty

            Assert.IsTrue(provider.IsDirty<float>(), "Float should be dirty.");
            Assert.IsTrue(provider.IsDirty<int>(), "Int should be dirty.");

            // Act
            provider.ClearDirty<float>();

            // Assert
            Assert.IsFalse(provider.IsDirty<float>(), "Float store should be clean.");
            Assert.IsTrue(provider.IsDirty<int>(), "Int store should still be dirty.");
        }

        [Test]
        public void ClearDirty_ShouldThrow_WhenTypeIsNotRegistered()
        {
            // Arrange
            var testData = CreateTestData(10, 4, true);
            var provider = testData.provider;

            // Act & Assert
            Assert.Throws<InvalidOperationException>(() => provider.ClearDirty<byte>(),
                "Should throw when trying to clear dirty state for an unregistered type.");
        }

        #endregion

        #region Release Tests

        [Test]
        public void Release_ShouldRemoveKeyAndReturnCorrectIndex()
        {
            // Arrange
            var testData = CreateTestData(10, 4, true);
            var provider = testData.provider;
            var key = new Vector2Int(1, 1);

            // Set a value to obtain an index
            int expectedIndex = provider.Set(key, 42.0f);

            // Act
            int releasedIndex = provider.Release(key);

            // Assert
            Assert.AreEqual(expectedIndex, releasedIndex, "Release should return the index previously associated with the key.");

            // Verify that the mapper no longer knows this key
            Assert.IsFalse(provider.TryGetIndex(key, out _), "The key should no longer be present in the mapper after Release.");
        }

        /// <summary>
        /// Verifies that Release returns -1 if the key does not exist.
        /// </summary>
        [Test]
        public void Release_ShouldReturnMinusOne_WhenKeyDoesNotExist()
        {
            // Arrange
            var testData = CreateTestData(10, 4, true);
            var provider = testData.provider;
            var key = new Vector2Int(9, 9);

            // Act
            int releasedIndex = provider.Release(key);

            // Assert
            Assert.AreEqual(-1, releasedIndex, "Release should return -1 if the key was not found.");
        }

        [Test]
        public void Release_ShouldNotClearDataInStore()
        {
            // Arrange
            var testData = CreateTestData(10, 4, true);
            var provider = testData.provider;
            var key = new Vector2Int(2, 2);
            float value = 123.45f;

            int index = provider.Set(key, value);

            // Act
            provider.Release(key);

            // Assert
            // The mapper does not know the key, but the store retains the data at the index.
            var buffer = provider.GetRawBuffer<float>();
            Assert.AreEqual(value, buffer.TypedBuffer[index], "The data in the store should persist after Release.");
        }

        [Test]
        public void Release_AllowsKeyReaddition()
        {
            // Arrange
            var testData = CreateTestData(10, 4, true);
            var provider = testData.provider;
            var key = new Vector2Int(3, 3);

            // Act
            provider.Set(key, 10.0f);
            provider.Release(key);

            // Re-add the key
            int newIndex = provider.Set(key, 20.0f);

            // Assert
            Assert.GreaterOrEqual(newIndex, 0, "Should be able to re-add a previously released key.");
            Assert.AreEqual(20.0f, provider.Get<float>(key), "The new value should be correctly stored for the re-added key.");
        }

        #endregion

        #region GetOrAllocateIndex Tests

        [Test]
        public void GetOrAllocateIndex_ShouldReturnExistingIndex_WhenKeyAlreadyExists()
        {
            // Arrange
            var testData = CreateTestData(10, 4, true);
            var provider = testData.provider;
            var key = new Vector2Int(1, 1);
            int initialIndex = provider.Set(key, 10.0f);

            // Act
            int retrievedIndex = provider.GetOrAllocateIndex(key);

            // Assert
            Assert.AreEqual(initialIndex, retrievedIndex, "Should return the existing index for an already allocated key.");
        }

        [Test]
        public void GetOrAllocateIndex_ShouldAllocateNewIndex_WhenKeyIsNew()
        {
            // Arrange
            var testData = CreateTestData(10, 4, true);
            var provider = testData.provider;
            var key = new Vector2Int(5, 5);

            // Act
            int newIndex = provider.GetOrAllocateIndex(key);

            // Assert
            Assert.GreaterOrEqual(newIndex, 0, "Should allocate a valid non-negative index for a new key.");
            Assert.IsTrue(provider.TryGetIndex(key, out int actualIndex), "The new key should now be present in the mapper.");
            Assert.AreEqual(newIndex, actualIndex, "The allocated index should match the mapper's record.");
        }

        [Test]
        public void GetOrAllocateIndex_ShouldReturnSameIndex_OnSequentialCalls()
        {
            // Arrange
            var testData = CreateTestData(10, 4, true);
            var provider = testData.provider;
            var key = new Vector2Int(2, 2);

            // Act
            int firstCall = provider.GetOrAllocateIndex(key);
            int secondCall = provider.GetOrAllocateIndex(key);

            // Assert
            Assert.AreEqual(firstCall, secondCall, "Subsequent calls for the same key must return the same index.");
        }

        [Test]
        public void GetOrAllocateIndex_ShouldThrowInvalidOperationException_WhenCapacityIsFull()
        {
            // Arrange
            // Create a provider with a very small capacity (e.g., 1 slot)
            int capacity = 1;
            var testData = CreateTestData(capacity, 1, true);
            var provider = testData.provider;

            // Fill the only available slot
            provider.GetOrAllocateIndex(new Vector2Int(1, 1));

            // Act & Assert
            // Attempting to allocate a second key should fail
            var key = new Vector2Int(2, 2);

            var ex = Assert.Throws<InvalidOperationException>(() => provider.GetOrAllocateIndex(key),
                "Should throw InvalidOperationException when no more capacity is available.");
        }

        #endregion

        #region Reconfigure Tests

        [Test]
        public void Reconfigure_ShouldUpdateStateAndReturnTrue_WhenParamsChange()
        {
            // Arrange
            var testData = CreateTestData(10, 4, true);
            var provider = testData.provider;
            int newCapacity = 20;
            int newBatchSize = 8;

            // Act
            bool result = provider.Reconfigure(newCapacity, newBatchSize);

            // Assert
            Assert.IsTrue(result, "Reconfigure should return true when parameters have changed.");
            Assert.AreEqual(newCapacity, provider.Capacity, "Capacity should be updated.");
            Assert.AreEqual(newBatchSize, provider.BatchSize, "BatchSize should be updated.");
        }

        [Test]
        public void Reconfigure_ShouldClearAndReturnFalse_WhenParamsRemainSame()
        {
            // Arrange
            int capacity = 10;
            int batchSize = 4;
            var testData = CreateTestData(capacity, batchSize, true);
            var provider = testData.provider;

            // Fill with data to verify Clear() is called
            provider.Set(new Vector2Int(1, 1), 10.0f);
            Assert.AreEqual(1, provider.Count);

            // Act
            bool result = provider.Reconfigure(capacity, batchSize);

            // Assert
            Assert.IsFalse(result, "Reconfigure should return false when parameters are unchanged.");
            Assert.AreEqual(0, provider.Count, "Provider should be cleared when parameters are unchanged.");
        }

        [Test]
        public void Reconfigure_ShouldResizeAllStores()
        {
            // Arrange
            var testData = CreateTestData(10, 4, true);
            var provider = testData.provider;
            int newCapacity = 50;

            // Act
            provider.Reconfigure(newCapacity, 4);

            // Assert
            // Assuming GetReadOnlyBuffer exists and exposes the underlying store capacity
            var buffer = provider.GetRawBuffer<float>();
            Assert.AreEqual(newCapacity, buffer.TypedBuffer.Length, "The underlying store buffer should be resized.");
        }

        [Test]
        public void Reconfigure_ShouldResetInternalStructures()
        {
            // Arrange
            var testData = CreateTestData(10, 4, true);
            var provider = testData.provider;
            provider.Set(new Vector2Int(1, 1), 10.0f);

            // Act
            provider.Reconfigure(20, 8);

            // Assert
            Assert.AreEqual(0, provider.Count, "Count should be reset to zero after reconfiguration.");
            Assert.IsFalse(provider.TryGetIndex(new Vector2Int(1, 1), out _), "Old mapping should be removed.");
        }

        [Test]
        public void Reconfigure_OnlyChangesCapacity_UpdatesCorrectly()
        {
            var testData = CreateTestData(10, 4, true);
            var provider = testData.provider;
            int newCapacity = 20;

            bool result = provider.Reconfigure(newCapacity, 4);

            Assert.IsTrue(result);
            Assert.AreEqual(newCapacity, provider.Capacity);
            Assert.AreEqual(4, provider.BatchSize);
        }

        [Test]
        public void Reconfigure_OnlyChangesBatchSize_UpdatesCorrectly()
        {
            var testData = CreateTestData(10, 4, true);
            var provider = testData.provider;
            int newBatchSize = 8;

            bool result = provider.Reconfigure(10, newBatchSize);

            Assert.IsTrue(result);
            Assert.AreEqual(10, provider.Capacity);
            Assert.AreEqual(newBatchSize, provider.BatchSize);
        }

        [Test]
        public void Reconfigure_InvalidCapacity_ThrowsException()
        {
            var testData = CreateTestData(10, 4, true);
            var provider = testData.provider;

            Assert.Throws<ArgumentOutOfRangeException>(() => provider.Reconfigure(0, 4));
            Assert.Throws<ArgumentOutOfRangeException>(() => provider.Reconfigure(-5, 4));
        }

        [Test]
        public void Reconfigure_InvalidBatchSize_ThrowsException()
        {
            var testData = CreateTestData(10, 4, true);
            var provider = testData.provider;

            Assert.Throws<ArgumentOutOfRangeException>(() => provider.Reconfigure(10, 0));
            Assert.Throws<ArgumentOutOfRangeException>(() => provider.Reconfigure(10, -2));
        }

        #endregion

        #region Resize Tests

        [Test]
        public void Resize_SameCapacity_ReturnsFalse()
        {
            var testData = CreateTestData(10, 4, true);
            var provider = testData.provider;

            bool result = provider.Resize(10);

            Assert.IsFalse(result);
            Assert.AreEqual(10, provider.Capacity);
        }

        [Test]
        public void Resize_NewCapacity_UpdatesStateAndReturnsTrue()
        {
            var testData = CreateTestData(10, 4, true);
            var provider = testData.provider;
            int newCapacity = 25;

            bool result = provider.Resize(newCapacity);

            Assert.IsTrue(result);
            Assert.AreEqual(newCapacity, provider.Capacity);
        }

        [Test]
        public void Resize_InvalidCapacity_ThrowsException()
        {
            var testData = CreateTestData(10, 4, true);
            var provider = testData.provider;

            Assert.Throws<ArgumentOutOfRangeException>(() => provider.Resize(0));
            Assert.Throws<ArgumentOutOfRangeException>(() => provider.Resize(-10));
        }

        [Test]
        public void Resize_ResizesMapperAndStores()
        {
            var testData = CreateTestData(10, 4, true);
            var provider = testData.provider;
            int newCapacity = 50;

            provider.Resize(newCapacity);

            // Verify store resize
            var buffer = provider.GetRawBuffer<float>();
            Assert.AreEqual(newCapacity, buffer.TypedBuffer.Length);

            // Verify mapper reset (should not contain old keys)
            var key = new Vector2Int(1, 1);
            provider.Set(key, 1.0f);
            provider.Resize(newCapacity);
            Assert.IsFalse(provider.TryGetIndex(key, out _));
        }

        [Test]
        public void Resize_PreservesBatchSize()
        {
            int initialBatchSize = 4;
            var testData = CreateTestData(10, initialBatchSize, true);
            var provider = testData.provider;

            provider.Resize(20);

            Assert.AreEqual(initialBatchSize, provider.BatchSize);
        }

        #endregion

        #endregion

        #region IReadOnlyGpuDataProvider

        #region Property Tests

        [Test]
        public void ProviderProperties_ShouldReturnCorrectInitialValues()
        {
            // Arrange
            int length = 16;
            int batchSize = 4;
            var testData = CreateTestData(length, batchSize, true);
            var provider = testData.provider;

            // Assert
            Assert.AreEqual(length, provider.Capacity, "Capacity should match the initialized length.");
            Assert.AreEqual(batchSize, provider.BatchSize, "BatchSize should match the initialized batch size.");
            Assert.AreEqual(0, provider.Count, "Count should be 0 when no keys are set.");
            Assert.AreEqual(-1, provider.HighestIndex, "HighestIndex should be -1 (or 0, depending on implementation) when no keys are set.");
        }

        [Test]
        public void Count_ShouldIncrease_WhenNewKeysAreAdded()
        {
            // Arrange
            var testData = CreateTestData(10, 4, true);
            var provider = testData.provider;

            // Act & Assert
            Assert.AreEqual(0, provider.Count);

            provider.Set(new Vector2Int(0, 0), 1.0f);
            Assert.AreEqual(1, provider.Count, "Count should be 1 after setting the first key.");

            provider.Set(new Vector2Int(0, 1), 2.0f);
            Assert.AreEqual(2, provider.Count, "Count should be 2 after setting a second unique key.");

            provider.Set(new Vector2Int(0, 0), 3.0f);
            Assert.AreEqual(2, provider.Count, "Count should remain 2 when updating an existing key.");
        }

        [Test]
        public void HighestIndex_ShouldTrackMaximumAllocatedSlot()
        {
            // Arrange
            var testData = CreateTestData(10, 4, true);
            var provider = testData.provider;

            // Act & Assert
            // Wir gehen davon aus, dass Set den Index zurückgibt
            int idx1 = provider.Set(new Vector2Int(1, 1), 10.0f); // Index 0
            Assert.AreEqual(idx1, provider.HighestIndex);

            int idx2 = provider.Set(new Vector2Int(2, 2), 20.0f); // Index 1
            Assert.AreEqual(idx2, provider.HighestIndex);

            // Update eines existierenden Keys sollte HighestIndex nicht verringern, 
            // aber auch nicht erhöhen, wenn es nicht der höchste ist
            provider.Set(new Vector2Int(1, 1), 30.0f);
            Assert.AreEqual(idx2, provider.HighestIndex, "HighestIndex should not change when updating a non-highest key.");
        }

        [Test]
        public void CapacityAndBatchSize_ShouldBeReadOnly()
        {
            // Arrange
            var testData = CreateTestData(10, 4, true);
            var provider = testData.provider;

            // Da die Properties keine Setter haben sollten, können wir hier höchstens 
            // die Werte prüfen, die beim Setup übergeben wurden.
            Assert.AreEqual(10, provider.Capacity);
            Assert.AreEqual(4, provider.BatchSize);
        }

        #endregion

        #region GetReadOnlyBuffer Tests

        [Test]
        public void GetReadOnlyBuffer_ReturnsCorrectDataForAllRegisteredStores()
        {
            // Arrange
            int length = 10;
            int batchSize = 4;
            var testData = CreateTestData(length, batchSize);
            var provider = testData.provider;

            // Act & Assert for int
            var bufferInt = provider.GetReadOnlyBuffer<int>().AsSpan();
            Assert.IsFalse(bufferInt.IsEmpty);
            var expectedInt = (int[])testData.expected[typeof(int)];
            for (int i = 0; i < length; i++) Assert.AreEqual(expectedInt[i], bufferInt[i]);

            // Act & Assert for float
            var bufferFloat = provider.GetReadOnlyBuffer<float>().AsSpan();
            Assert.IsFalse(bufferFloat.IsEmpty);
            var expectedFloat = (float[])testData.expected[typeof(float)];
            for (int i = 0; i < length; i++) Assert.AreEqual(expectedFloat[i], bufferFloat[i]);

            // Act & Assert for long
            var bufferLong = provider.GetReadOnlyBuffer<long>().AsSpan();
            Assert.IsFalse(bufferLong.IsEmpty);
            var expectedLong = (long[])testData.expected[typeof(long)];
            for (int i = 0; i < length; i++) Assert.AreEqual(expectedLong[i], bufferLong[i]);

            // Act & Assert for double
            var bufferDouble = provider.GetReadOnlyBuffer<double>().AsSpan();
            Assert.IsFalse(bufferDouble.IsEmpty);
            var expectedDouble = (double[])testData.expected[typeof(double)];
            for (int i = 0; i < length; i++) Assert.AreEqual(expectedDouble[i], bufferDouble[i]);
        }

        [Test]
        public void GetReadOnlyBuffer_UnregisteredType_ReturnsNull()
        {
            // Arrange
            int length = 10;
            int batchSize = 4;
            var testData = CreateTestData(length, batchSize);
            var provider = testData.provider;

            // Act & Assert
            Assert.IsNull(provider.GetReadOnlyBuffer<byte>(), "Calling GetReadOnlyBuffer for an unregistered type should return null.");
        }

        #endregion

        #region Get Tests

        [Test]
        public void Get_ShouldThrowKeyNotFoundException_WhenKeyDoesNotExist()
        {
            // Arrange
            var testData = CreateTestData(10, 4, true);
            var provider = testData.provider;
            var key = new Vector2Int(9, 9);

            // Act & Assert
            Assert.Throws<KeyNotFoundException>(() => provider.Get<float>(key),
                "Get should throw KeyNotFoundException when the key has not been set.");
        }

        [Test]
        public void Get_ShouldThrowInvalidOperationException_WhenStoreTypeIsNotRegistered()
        {
            // Arrange
            var testData = CreateTestData(10, 4);
            var provider = testData.provider;
            var key = new Vector2Int(0, 0);

            // Act & Assert
            var ex = Assert.Throws<InvalidOperationException>(() => provider.Get<byte>(key));

            Assert.That(ex.Message, Does.Contain(typeof(byte).Name),
                "Exception should indicate that the store for the requested type 'byte' is not registered.");
        }

        [Test]
        public void Get_ShouldReturnCorrectValue_AfterMultipleUpdates()
        {
            // Arrange
            var testData = CreateTestData(10, 4, true);
            var provider = testData.provider;
            var key = new Vector2Int(2, 2);

            // Act
            provider.Set(key, 100.0f);
            provider.Set(key, 200.0f);
            provider.Set(key, 300.0f);

            // Assert
            Assert.AreEqual(300.0f, provider.Get<float>(key), "Get should return the most recently set value for the key.");
        }

        [Test]
        public void Get_ShouldReturnCorrectValue_ForSpecificKeyAmongOthers()
        {
            // Arrange
            var testData = CreateTestData(10, 4, true);
            var provider = testData.provider;

            var keyA = new Vector2Int(1, 0);
            var keyB = new Vector2Int(0, 1);

            provider.Set(keyA, 5.0f);
            provider.Set(keyB, 10.0f);

            // Act & Assert
            Assert.AreEqual(5.0f, provider.Get<float>(keyA), "Get should return the value for keyA without interference from keyB.");
            Assert.AreEqual(10.0f, provider.Get<float>(keyB), "Get should return the value for keyB without interference from keyA.");
        }

        #endregion

        #region TryGet Tests

        [Test]
        public void TryGet_ShouldReturnTrue_WhenKeyExists()
        {
            // Arrange
            var testData = CreateTestData(10, 4, true);
            var provider = testData.provider;
            var key = new Vector2Int(1, 1);
            float expectedValue = 42.0f;
            provider.Set(key, expectedValue);

            // Act
            bool success = provider.TryGet(key, out float actualValue);

            // Assert
            Assert.IsTrue(success, "TryGet should return true when the key exists.");
            Assert.AreEqual(expectedValue, actualValue, "The retrieved value should match the set value.");
        }

        [Test]
        public void TryGet_ShouldReturnFalse_WhenKeyDoesNotExist()
        {
            // Arrange
            var testData = CreateTestData(10, 4, true);
            var provider = testData.provider;
            var key = new Vector2Int(9, 9);

            // Act
            bool success = provider.TryGet(key, out float value);

            // Assert
            Assert.IsFalse(success, "TryGet should return false when the key does not exist.");
            Assert.AreEqual(default(float), value, "Value should be default when TryGet returns false.");
        }

        [Test]
        public void TryGet_ShouldThrowInvalidOperationException_WhenStoreTypeIsNotRegistered()
        {
            // Arrange
            var testData = CreateTestData(10, 4);
            var provider = testData.provider;
            var key = new Vector2Int(0, 0);

            // Act & Assert
            var ex = Assert.Throws<InvalidOperationException>(() => provider.TryGet<byte>(key, out _));

            Assert.That(ex.Message, Does.Contain(typeof(byte).Name),
                "Exception should clearly indicate that the store for 'byte' was not registered.");
        }

        #endregion

        #region TryGetIndex Tests

        [Test]
        public void TryGetIndex_ShouldReturnTrueAndCorrectIndex_WhenKeyExists()
        {
            // Arrange
            var testData = CreateTestData(10, 4, true);
            var provider = testData.provider;
            var key = new Vector2Int(2, 2);
            int expectedIndex = provider.Set(key, 10.0f);

            // Act
            bool success = provider.TryGetIndex(key, out int actualIndex);

            // Assert
            Assert.IsTrue(success, "TryGetIndex should return true for an existing key.");
            Assert.AreEqual(expectedIndex, actualIndex, "The returned index should match the allocated index.");
        }

        [Test]
        public void TryGetIndex_ShouldReturnFalse_WhenKeyDoesNotExist()
        {
            // Arrange
            var testData = CreateTestData(10, 4, true);
            var provider = testData.provider;
            var key = new Vector2Int(9, 9); // Key that has not been set

            // Act
            bool success = provider.TryGetIndex(key, out int index);

            // Assert
            Assert.IsFalse(success, "TryGetIndex should return false for a non-existent key.");
            Assert.AreEqual(0, index, "The index out-parameter should be 0 (default) when the key is not found.");
        }

        [Test]
        public void TryGetIndex_ShouldReturnFalse_AfterKeyIsReleased()
        {
            // Arrange
            var testData = CreateTestData(10, 4, true);
            var provider = testData.provider;
            var key = new Vector2Int(1, 1);
            provider.Set(key, 5.0f);
            provider.Release(key);

            // Act
            bool success = provider.TryGetIndex(key, out int index);

            // Assert
            Assert.IsFalse(success, "TryGetIndex should return false after the key has been released.");
        }

        #endregion

        #endregion
    }
}
