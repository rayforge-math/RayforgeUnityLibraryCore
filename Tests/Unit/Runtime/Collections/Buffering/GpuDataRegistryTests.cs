using NUnit.Framework;
using Rayforge.Core.Collections.Abstractions;
using Rayforge.Core.Collections.Abstractions.Tests;
using Rayforge.Core.TestEnv;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SocialPlatforms.Impl;

namespace Rayforge.Core.Collections.Buffering.Tests
{
    [TestFixture]
    public class GpuDataRegistryTests : IGpuDataProviderTests
    {
        #region Create Test Env

        protected override IGpuDataProvider<Vector2Int> CreateProvider(Dictionary<Type, Array> expected, Vector2Int[] keys, int length, int batchSize)
        {
            var registry = new GpuDataRegistry<Vector2Int>(length, batchSize);

            registry.AddStore<int>();
            registry.AddStore<float>();
            registry.AddStore<long>();
            registry.AddStore<double>();

            var intArr = (int[])expected[typeof(int)];
            var floatArr = (float[])expected[typeof(float)];
            var longArr = (long[])expected[typeof(long)];
            var doubleArr = (double[])expected[typeof(double)];

            for (int i = 0; i < length; i++)
            {
                Vector2Int key = keys[i];

                registry.Set(key, intArr[i]);
                registry.Set(key, floatArr[i]);
                registry.Set(key, longArr[i]);
                registry.Set(key, doubleArr[i]);
            }

            return registry;
        }

        protected override IGpuDataProvider<Vector2Int> CreateProvider(int length, int batchSize)
        {
            var registry = new GpuDataRegistry<Vector2Int>(length, batchSize);

            registry.AddStore<int>();
            registry.AddStore<float>();
            registry.AddStore<long>();
            registry.AddStore<double>();

            return registry;
        }

        #endregion

        #region Constructor Tests

        [Test]
        public void Constructor_ValidParameters_SetsPropertiesCorrectly()
        {
            // Arrange
            int capacity = 128;
            int batchSize = 16;

            // Act
            var registry = new GpuDataRegistry<Vector2Int>(capacity, batchSize);

            // Assert
            Assert.AreEqual(capacity, registry.Capacity, "Registry capacity should match the constructor argument.");
            Assert.AreEqual(batchSize, registry.BatchSize, "Registry batch size should match the constructor argument.");
        }

        [Test]
        public void Constructor_ZeroCapacity_ThrowsArgumentOutOfRangeException()
        {
            // Act & Assert
            Assert.Throws<ArgumentOutOfRangeException>(() => new GpuDataRegistry<Vector2Int>(0, 16),
                "Registry should throw if capacity is zero.");
        }

        [Test]
        public void Constructor_NegativeCapacity_ThrowsArgumentOutOfRangeException()
        {
            // Act & Assert
            Assert.Throws<ArgumentOutOfRangeException>(() => new GpuDataRegistry<Vector2Int>(-10, 16),
                "Registry should throw if capacity is negative.");
        }

        [Test]
        public void Constructor_ZeroBatchSize_ThrowsArgumentOutOfRangeException()
        {
            // Act & Assert
            Assert.Throws<ArgumentOutOfRangeException>(() => new GpuDataRegistry<Vector2Int>(32, 0),
                "Registry should throw if batch size is zero.");
        }

        [Test]
        public void Constructor_NegativeBatchSize_ThrowsArgumentOutOfRangeException()
        {
            // Act & Assert
            Assert.Throws<ArgumentOutOfRangeException>(() => new GpuDataRegistry<Vector2Int>(32, -5),
                "Registry should throw if batch size is negative.");
        }

        #endregion

        #region AddStore Tests

        [Test]
        public void AddStore_NewType_CreatesNewStore()
        {
            // Arrange
            var registry = new GpuDataRegistry<Vector2Int>(32, 4);

            // Act
            var store = registry.AddStore<float>();

            // Assert
            Assert.IsNotNull(store, "Store should be created successfully.");
            Assert.IsInstanceOf<MetadataStore<float>>(store);
        }

        [Test]
        public void AddStore_AlreadyRegistered_ReturnsExistingStore()
        {
            // Arrange
            var registry = new GpuDataRegistry<Vector2Int>(32, 4);
            var firstStore = registry.AddStore<float>();

            // Act
            var secondStore = registry.AddStore<float>();

            // Assert
            Assert.AreSame(firstStore, secondStore, "Registry should return the exact same instance for the same type.");
        }

        [Test]
        public void AddStore_MultipleTypes_CreatesDistinctStores()
        {
            // Arrange
            var registry = new GpuDataRegistry<Vector2Int>(32, 4);

            // Act
            var floatStore = registry.AddStore<float>();
            var intStore = registry.AddStore<int>();

            // Assert
            Assert.AreNotSame(floatStore, intStore, "Registry should maintain separate stores for different types.");
            Assert.IsInstanceOf<MetadataStore<float>>(floatStore);
            Assert.IsInstanceOf<MetadataStore<int>>(intStore);
        }

        [Test]
        public void AddStore_VerifyStoreInitializationParameters()
        {
            // Arrange
            int capacity = 64;
            int batchSize = 8;
            var registry = new GpuDataRegistry<Vector2Int>(capacity, batchSize);

            // Act
            var store = registry.AddStore<float>();

            // Assert
            // Assuming MetadataStore has public accessors for these values
            Assert.AreEqual(capacity, store.Capacity, "Store should be initialized with the registry's capacity.");
            Assert.AreEqual(batchSize, store.BatchSize, "Store should be initialized with the registry's batch size.");
        }

        #endregion

        #region GetStore Tests

        [Test]
        public void GetStore_ExistingStore_ReturnsCorrectInstance()
        {
            // Arrange
            var registry = new GpuDataRegistry<Vector2Int>(32, 4);
            var originalStore = registry.AddStore<float>();

            // Act
            var retrievedStore = registry.GetStore<float>();

            // Assert
            Assert.IsNotNull(retrievedStore, "GetStore should return the registered store.");
            Assert.AreSame(originalStore, retrievedStore, "GetStore should return the exact instance that was added.");
        }

        [Test]
        public void GetStore_NonExistentStore_ReturnsNull()
        {
            // Arrange
            var registry = new GpuDataRegistry<Vector2Int>(32, 4);

            // Act
            var store = registry.GetStore<int>();

            // Assert
            Assert.IsNull(store, "GetStore should return null if the requested type was not added yet.");
        }

        [Test]
        public void GetStore_AfterAddingDifferentType_ReturnsNullForRequestedType()
        {
            // Arrange
            var registry = new GpuDataRegistry<Vector2Int>(32, 4);
            registry.AddStore<float>();

            // Act
            var store = registry.GetStore<int>();

            // Assert
            Assert.IsNull(store, "GetStore should return null for types that haven't been added, even if other types exist.");
        }

        [Test]
        public void GetStore_MultipleTypes_ReturnsCorrectTypeInstance()
        {
            // Arrange
            var registry = new GpuDataRegistry<Vector2Int>(32, 4);
            var floatStore = registry.AddStore<float>();
            var intStore = registry.AddStore<int>();

            // Act
            var retrievedFloat = registry.GetStore<float>();
            var retrievedInt = registry.GetStore<int>();

            // Assert
            Assert.AreSame(floatStore, retrievedFloat, "Retrieved float store should match the added instance.");
            Assert.AreSame(intStore, retrievedInt, "Retrieved int store should match the added instance.");
        }

        #endregion
    }
}
