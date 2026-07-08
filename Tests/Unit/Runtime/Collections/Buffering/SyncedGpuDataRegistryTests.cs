using NUnit.Framework;
using Rayforge.Core.Collections.Abstractions;
using Rayforge.Core.TestEnv;
using System.Collections.Generic;
using UnityEngine;

namespace Rayforge.Core.Collections.Buffering.Tests
{
    [TestFixture]
    public class SyncedGpuDataRegistryTests
    {
        #region Test Structs

        public struct PositionData : IGpuData<PositionData>
        {
            public Vector3 Value;

            public bool IsValid => Value != new Vector3(float.MinValue, 0, 0);

            public PositionData InvalidData() => new PositionData { Value = new Vector3(float.MinValue, 0, 0) };
        }

        public struct RotationData : IGpuData<RotationData>
        {
            public float Angle;

            public bool IsValid => Angle != float.MinValue;

            public RotationData InvalidData() => new RotationData { Angle = float.MinValue };
        }

        #endregion

        #region Constructor

        [Test]
        public void Constructor_ValidParameters_InitializesStoresAndCapacity()
        {
            // Arrange
            int capacity = 128;
            int batchSize = 16;

            // Act
            var registry = new SyncedGpuDataRegistry<Vector2, PositionData, RotationData>(capacity, batchSize);

            // Assert
            Assert.AreEqual(capacity, registry.Capacity, "Capacity should be set correctly.");
            Assert.AreEqual(batchSize, registry.BatchSize, "BatchSize should be set correctly.");

            Assert.IsNotNull(registry.StoreARawBuffer, "StoreA should be initialized.");
            Assert.IsNotNull(registry.StoreBRawBuffer, "StoreB should be initialized.");
        }

        #endregion

        #region Property Tests

        [Test]
        public void PropertyAccessors_ExposeCorrectInterfaces()
        {
            // Arrange
            var registry = new SyncedGpuDataRegistry<Vector2, PositionData, RotationData>(64, 8);

            // Assert - Verify Metadata interfaces
            Assert.IsInstanceOf<IBufferMetadata>(registry.StoreAMetadata, "StoreAMetadata should implement IBufferMetadata.");
            Assert.IsInstanceOf<IBufferMetadata>(registry.StoreBMetadata, "StoreBMetadata should implement IBufferMetadata.");

            // Assert - Verify RawBuffer interfaces
            Assert.IsInstanceOf<IRawBuffer<PositionData>>(registry.StoreARawBuffer, "StoreARawBuffer should implement IRawBuffer<PositionData>.");
            Assert.IsInstanceOf<IRawBuffer<RotationData>>(registry.StoreBRawBuffer, "StoreBRawBuffer should implement IRawBuffer<RotationData>.");

            // Assert - Verify Iterable interfaces
            Assert.IsInstanceOf<IIterable<PositionData>>(registry.StoreAIterable, "StoreAIterable should implement IIterable<PositionData>.");
            Assert.IsInstanceOf<IIterable<RotationData>>(registry.StoreBIterable, "StoreBIterable should implement IIterable<RotationData>.");
        }

        [Test]
        public void PropertyAccessors_ReferenceConsistency()
        {
            // Arrange
            var registry = new SyncedGpuDataRegistry<Vector2, PositionData, RotationData>(64, 8);

            // Assert for Store A: All interfaces must point to the same internal StoreA instance
            Assert.AreSame(registry.StoreARawBuffer, registry.StoreAMetadata, "StoreA RawBuffer and Metadata should refer to the same instance.");
            Assert.AreSame(registry.StoreARawBuffer, registry.StoreAIterable, "StoreA RawBuffer and Iterable should refer to the same instance.");

            // Assert for Store B: All interfaces must point to the same internal StoreB instance
            Assert.AreSame(registry.StoreBRawBuffer, registry.StoreBMetadata, "StoreB RawBuffer and Metadata should refer to the same instance.");
            Assert.AreSame(registry.StoreBRawBuffer, registry.StoreBIterable, "StoreB RawBuffer and Iterable should refer to the same instance.");
        }

        #endregion

        #region Set Tests

        [Test]
        public void Set_StoresDataCorrectlyInBothStores()
        {
            // Arrange
            var registry = new SyncedGpuDataRegistry<Vector2, PositionData, RotationData>(128, 16);
            var key = new Vector2(10f, 20f);
            var valA = new PositionData { Value = new Vector3(1f, 2f, 3f) };
            var valB = new RotationData { Angle = 45.0f };

            // Act
            int index = registry.Set(key, valA, valB);

            // Assert
            // Verify that data can be retrieved from the store buffers directly
            var storedA = registry.StoreARawBuffer.TypedBuffer[index];
            var storedB = registry.StoreBRawBuffer.TypedBuffer[index];

            Assert.AreEqual(valA.Value, storedA.Value, "StoreA should contain the expected value.");
            Assert.AreEqual(valB.Angle, storedB.Angle, "StoreB should contain the expected value.");
        }

