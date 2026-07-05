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

        protected abstract IGpuDataProvider<Vector2> CreateProvider(Dictionary<Type, Array> expected, Vector2[] keys, int length, int batchSize);

        private GpuDataProviderTestData CreateTestData(int length, int batchSize)
        {
            var expected = new Dictionary<Type, Array>();

            expected[typeof(int)] = TestUtility.CreateSampleItems<int>(length);
            expected[typeof(float)] = TestUtility.CreateSampleItems<float>(length);
            expected[typeof(long)] = TestUtility.CreateSampleItems<long>(length);
            expected[typeof(double)] = TestUtility.CreateSampleItems<double>(length);

            var keys = new Vector2[length];

            for (int i = 0; i < length; ++i)
            {
                keys[i] = new Vector2(i, 0);
            }

            var registry = CreateProvider(expected, keys, length, batchSize);

            return new GpuDataProviderTestData
            {
                keys = keys,
                provider = registry,
                expected = expected
            };
        }

        #endregion

        #region IGpuDataProvider

        #region GetTypedBuffer Tests

        [Test]
        public void GetTypedBuffer_ReturnsCorrectDataForAllRegisteredStores()
        {
            // Arrange
            int length = 10;
            int batchSize = 4;
            var testData = CreateTestData(length, batchSize);
            var provider = testData.provider;

            // Act & Assert for int
            var bufferInt = provider.GetTypedBuffer<int>();
            Assert.IsNotNull(bufferInt);
            var expectedInt = (int[])testData.expected[typeof(int)];
            for (int i = 0; i < length; i++) Assert.AreEqual(expectedInt[i], bufferInt[i]);

            // Act & Assert for float
            var bufferFloat = provider.GetTypedBuffer<float>();
            Assert.IsNotNull(bufferFloat);
            var expectedFloat = (float[])testData.expected[typeof(float)];
            for (int i = 0; i < length; i++) Assert.AreEqual(expectedFloat[i], bufferFloat[i]);

            // Act & Assert for long
            var bufferLong = provider.GetTypedBuffer<long>();
            Assert.IsNotNull(bufferLong);
            var expectedLong = (long[])testData.expected[typeof(long)];
            for (int i = 0; i < length; i++) Assert.AreEqual(expectedLong[i], bufferLong[i]);

            // Act & Assert for double
            var bufferDouble = provider.GetTypedBuffer<double>();
            Assert.IsNotNull(bufferDouble);
            var expectedDouble = (double[])testData.expected[typeof(double)];
            for (int i = 0; i < length; i++) Assert.AreEqual(expectedDouble[i], bufferDouble[i]);
        }

        [Test]
        public void GetTypedBuffer_UnregisteredType_ThrowsInvalidOperationException()
        {
            // Arrange
            int length = 10;
            int batchSize = 4;
            var testData = CreateTestData(length, batchSize);
            var provider = testData.provider;

            // Act & Assert
            Assert.Throws<InvalidOperationException>(() =>
            {
                provider.GetTypedBuffer<byte>();
            }, "Calling GetTypedBuffer for an unregistered type should throw an InvalidOperationException.");
        }

        #endregion

        #region GetUntypedBuffer Tests

        [Test]
        public void GetUntypedBuffer_ReturnsCorrectDataForAllRegisteredStores()
        {
            // Arrange
            int length = 10;
            int batchSize = 4;
            var testData = CreateTestData(length, batchSize);
            var provider = testData.provider;

            // Act & Assert for int
            var bufferInt = (int[])provider.GetUntypedBuffer<int>();
            Assert.IsNotNull(bufferInt);
            var expectedInt = (int[])testData.expected[typeof(int)];
            for (int i = 0; i < length; i++) Assert.AreEqual(expectedInt[i], bufferInt[i]);

            // Act & Assert for float
            var bufferFloat = (float[])provider.GetUntypedBuffer<float>();
            Assert.IsNotNull(bufferFloat);
            var expectedFloat = (float[])testData.expected[typeof(float)];
            for (int i = 0; i < length; i++) Assert.AreEqual(expectedFloat[i], bufferFloat[i]);

            // Act & Assert for long
            var bufferLong = (long[])provider.GetUntypedBuffer<long>();
            Assert.IsNotNull(bufferLong);
            var expectedLong = (long[])testData.expected[typeof(long)];
            for (int i = 0; i < length; i++) Assert.AreEqual(expectedLong[i], bufferLong[i]);

            // Act & Assert for double
            var bufferDouble = (double[])provider.GetUntypedBuffer<double>();
            Assert.IsNotNull(bufferDouble);
            var expectedDouble = (double[])testData.expected[typeof(double)];
            for (int i = 0; i < length; i++) Assert.AreEqual(expectedDouble[i], bufferDouble[i]);
        }

        [Test]
        public void GetUntypedBuffer_UnregisteredType_ThrowsInvalidOperationException()
        {
            // Arrange
            int length = 10;
            int batchSize = 4;
            var testData = CreateTestData(length, batchSize);
            var provider = testData.provider;

            // Act & Assert
            Assert.Throws<InvalidOperationException>(() =>
            {
                provider.GetUntypedBuffer<byte>();
            }, "Calling GetTypedBuffer for an unregistered type should throw an InvalidOperationException.");
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
            var bufferInt = provider.GetTypedBuffer<int>();
            for (int i = 0; i < length; i++)
                Assert.AreEqual(0, bufferInt[i], $"int buffer index {i} not cleared.");

            // Check float
            var bufferFloat = provider.GetTypedBuffer<float>();
            for (int i = 0; i < length; i++)
                Assert.AreEqual(0f, bufferFloat[i], $"float buffer index {i} not cleared.");

            // Check long
            var bufferLong = provider.GetTypedBuffer<long>();
            for (int i = 0; i < length; i++)
                Assert.AreEqual(0L, bufferLong[i], $"long buffer index {i} not cleared.");

            // Check double
            var bufferDouble = provider.GetTypedBuffer<double>();
            for (int i = 0; i < length; i++)
                Assert.AreEqual(0.0, bufferDouble[i], $"double buffer index {i} not cleared.");
        }

        #endregion

        #endregion
    }
}
