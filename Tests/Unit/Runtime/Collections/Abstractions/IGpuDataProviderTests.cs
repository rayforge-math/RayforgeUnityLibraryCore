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

        #region Clear

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

        #endregion
    }
}