        [Test]
        public void Set_ReturnsConsistentIndexForSameKey()
        {
            // Arrange
            var registry = new SyncedGpuDataRegistry<Vector2, PositionData, RotationData>(128, 16);
            var key = new Vector2(5f, 5f);

            // Act
            int firstSetIndex = registry.Set(key, new PositionData(), new RotationData());
            int secondSetIndex = registry.Set(key, new PositionData(), new RotationData());

            // Assert
            Assert.AreEqual(firstSetIndex, secondSetIndex, "Setting the same key twice should return the same index.");
        }

        [Test]
        public void Set_AssignsDifferentIndicesForDifferentKeys()
        {
            // Arrange
            var registry = new SyncedGpuDataRegistry<Vector2, PositionData, RotationData>(128, 16);
            var key1 = new Vector2(1f, 1f);
            var key2 = new Vector2(2f, 2f);

            // Act
            int index1 = registry.Set(key1, new PositionData(), new RotationData());
            int index2 = registry.Set(key2, new PositionData(), new RotationData());

            // Assert
            Assert.AreNotEqual(index1, index2, "Different keys should be assigned to different indices.");
        }

        #endregion

        #region TryGet Tests

        [Test]
        public void TryGet_ExistingKey_ReturnsTrueAndCorrectValues()
        {
            // Arrange
            var registry = new SyncedGpuDataRegistry<Vector2, PositionData, RotationData>(128, 16);
            var key = new Vector2(5f, 5f);
            var valA = new PositionData { Value = new Vector3(10f, 0f, 0f) };
            var valB = new RotationData { Angle = 90f };
            registry.Set(key, valA, valB);

            // Act
            bool success = registry.TryGet(key, out var outA, out var outB);

            // Assert
            Assert.IsTrue(success, "TryGet should return true for an existing key.");
            Assert.AreEqual(valA.Value, outA.Value, "StoreA value mismatch.");
            Assert.AreEqual(valB.Angle, outB.Angle, "StoreB value mismatch.");
        }

        [Test]
        public void TryGet_NonExistentKey_ReturnsFalseAndDefaultValues()
        {
            // Arrange
            var registry = new SyncedGpuDataRegistry<Vector2, PositionData, RotationData>(128, 16);
            var key = new Vector2(999f, 999f);

            // Act
            bool success = registry.TryGet(key, out var outA, out var outB);

            // Assert
            Assert.IsFalse(success, "TryGet should return false for a non-existent key.");
            Assert.AreEqual(default(PositionData), outA, "OutA should be default.");
            Assert.AreEqual(default(RotationData), outB, "OutB should be default.");
        }

        #endregion

        #region Get Tests

        [Test]
        public void Get_ExistingKey_ReturnsCorrectValues()
        {
            // Arrange
            var registry = new SyncedGpuDataRegistry<Vector2, PositionData, RotationData>(128, 16);
            var key = new Vector2(1f, 1f);
            var valA = new PositionData { Value = new Vector3(5f, 5f, 5f) };
            var valB = new RotationData { Angle = 180f };
            registry.Set(key, valA, valB);

            // Act
            registry.Get(key, out var outA, out var outB);

            // Assert
            Assert.AreEqual(valA.Value, outA.Value, "StoreA value mismatch.");
            Assert.AreEqual(valB.Angle, outB.Angle, "StoreB value mismatch.");
        }

        [Test]
        public void Get_NonExistentKey_ThrowsKeyNotFoundException()
        {
            // Arrange
            var registry = new SyncedGpuDataRegistry<Vector2, PositionData, RotationData>(128, 16);
            var key = new Vector2(999f, 999f);

            // Assert
            Assert.Throws<KeyNotFoundException>(() => registry.Get(key, out _, out _),
                "Get should throw KeyNotFoundException when the key does not exist.");
        }

        #endregion

        #region GetStoreA Tests

        [Test]
        public void GetStoreA_ExistingKey_ReturnsCorrectValue()
        {
            // Arrange
            var registry = new SyncedGpuDataRegistry<Vector2, PositionData, RotationData>(128, 16);
            var key = new Vector2(10f, 10f);
            var valA = new PositionData { Value = new Vector3(1f, 2f, 3f) };

            // We only set StoreA
            registry.SetStoreA(key, valA);

            // Act
            var result = registry.GetStoreA(key);

            // Assert
            Assert.AreEqual(valA.Value, result.Value, "GetStoreA should return the correct value from StoreA.");
        }

        [Test]
        public void GetStoreA_NonExistentKey_ThrowsKeyNotFoundException()
        {
            // Arrange
            var registry = new SyncedGpuDataRegistry<Vector2, PositionData, RotationData>(128, 16);
            var key = new Vector2(999f, 999f);

            // Assert
            Assert.Throws<KeyNotFoundException>(() => registry.GetStoreA(key),
                "GetStoreA should throw KeyNotFoundException when the key does not exist.");
        }

        #endregion

        #region SetStoreA Tests

        [Test]
        public void SetStoreA_UpdatesStoreA_ValueIsStoredCorrectly()
        {
            // Arrange
            var registry = new SyncedGpuDataRegistry<Vector2, PositionData, RotationData>(128, 16);
            var key = new Vector2(1f, 1f);
            var valA = new PositionData { Value = new Vector3(5f, 5f, 5f) };

            // Act
            registry.SetStoreA(key, valA);

            // Assert
            Assert.AreEqual(valA.Value, registry.GetStoreA(key).Value, "StoreA should hold the set value.");
        }

        [Test]
        public void SetStoreA_DoesNotModifyStoreB_RetainsDefaultValue()
        {
            // Arrange
            var registry = new SyncedGpuDataRegistry<Vector2, PositionData, RotationData>(128, 16);
            var key = new Vector2(1f, 1f);
            var valA = new PositionData { Value = new Vector3(5f, 5f, 5f) };

            // Act
            registry.SetStoreA(key, valA);

            // Assert
            // Verify that the key exists in the registry (via mapper) but StoreB is still at default
            Assert.IsTrue(registry.TryGetStoreB(key, out var valB), "Key should exist in registry after setting StoreA.");
            Assert.AreEqual(default(RotationData), valB, "StoreB should still be at default value.");
        }

        [Test]
        public void SetStoreA_AllocatesNewIndexIfKeyIsNew()
        {
            // Arrange
            var registry = new SyncedGpuDataRegistry<Vector2, PositionData, RotationData>(128, 16);
            var key = new Vector2(2f, 2f);
            var valA = new PositionData { Value = new Vector3(1f, 1f, 1f) };

            // Act
            registry.SetStoreA(key, valA);

            // Assert
            // If it successfully retrieves the value, the index was allocated correctly
            Assert.DoesNotThrow(() => registry.GetStoreA(key));
        }

        #endregion

        #region GetStoreB Tests

        [Test]
        public void GetStoreB_ExistingKey_ReturnsCorrectValue()
        {
            // Arrange
            var registry = new SyncedGpuDataRegistry<Vector2, PositionData, RotationData>(128, 16);
            var key = new Vector2(10f, 10f);
            var valB = new RotationData { Angle = 45f };

            // Set StoreB
            registry.SetStoreB(key, valB);

            // Act
            var result = registry.GetStoreB(key);

            // Assert
            Assert.AreEqual(valB.Angle, result.Angle, "GetStoreB should return the correct value from StoreB.");
        }

        [Test]
        public void GetStoreB_NonExistentKey_ThrowsKeyNotFoundException()
        {
            // Arrange
            var registry = new SyncedGpuDataRegistry<Vector2, PositionData, RotationData>(128, 16);
            var key = new Vector2(999f, 999f);

            // Assert
            Assert.Throws<KeyNotFoundException>(() => registry.GetStoreB(key),
                "GetStoreB should throw KeyNotFoundException when the key does not exist.");
        }

        #endregion

        #region SetStoreB Tests

        [Test]
        public void SetStoreB_UpdatesStoreB_ValueIsStoredCorrectly()
        {
            // Arrange
            var registry = new SyncedGpuDataRegistry<Vector2, PositionData, RotationData>(128, 16);
            var key = new Vector2(1f, 1f);
            var valB = new RotationData { Angle = 90.0f };

            // Act
            registry.SetStoreB(key, valB);

            // Assert
            Assert.AreEqual(valB.Angle, registry.GetStoreB(key).Angle, "StoreB should hold the set value.");
        }

        [Test]
        public void SetStoreB_DoesNotModifyStoreA_RetainsDefaultValue()
        {
            // Arrange
            var registry = new SyncedGpuDataRegistry<Vector2, PositionData, RotationData>(128, 16);
            var key = new Vector2(1f, 1f);
            var valB = new RotationData { Angle = 90.0f };

            // Act
            registry.SetStoreB(key, valB);

            // Assert
            // Verify that the key exists in the registry (via mapper) but StoreA is still at default
            Assert.IsTrue(registry.TryGetStoreA(key, out var valA), "Key should exist in registry after setting StoreB.");
            Assert.AreEqual(default(PositionData), valA, "StoreA should still be at default value.");
        }

        [Test]
        public void SetStoreB_AllocatesNewIndexIfKeyIsNew()
        {
            // Arrange
            var registry = new SyncedGpuDataRegistry<Vector2, PositionData, RotationData>(128, 16);
            var key = new Vector2(2f, 2f);
            var valB = new RotationData { Angle = 45.0f };

            // Act
            registry.SetStoreB(key, valB);

            // Assert
            // If it successfully retrieves the value, the index was allocated correctly
            Assert.DoesNotThrow(() => registry.GetStoreB(key), "Index allocation for a new key should not throw.");
        }

        #endregion

        #region TryGetStoreA Tests

        [Test]
        public void TryGetStoreA_ExistingKey_ReturnsTrueAndValue()
        {
            // Arrange
            var registry = new SyncedGpuDataRegistry<Vector2, PositionData, RotationData>(128, 16);
            var key = new Vector2(5f, 5f);
            var valA = new PositionData { Value = new Vector3(10f, 0f, 0f) };
            registry.SetStoreA(key, valA);

            // Act
            bool success = registry.TryGetStoreA(key, out var result);

            // Assert
            Assert.IsTrue(success, "TryGetStoreA should return true for an existing key.");
            Assert.AreEqual(valA.Value, result.Value, "The retrieved value should match the set value.");
        }

        [Test]
        public void TryGetStoreA_NonExistentKey_ReturnsFalseAndDefault()
        {
            // Arrange
            var registry = new SyncedGpuDataRegistry<Vector2, PositionData, RotationData>(128, 16);
            var key = new Vector2(999f, 999f);

            // Act
            bool success = registry.TryGetStoreA(key, out var result);

            // Assert
            Assert.IsFalse(success, "TryGetStoreA should return false for a non-existent key.");
            Assert.AreEqual(default(PositionData), result, "Result should be default for non-existent keys.");
        }

        #endregion

        #region TryGetStoreB Tests

        [Test]
        public void TryGetStoreB_ExistingKey_ReturnsTrueAndValue()
        {
            // Arrange
            var registry = new SyncedGpuDataRegistry<Vector2, PositionData, RotationData>(128, 16);
            var key = new Vector2(5f, 5f);
            var valB = new RotationData { Angle = 180f };
            registry.SetStoreB(key, valB);

            // Act
            bool success = registry.TryGetStoreB(key, out var result);

            // Assert
            Assert.IsTrue(success, "TryGetStoreB should return true for an existing key.");
            Assert.AreEqual(valB.Angle, result.Angle, "The retrieved value should match the set value.");
        }

        [Test]
        public void TryGetStoreB_NonExistentKey_ReturnsFalseAndDefault()
        {
            // Arrange
            var registry = new SyncedGpuDataRegistry<Vector2, PositionData, RotationData>(128, 16);
            var key = new Vector2(999f, 999f);

            // Act
            bool success = registry.TryGetStoreB(key, out var result);

            // Assert
            Assert.IsFalse(success, "TryGetStoreB should return false for a non-existent key.");
            Assert.AreEqual(default(RotationData), result, "Result should be default for non-existent keys.");
        }

        #endregion

        #region ForEachSyncedDirtySegment Tests

        [Test]
        public void ForEachSyncedDirtySegment_ProcessesAllDirtyElementsCorrectly()
        {
            // Arrange
            var registry = new SyncedGpuDataRegistry<Vector2, PositionData, RotationData>(128, 2);
            
            registry.SetStoreA(new Vector2(1, 1), new PositionData());
            registry.SetStoreA(new Vector2(2, 2), new PositionData());
            registry.SetStoreA(new Vector2(3, 3), new PositionData());

            var action = new SyncedDirtySegmentAction<PositionData, RotationData>();

            // Act
            registry.ForEachSyncedDirtySegment(ref action);

            // Assert
            Assert.AreEqual(2, action.CallCount, "The total call count of the action handler should be 2.");
            Assert.AreEqual(4, action.TotalLength, "The total count of dirty segments processed should match twice the batchsize.");
        }

        #endregion

        #region ForEachSyncedDirtyIndex Tests

        [Test]
        public void ForEachSyncedDirtyIndex_IteratesAllDirtyBits_AndExecutesAction()
        {
            // Arrange: Create registry with one batch (16 elements)
            var registry = new SyncedGpuDataRegistry<Vector2, PositionData, RotationData>(128, 2);

            // Set three distributed bits within the same batch
            registry.SetStoreA(new Vector2(1, 1), new PositionData()); // Bit 0
            registry.SetStoreA(new Vector2(2, 2), new PositionData()); // Bit 5
            registry.SetStoreA(new Vector2(3, 3), new PositionData()); // Bit 10

            // Prepare the execution handler
            var action = new SyncedDirtyIndexAction<PositionData, RotationData>
            {
                IndexList = new Dictionary<int, bool[]>()
            };

            // Act: Process dirty indices
            registry.ForEachSyncedDirtyIndex(ref action);

            // Assert: Verify processing count and data integrity
            Assert.AreEqual(2, action.CallCount, "The action should be executed exactly once for each found dirty bit.");

            Assert.IsTrue(action.IndexList.ContainsKey(0), "Index 0 should have been processed.");
            Assert.IsTrue(action.IndexList.ContainsKey(1), "Index 1 should have been processed.");
        }

        #endregion

        #region GetSyncedDirtySegments Tests

        [Test]
        public void GetSyncedDirtySegments_ProcessesAllDirtyElementsCorrectly()
        {
            // Arrange: Create registry with 128 capacity and batch size of 2
            var registry = new SyncedGpuDataRegistry<Vector2, PositionData, RotationData>(128, 2);

            // Set items that occupy the first two batches (indices 0, 1, 2)
            registry.SetStoreA(new Vector2(1, 1), new PositionData());
            registry.SetStoreA(new Vector2(2, 2), new PositionData());
            registry.SetStoreA(new Vector2(3, 3), new PositionData());

            // Act: Get iterator and consume
            var iterator = registry.GetSyncedDirtySegments();
            int callCount = 0;
            int totalLength = 0;

            while (iterator.MoveNext())
            {
                callCount++;
                totalLength += iterator.Current.SegmentA.Count;
            }

            // Assert: Verify processing count and data length
            // With batch size 2, indices 0,1 fall into batch 0 (count 2), index 2 falls into batch 1 (count 2)
            Assert.AreEqual(2, callCount, "The total call count of the iterator should be 2.");
            Assert.AreEqual(4, totalLength, "The total count of dirty segments processed should match the batch-aligned length.");
        }

        #endregion

        #region GetSyncedDirtyIndices Tests

        [Test]
        public void GetSyncedDirtyIndices_IteratesAllDirtyBits_AndReturnsCorrectIndices()
        {
            // Arrange: Create registry with 128 capacity
            var registry = new SyncedGpuDataRegistry<Vector2, PositionData, RotationData>(128, 2);

            // Set bits 0 and 1 as dirty
            registry.SetStoreA(new Vector2(1, 1), new PositionData());
            registry.SetStoreA(new Vector2(2, 2), new PositionData());
            registry.SetStoreA(new Vector2(3, 3), new PositionData());

            // Act: Get iterator and consume
            var iterator = registry.GetSyncedDirtyIndices<PositionData>();
            var foundIndices = new List<int>();

            while (iterator.MoveNext())
            {
                foundIndices.Add(iterator.Current.Index);
            }

            // Assert: Verify data integrity
            Assert.AreEqual(2, foundIndices.Count, "The iterator should yield exactly 2 indices.");
            Assert.Contains(0, foundIndices, "Index 0 should have been processed.");
            Assert.Contains(1, foundIndices, "Index 1 should have been processed.");
        }

        #endregion

        #region ForEach Tests

        [Test]
        public void ForEach_ProcessesExactlyAllSegments_BasedOnCapacityAndBatchSize()
        {
            // Arrange: Registry with 128 capacity and batch size of 2
            // With 128 capacity and batch size of 2, we expect 128 / 2 = 64 segments.
            var registry = new SyncedGpuDataRegistry<Vector2, PositionData, RotationData>(128, 2);

            var expectedData = TestUtility.CreateSampleItems<float>(128);
            for (int i = 0; i < 128; ++i)
            {
                var pos = new PositionData { Value = new Vector3(expectedData[i], 0, 0) };
                var key = new Vector2(i, 0);
                registry.SetStoreA(key, pos);
            }

            // Act
            var action = new SyncedSegmentAction<PositionData, RotationData>();
            registry.ForEach(ref action);

            // Assert: Verify that the registry correctly divides the 128 capacity 
            // into 64 segments (given a batch size of 2).
            Assert.AreEqual(64, action.SegmentCount, "Registry with 128 capacity and BatchSize 2 must process exactly 64 segments.");
        }

        #endregion
    }
}
