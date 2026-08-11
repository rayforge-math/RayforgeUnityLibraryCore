using NUnit.Framework;
using Rayforge.Core.Collections.Abstractions;
using Rayforge.Core.Common.Rendering;
using Rayforge.Core.Execution.Abstractions;
using Rayforge.Core.Rendering.Abstractions;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Rayforge.Core.Environment.Spatial.Rendering.Tests
{
    [TestFixture]
    public class LodAtlasMapperTests
    {
        #region Test Env

        public class MockAtlasMapper<TKey> : LodAtlasMapper<TKey, AabbSpatialData, AabbGpuDataRegistry<TKey, TextureMappingData>>
            where TKey : struct, IEquatable<TKey>
        {
            protected override AabbGpuDataRegistry<TKey, TextureMappingData> CreateRegistry(int totalCapacity, int batchSize)
            {
                return new AabbGpuDataRegistry<TKey, TextureMappingData>(totalCapacity, batchSize);
            }

            protected override AabbSpatialData CreateSpatialEntry(Vector3 worldPos, float extent)
            {
                float halfExtent = extent * 0.5f;
                var minBounds = worldPos - new Vector3(halfExtent, halfExtent, halfExtent);
                var maxBounds = worldPos + new Vector3(halfExtent, halfExtent, halfExtent);
                return new AabbSpatialData { MinBounds = minBounds, MaxBounds = maxBounds };
            }
        }

        private struct TestBakeHandler : IExecutionHandler<TileMetadata<int>>
        {
            public List<TileMetadata<int>> Collected;

            public void Execute(TileMetadata<int> metadata)
            {
                Collected.Add(metadata);
            }
        }

        private struct TestCullingHandler<TSpatial> : IExecutionHandler<BufferSegmentMeta<TSpatial>>
            where TSpatial : unmanaged, IGpuData<TSpatial>
        {
            public int Count;

            public void Execute(BufferSegmentMeta<TSpatial> segment)
            {
                Count++;
            }
        }

        private struct TestRenderHandler : IExecutionHandler<BufferSegmentMeta<TextureMappingData>>
        {
            public int Count;

            public void Execute(BufferSegmentMeta<TextureMappingData> segment)
            {
                Count++;
            }
        }

        private struct TestSyncedHandler<TSpatial> : IExecutionHandler<SyncedSegmentMeta<TSpatial, TextureMappingData>>
            where TSpatial : unmanaged, IGpuData<TSpatial>
        {
            public int Count;

            public void Execute(SyncedSegmentMeta<TSpatial, TextureMappingData> segment)
            {
                Count++;
            }
        }

        #endregion

        #region Initialize Tests

        [Test]
        public void Initialize_ValidParameters_SetsUpCorrectly()
        {
            // Arrange
            var mapper = new MockAtlasMapper<int>();
            int[] capacities = { 10, 20 };
            var baseResolution = PowerOfTwoResolution.Res64;

            // Act
            mapper.Initialize(capacities, baseResolution, batchSize: 1);

            // Assert
            Assert.IsTrue(mapper.IsInitialized);
            Assert.AreEqual(2, mapper.RequiredSliceCount > 0 ? 2 : 0); // Verifying initialization success
        }

        [Test]
        public void Initialize_NullMaxCapacities_ThrowsArgumentNullException()
        {
            // Arrange
            var mapper = new MockAtlasMapper<int>();
            var baseResolution = PowerOfTwoResolution.Res64;

            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => mapper.Initialize(null, baseResolution, batchSize: 4));
        }

        [Test]
        public void Initialize_EmptyMaxCapacities_ThrowsArgumentException()
        {
            // Arrange
            var mapper = new MockAtlasMapper<int>();
            int[] capacities = Array.Empty<int>();
            var baseResolution = PowerOfTwoResolution.Res64;

            // Act & Assert
            Assert.Throws<ArgumentException>(() => mapper.Initialize(capacities, baseResolution, batchSize: 4));
        }

        [Test]
        public void Initialize_InvalidBatchSize_ThrowsArgumentOutOfRangeException()
        {
            // Arrange
            var mapper = new MockAtlasMapper<int>();
            int[] capacities = { 10 };
            var baseResolution = PowerOfTwoResolution.Res64;

            // Act & Assert
            Assert.Throws<ArgumentOutOfRangeException>(() => mapper.Initialize(capacities, baseResolution, batchSize: 0));
        }

        [Test]
        public void Initialize_InsufficientDownscales_ThrowsInvalidOperationException()
        {
            // Arrange
            var mapper = new MockAtlasMapper<int>();
            int[] capacities = { 10, 20, 30 }; // 3 LOD levels needed
            var baseResolution = PowerOfTwoResolution.Res1; // Res1 cannot downscale 3 times

            // Act & Assert
            Assert.Throws<InvalidOperationException>(() => mapper.Initialize(capacities, baseResolution, batchSize: 4));
        }

        [Test]
        public void Initialize_CalledMultipleTimes_ReinitializesCleanlyWithoutError()
        {
            // Arrange
            var mapper = new MockAtlasMapper<int>();
            int[] firstCapacities = { 10, 20 }; // Sum = 30 (Divisible by batchSize: 2)
            int[] secondCapacities = { 4, 8, 16 }; // Sum = 28 (Divisible by batchSize: 4)
            var baseResolution = PowerOfTwoResolution.Res64;

            // Act & Assert - First initialization
            mapper.Initialize(firstCapacities, baseResolution, batchSize: 2);
            Assert.IsTrue(mapper.IsInitialized);

            // Act & Assert - Second initialization (should dispose old registry and set up new state)
            mapper.Initialize(secondCapacities, baseResolution, batchSize: 4);

            Assert.IsTrue(mapper.IsInitialized, "Mapper must remain initialized after re-initialization.");
            Assert.IsNotNull(mapper.Registry, "Registry must be re-created properly.");
            Assert.IsFalse(mapper.HasPendingRequests, "State should be cleared upon re-initialization.");
        }

        [Test]
        public void Initialize_WhenTotalCapacityIsNotBatchAligned_PadsToNextMultiple()
        {
            // Arrange
            int[] maxCapacities = new int[] { 4, 6 };
            int batchSize = 4;
            var baseRes = PowerOfTwoResolution.Res64;

            var mapper = new SphereAtlasMapper<Vector3Int>();

            // Act
            mapper.Initialize(maxCapacities, baseRes, batchSize);

            // Assert
            Assert.AreEqual(12, mapper.Registry.Capacity, "Buffer capacity should be padded to the next multiple of the batch size.");
        }

        [Test]
        public void Initialize_WhenTotalCapacityIsAlreadyBatchAligned_KeepsExactCapacity()
        {
            // Arrange
            int[] maxCapacities = new int[] { 4, 4 };
            int batchSize = 4;
            var baseRes = PowerOfTwoResolution.Res64;

            var mapper = new SphereAtlasMapper<Vector3Int>();

            // Act
            mapper.Initialize(maxCapacities, baseRes, batchSize);

            // Assert
            Assert.AreEqual(8, mapper.Registry.Capacity, "Buffer capacity should remain unchanged if it is already a multiple of the batch size.");
        }

        #endregion

        #region Property Tests

        [Test]
        public void Properties_Uninitialized_ReturnDefaultValues()
        {
            // Arrange
            var mapper = new MockAtlasMapper<int>();

            // Act & Assert
            Assert.IsFalse(mapper.IsInitialized, "Uninitialized mapper must not be marked as initialized.");
            Assert.AreEqual(0, mapper.RequiredSliceCount, "Uninitialized mapper should require zero slices.");
            Assert.AreEqual(PowerOfTwoResolution.None, mapper.BaseResolution, "Uninitialized mapper should have None as base resolution.");
            Assert.IsFalse(mapper.HasPendingRequests, "Uninitialized mapper should have no pending requests.");
            Assert.IsFalse(mapper.HasBakeCommands, "Uninitialized mapper should have no bake commands.");
            Assert.IsNull(mapper.Registry, "Uninitialized mapper registry must be null.");
        }

        [Test]
        public void Properties_Initialized_ReturnExpectedValues()
        {
            // Arrange
            var mapper = new MockAtlasMapper<int>();
            int[] capacities = { 10, 20 };
            var baseRes = PowerOfTwoResolution.Res64;

            // Act
            mapper.Initialize(capacities, baseRes, batchSize: 1);

            // Assert
            Assert.IsTrue(mapper.IsInitialized, "Initialized mapper must be marked as initialized.");
            Assert.Greater(mapper.RequiredSliceCount, 0, "RequiredSliceCount must be greater than zero after initialization.");
            Assert.AreEqual(baseRes, mapper.BaseResolution, "BaseResolution must match the configured value.");
            Assert.IsNotNull(mapper.Registry, "Registry must be instantiated after initialization.");
        }

        [Test]
        public void HasPendingRequests_AfterRequestTile_ReturnsTrue()
        {
            // Arrange
            var mapper = new MockAtlasMapper<int>();
            mapper.Initialize(new[] { 10 }, PowerOfTwoResolution.Res64, batchSize: 1);

            Assert.IsFalse(mapper.HasPendingRequests, "Initially there should be no pending requests.");

            // Act
            mapper.RequestTile(key: 1, lodIndex: 0, worldPos: Vector3.zero, extent: 1f);

            // Assert
            Assert.IsTrue(mapper.HasPendingRequests, "HasPendingRequests must be true after queueing a tile update.");
        }

        [Test]
        public void HasBakeCommands_AfterFlushWithValidUpdates_ReturnsTrue()
        {
            // Arrange
            var mapper = new MockAtlasMapper<int>();
            mapper.Initialize(new[] { 10 }, PowerOfTwoResolution.Res64, batchSize: 1);

            mapper.RequestTile(key: 1, lodIndex: 0, worldPos: Vector3.zero, extent: 1f);

            Assert.IsFalse(mapper.HasBakeCommands, "No bake commands before flushing.");

            // Act
            mapper.FlushTileRequests();

            // Assert
            Assert.IsTrue(mapper.HasBakeCommands, "HasBakeCommands must be true after flushing newly mapped tiles.");
        }

        [Test]
        public void LodProperties_WhenUninitialized_ReturnsZero()
        {
            // Arrange
            var mapper = new MockAtlasMapper<int>();

            // Act & Assert
            Assert.AreEqual(0, mapper.LodCount);
            Assert.AreEqual(0, mapper.GetLodCapacity(0));
        }

        [Test]
        public void LodProperties_WhenInitialized_ReturnsCorrectValues()
        {
            // Arrange
            var mapper = new MockAtlasMapper<int>();
            mapper.Initialize(new[] { 8, 16, 32 }, PowerOfTwoResolution.Res64, batchSize: 2);

            // Act & Assert
            Assert.AreEqual(3, mapper.LodCount);
            Assert.AreEqual(8, mapper.GetLodCapacity(0));
            Assert.AreEqual(16, mapper.GetLodCapacity(1));
            Assert.AreEqual(32, mapper.GetLodCapacity(2));
            Assert.AreEqual(0, mapper.GetLodCapacity(-1), "Out-of-bounds negative index should return 0 capacity.");
            Assert.AreEqual(0, mapper.GetLodCapacity(5), "Out-of-bounds positive index should return 0 capacity.");
        }

        [Test]
        public void ActiveTileCount_And_IsTileActive_TrackStateCorrectly()
        {
            // Arrange
            var mapper = new MockAtlasMapper<int>();
            mapper.Initialize(new[] { 10 }, PowerOfTwoResolution.Res64, batchSize: 1);
            int key = 42;

            // Initial state
            Assert.IsFalse(mapper.IsTileActive(key));
            Assert.AreEqual(0, mapper.ActiveTileCount);

            // Request tile (queued, not yet active)
            mapper.RequestTile(key, lodIndex: 0, Vector3.zero, 1f);
            Assert.IsFalse(mapper.IsTileActive(key), "Tile should not be active before flushing the request.");
            Assert.AreEqual(0, mapper.ActiveTileCount);

            // Flush requests
            mapper.FlushTileRequests();
            Assert.IsTrue(mapper.IsTileActive(key), "Tile should be active after flushing.");
            Assert.AreEqual(1, mapper.ActiveTileCount);

            // Release tile (queued for removal, still active until flush)
            mapper.ReleaseTile(key);
            Assert.IsTrue(mapper.IsTileActive(key), "Tile should remain active until removal is flushed.");
            Assert.AreEqual(1, mapper.ActiveTileCount);

            // Flush removals
            mapper.FlushTileRequests();
            Assert.IsFalse(mapper.IsTileActive(key), "Tile should no longer be active after removal flush.");
            Assert.AreEqual(0, mapper.ActiveTileCount);
        }

        [Test]
        public void TryGetActiveTile_WhenTileExists_ReturnsTrueAndCorrectMapping()
        {
            // Arrange
            var mapper = new MockAtlasMapper<int>();
            mapper.Initialize(new[] { 10 }, PowerOfTwoResolution.Res64, batchSize: 1);
            int key = 42;

            mapper.RequestTile(key, lodIndex: 0, Vector3.zero, 1f);
            mapper.FlushTileRequests();

            // Act
            bool found = mapper.TryGetActiveTile(key, out int lodIndex, out var mapping);

            // Assert
            Assert.IsTrue(found, "TryGetActiveTile should return true for an active tile.");
            Assert.AreEqual(0, lodIndex, "LOD index must match the requested level.");
            Assert.IsTrue(mapping.IsValid, "Returned mapping data must be valid.");
        }

        [Test]
        public void TryGetActiveTile_WhenTileDoesNotExist_ReturnsFalse()
        {
            // Arrange
            var mapper = new MockAtlasMapper<int>();
            mapper.Initialize(new[] { 10 }, PowerOfTwoResolution.Res64, batchSize: 1);

            // Act
            bool found = mapper.TryGetActiveTile(999, out int lodIndex, out var mapping);

            // Assert
            Assert.IsFalse(found, "TryGetActiveTile should return false for an unknown tile key.");
            Assert.AreEqual(-1, lodIndex);
        }

        #endregion

        #region UpdateBatchSize Tests

        [Test]
        public void UpdateBatchSize_WhenUninitialized_ReturnsFalse()
        {
            // Arrange
            var mapper = new MockAtlasMapper<int>();

            // Act
            bool result = mapper.UpdateBatchSize(4);

            // Assert
            Assert.IsFalse(result, "Updating batch size on an uninitialized mapper must return false.");
        }

        [Test]
        public void UpdateBatchSize_WhenSameBatchSize_ReturnsFalse()
        {
            // Arrange
            var mapper = new MockAtlasMapper<int>();
            mapper.Initialize(new[] { 10 }, PowerOfTwoResolution.Res64, batchSize: 2);

            // Act
            bool result = mapper.UpdateBatchSize(2);

            // Assert
            Assert.IsFalse(result, "Updating to the already active batch size should return false.");
        }

        [Test]
        public void UpdateBatchSize_WhenDifferentBatchSize_ReturnsTrue()
        {
            // Arrange
            var mapper = new MockAtlasMapper<int>();
            mapper.Initialize(new[] { 10 }, PowerOfTwoResolution.Res64, batchSize: 2);

            // Act
            bool result = mapper.UpdateBatchSize(1);

            // Assert
            Assert.IsTrue(result, "Updating to a different valid batch size must return true indicating migration.");
        }

        #endregion

        #region Clear Tests

        [Test]
        public void Clear_WhenUninitialized_DoesNotThrow()
        {
            // Arrange
            var mapper = new MockAtlasMapper<int>();

            // Act & Assert
            Assert.DoesNotThrow(() => mapper.Clear());
        }

        [Test]
        public void Clear_WhenInitializedWithState_ResetsStateAndKeepsInitialization()
        {
            // Arrange
            var mapper = new MockAtlasMapper<int>();
            mapper.Initialize(new[] { 10, 20 }, PowerOfTwoResolution.Res64, batchSize: 2);

            // Populate state (request and flush to populate active mappings and bake queue)
            mapper.RequestTile(key: 1, lodIndex: 0, worldPos: Vector3.zero, extent: 1f);
            mapper.FlushTileRequests();

            Assert.IsTrue(mapper.IsInitialized, "Mapper must be initialized.");
            Assert.IsTrue(mapper.HasBakeCommands, "Bake commands should exist before clearing.");

            // Act
            mapper.Clear();

            // Assert
            Assert.IsTrue(mapper.IsInitialized, "Clearing must keep the internal structures and registry allocated.");
            Assert.IsFalse(mapper.HasPendingRequests, "Pending requests queue must be cleared.");
            Assert.IsFalse(mapper.HasBakeCommands, "Bake lookup dictionary must be cleared.");
        }

        #endregion

        #region Reset Tests

        [Test]
        public void Reset_WhenInitialized_ReturnsToUninitializedState()
        {
            // Arrange
            var mapper = new MockAtlasMapper<int>();
            int[] capacities = { 10, 20 };
            mapper.Initialize(capacities, PowerOfTwoResolution.Res64, batchSize: 2);

            Assert.IsTrue(mapper.IsInitialized, "Precondition: Mapper should be initialized.");
            Assert.Greater(mapper.LodCount, 0, "Precondition: LodCount should be greater than zero.");

            // Act
            mapper.Reset();

            // Assert
            Assert.IsFalse(mapper.IsInitialized, "Mapper should not be initialized after Reset.");
            Assert.AreEqual(0, mapper.LodCount, "LodCount should be 0 after Reset.");
            Assert.AreEqual(0, mapper.RequiredSliceCount, "RequiredSliceCount should be 0 after Reset.");
            Assert.IsNull(mapper.Registry, "Registry reference should be null after Reset.");
        }

        [Test]
        public void Reset_WhenNotInitialized_DoesNotThrow()
        {
            // Arrange
            var mapper = new MockAtlasMapper<int>();

            // Assert
            Assert.IsFalse(mapper.IsInitialized, "Precondition: Mapper should not be initialized.");

            // Act & Assert
            Assert.DoesNotThrow(() => mapper.Reset(), "Reset on an uninitialized mapper should safely handle null references.");
        }

        [Test]
        public void Reset_ClearsActiveMappingsAndQueues()
        {
            // Arrange
            var mapper = new MockAtlasMapper<int>();
            mapper.Initialize(new[] { 10 }, PowerOfTwoResolution.Res64, batchSize: 1);

            int testKey = 42;
            mapper.RequestTile(testKey, lodIndex: 0, Vector3.zero, 1f);
            mapper.FlushTileRequests();

            Assert.IsTrue(mapper.IsTileActive(testKey), "Precondition: Test tile should be active.");

            // Act
            mapper.Reset();

            // Assert
            Assert.IsFalse(mapper.IsTileActive(testKey), "Active mappings should be cleared after Reset.");
            Assert.AreEqual(0, mapper.ActiveTileCount, "ActiveTileCount should be 0 after Reset.");
        }

        #endregion

        #region RequestTile Tests

        [Test]
        public void RequestTile_WhenUninitialized_ThrowsInvalidOperationException()
        {
            // Arrange
            var mapper = new MockAtlasMapper<int>();

            // Act & Assert
            Assert.Throws<InvalidOperationException>(() => mapper.RequestTile(1, 0, Vector3.zero, 1f));
        }

        [TestCase(-1)]
        [TestCase(2)] // Out of bounds for 2 LOD levels (indices 0 and 1)
        public void RequestTile_WithInvalidLodIndex_ThrowsArgumentOutOfRangeException(int invalidLodIndex)
        {
            // Arrange
            var mapper = new MockAtlasMapper<int>();
            mapper.Initialize(new[] { 10, 20 }, PowerOfTwoResolution.Res64, batchSize: 1);

            // Act & Assert
            Assert.Throws<ArgumentOutOfRangeException>(() => mapper.RequestTile(1, invalidLodIndex, Vector3.zero, 1f));
        }

        [Test]
        public void RequestTile_ValidParameters_SetsPendingRequests()
        {
            // Arrange
            var mapper = new MockAtlasMapper<int>();
            mapper.Initialize(new[] { 10, 20 }, PowerOfTwoResolution.Res64, batchSize: 1);

            // Act
            mapper.RequestTile(key: 42, lodIndex: 0, Vector3.zero, extent: 5f);

            // Assert
            Assert.IsTrue(mapper.HasPendingRequests, "Valid tile request must set HasPendingRequests to true.");
        }

        #endregion

        #region ReleaseTile Tests

        [Test]
        public void ReleaseTile_WhenUninitialized_ThrowsInvalidOperationException()
        {
            // Arrange
            var mapper = new MockAtlasMapper<int>();

            // Act & Assert
            Assert.Throws<InvalidOperationException>(() => mapper.ReleaseTile(1));
        }

        [Test]
        public void ReleaseTile_WhenActiveTileExists_QueuesRemoval()
        {
            // Arrange
            var mapper = new MockAtlasMapper<int>();
            mapper.Initialize(new[] { 10 }, PowerOfTwoResolution.Res64, batchSize: 1);

            mapper.RequestTile(key: 42, lodIndex: 0, Vector3.zero, 1f);
            mapper.FlushTileRequests();

            Assert.IsFalse(mapper.HasPendingRequests, "Precondition: No pending requests after flush.");

            // Act
            mapper.ReleaseTile(key: 42);

            // Assert
            Assert.IsTrue(mapper.HasPendingRequests, "Releasing a tile must queue a removal request.");
        }

        #endregion

        #region FlushTileRequests Tests

        [Test]
        public void FlushTileRequests_WhenUninitialized_ThrowsInvalidOperationException()
        {
            // Arrange
            var mapper = new MockAtlasMapper<int>();

            // Act & Assert
            Assert.Throws<InvalidOperationException>(() => mapper.FlushTileRequests());
        }

        [Test]
        public void FlushTileRequests_WithPendingRequests_ProcessesThemAndClearsQueue()
        {
            // Arrange
            var mapper = new MockAtlasMapper<int>();
            mapper.Initialize(new[] { 10 }, PowerOfTwoResolution.Res64, batchSize: 1);

            mapper.RequestTile(key: 1, lodIndex: 0, worldPos: Vector3.zero, extent: 1f);
            Assert.IsTrue(mapper.HasPendingRequests);

            // Act
            mapper.FlushTileRequests();

            // Assert
            Assert.IsFalse(mapper.HasPendingRequests, "Queue should be cleared after flushing.");
            Assert.IsTrue(mapper.HasBakeCommands, "Bake commands should be generated after flushing updates.");
        }

        [Test]
        public void FlushTileRequests_WhenCapacityExceeded_ThrowsException()
        {
            // Arrange
            var mapper = new MockAtlasMapper<int>();
            // Capacity for LOD 0 is strictly 1
            mapper.Initialize(new[] { 1 }, PowerOfTwoResolution.Res64, batchSize: 1);

            // Request two tiles for the same LOD level, exceeding capacity (max 1)
            mapper.RequestTile(key: 1, lodIndex: 0, Vector3.zero, 1f);
            mapper.RequestTile(key: 2, lodIndex: 0, Vector3.zero, 1f);

            // Act & Assert
            Assert.Throws<OverflowException>(() => mapper.FlushTileRequests());
        }

        #endregion

        #region GetPendingBakes Tests

        [Test]
        public void GetPendingBakes_WhenUninitialized_ThrowsInvalidOperationException()
        {
            // Arrange
            var mapper = new MockAtlasMapper<int>();

            // Act & Assert
            Assert.Throws<InvalidOperationException>(() => mapper.GetPendingBakes(),
                "GetPendingBakes should throw InvalidOperationException when the mapper is not initialized.");
        }

        [Test]
        public void GetPendingBakes_WhenNoRequestsFlushed_IsEmpty()
        {
            // Arrange
            var mapper = new MockAtlasMapper<int>();
            mapper.Initialize(new[] { 10 }, PowerOfTwoResolution.Res64, batchSize: 1);

            // Act
            var iterator = mapper.GetPendingBakes();

            // Assert
            Assert.IsNotNull(iterator, "Pending bakes iterator must not be null.");
            Assert.IsFalse(iterator.MoveNext(), "Iterator should not yield any elements if no bakes are pending.");
        }

        [Test]
        public void GetPendingBakes_AfterFlush_YieldsCorrectMetadata()
        {
            // Arrange
            var mapper = new MockAtlasMapper<int>();
            mapper.Initialize(new[] { 10 }, PowerOfTwoResolution.Res64, batchSize: 1);

            int testKey = 42;
            mapper.RequestTile(testKey, lodIndex: 0, Vector3.one, extent: 2f);
            mapper.FlushTileRequests();

            // Act
            var iterator = mapper.GetPendingBakes();

            // Assert
            Assert.IsTrue(iterator.MoveNext(), "Iterator should yield at least one pending bake item.");
            var tileMetadata = iterator.Current;

            Assert.AreEqual(testKey, tileMetadata.Key, "The metadata key must match the requested tile key.");
            Assert.IsFalse(iterator.MoveNext(), "There should only be one pending bake in the iterator.");
        }

        [Test]
        public void GetPendingBakes_AfterClearingBakeQueue_IsEmpty()
        {
            // Arrange
            var mapper = new MockAtlasMapper<int>();
            mapper.Initialize(new[] { 10 }, PowerOfTwoResolution.Res64, batchSize: 1);

            mapper.RequestTile(key: 42, lodIndex: 0, Vector3.zero, 1f);
            mapper.FlushTileRequests();

            // Act
            mapper.ClearBakeQueue();
            var iterator = mapper.GetPendingBakes();

            // Assert
            Assert.IsFalse(iterator.MoveNext(), "Iterator should be empty after clearing the bake queue.");
        }

        #endregion

        #region GetCullingDirtyIterator Tests

        [Test]
        public void GetCullingDirtyIterator_WhenUninitialized_ThrowsInvalidOperationException()
        {
            // Arrange
            var mapper = new MockAtlasMapper<int>();

            // Act & Assert
            Assert.Throws<InvalidOperationException>(() => mapper.GetCullingDirtyIterator(),
                "GetCullingDirtyIterator should throw InvalidOperationException when the mapper is not initialized.");
        }

        [Test]
        public void GetCullingDirtyIterator_AfterFlush_ReturnsNonEmptyIteratorForUnmerged()
        {
            // Arrange
            var mapper = new MockAtlasMapper<int>();
            mapper.Initialize(new[] { 10 }, PowerOfTwoResolution.Res64, batchSize: 1);

            mapper.RequestTile(key: 1, lodIndex: 0, Vector3.zero, 1f);
            mapper.FlushTileRequests();

            // Act
            var iterator = mapper.GetCullingDirtyIterator(merge: false);

            // Assert
            Assert.IsNotNull(iterator, "Culling dirty iterator must not be null.");
            Assert.IsTrue(iterator.MoveNext(), "Iterator should yield dirty segments after adding a tile.");
        }

        [Test]
        public void GetCullingDirtyIterator_AfterFlush_ReturnsNonEmptyIteratorForMerged()
        {
            // Arrange
            var mapper = new MockAtlasMapper<int>();
            mapper.Initialize(new[] { 10 }, PowerOfTwoResolution.Res64, batchSize: 1);

            mapper.RequestTile(key: 1, lodIndex: 0, Vector3.zero, 1f);
            mapper.FlushTileRequests();

            // Act
            var iterator = mapper.GetCullingDirtyIterator(merge: true);

            // Assert
            Assert.IsNotNull(iterator, "Culling dirty iterator must not be null.");
            Assert.IsTrue(iterator.MoveNext(), "Iterator should yield dirty segments after adding a tile with merge enabled.");
        }

        #endregion

        #region GetRenderDirtyIterator Tests

        [Test]
        public void GetRenderDirtyIterator_WhenUninitialized_ThrowsInvalidOperationException()
        {
            // Arrange
            var mapper = new MockAtlasMapper<int>();

            // Act & Assert
            Assert.Throws<InvalidOperationException>(() => mapper.GetRenderDirtyIterator(),
                "GetRenderDirtyIterator should throw InvalidOperationException when the mapper is not initialized.");
        }

        [Test]
        public void GetRenderDirtyIterator_AfterFlush_ReturnsNonEmptyIteratorForUnmerged()
        {
            // Arrange
            var mapper = new MockAtlasMapper<int>();
            mapper.Initialize(new[] { 10 }, PowerOfTwoResolution.Res64, batchSize: 1);

            mapper.RequestTile(key: 1, lodIndex: 0, Vector3.zero, 1f);
            mapper.FlushTileRequests();

            // Act
            var iterator = mapper.GetRenderDirtyIterator(merge: false);

            // Assert
            Assert.IsNotNull(iterator, "Render dirty iterator must not be null.");
            Assert.IsTrue(iterator.MoveNext(), "Iterator should yield dirty render segments after adding a tile.");
        }

        [Test]
        public void GetRenderDirtyIterator_AfterFlush_ReturnsNonEmptyIteratorForMerged()
        {
            // Arrange
            var mapper = new MockAtlasMapper<int>();
            mapper.Initialize(new[] { 10 }, PowerOfTwoResolution.Res64, batchSize: 1);

            mapper.RequestTile(key: 1, lodIndex: 0, Vector3.zero, 1f);
            mapper.FlushTileRequests();

            // Act
            var iterator = mapper.GetRenderDirtyIterator(merge: true);

            // Assert
            Assert.IsNotNull(iterator, "Render dirty iterator must not be null.");
            Assert.IsTrue(iterator.MoveNext(), "Iterator should yield dirty render segments after adding a tile with merge enabled.");
        }

        #endregion

        #region GetSyncedDirtyIterator Tests

        [Test]
        public void GetSyncedDirtyIterator_WhenUninitialized_ThrowsInvalidOperationException()
        {
            // Arrange
            var mapper = new MockAtlasMapper<int>();

            // Act & Assert
            Assert.Throws<InvalidOperationException>(() => mapper.GetSyncedDirtyIterator(),
                "GetSyncedDirtyIterator should throw InvalidOperationException when the mapper is not initialized.");
        }

        [Test]
        public void GetSyncedDirtyIterator_AfterFlush_ReturnsNonEmptyIterator()
        {
            // Arrange
            var mapper = new MockAtlasMapper<int>();
            mapper.Initialize(new[] { 10 }, PowerOfTwoResolution.Res64, batchSize: 1);

            mapper.RequestTile(key: 1, lodIndex: 0, Vector3.zero, 1f);
            mapper.FlushTileRequests();

            // Act
            var iterator = mapper.GetSyncedDirtyIterator(batchesPerWindow: 1);

            // Assert
            Assert.IsNotNull(iterator, "Synced dirty iterator must not be null.");
            Assert.IsTrue(iterator.MoveNext(), "Iterator should yield synchronized dirty segments after adding a tile.");
        }

        [Test]
        public void GetSyncedDirtyIterator_WithCustomBatchesPerWindow_ReturnsValidIterator()
        {
            // Arrange
            var mapper = new MockAtlasMapper<int>();
            mapper.Initialize(new[] { 10 }, PowerOfTwoResolution.Res64, batchSize: 1);

            mapper.RequestTile(key: 1, lodIndex: 0, Vector3.zero, 1f);
            mapper.FlushTileRequests();

            // Act
            var iterator = mapper.GetSyncedDirtyIterator(batchesPerWindow: 2);

            // Assert
            Assert.IsNotNull(iterator, "Synced dirty iterator with custom window size must not be null.");
        }

        #endregion

        #region ForEachPendingBake Tests

        [Test]
        public void ForEachPendingBake_WhenBakesArePending_ExecutesHandlerForEachBake()
        {
            // Arrange
            var mapper = new MockAtlasMapper<int>();
            mapper.Initialize(new[] { 10 }, PowerOfTwoResolution.Res64, batchSize: 1);

            int key = 42;
            mapper.RequestTile(key, lodIndex: 0, Vector3.zero, 1f);
            mapper.FlushTileRequests();

            var handler = new TestBakeHandler { Collected = new List<TileMetadata<int>>() };

            // Act
            mapper.ForEachPendingBake(ref handler);

            // Assert
            Assert.AreEqual(1, handler.Collected.Count);
            Assert.AreEqual(key, handler.Collected[0].Key);
        }

        [Test]
        public void ForEachPendingBake_WhenUninitialized_ThrowsInvalidOperationException()
        {
            // Arrange
            var mapper = new MockAtlasMapper<int>();
            var handler = new TestBakeHandler { Collected = new List<TileMetadata<int>>() };

            // Act & Assert
            Assert.Throws<InvalidOperationException>(() => mapper.ForEachPendingBake(ref handler),
                "ForEachPendingBake should throw InvalidOperationException when the mapper is not initialized.");
        }

        #endregion

        #region ForEachCullingDirty Tests

        [Test]
        public void ForEachCullingDirty_WhenDirtySegmentsExist_ExecutesHandlerForEachSegment()
        {
            // Arrange
            var mapper = new MockAtlasMapper<int>();
            mapper.Initialize(new[] { 10 }, PowerOfTwoResolution.Res64, batchSize: 1);

            mapper.RequestTile(42, lodIndex: 0, Vector3.zero, 1f);
            mapper.FlushTileRequests();

            var handler = new TestCullingHandler<AabbSpatialData>();

            // Act
            mapper.ForEachCullingDirty(ref handler, merge: false);

            // Assert
            Assert.Greater(handler.Count, 0, "Culling dirty handler should be executed at least once for active tiles.");
        }

        [Test]
        public void ForEachCullingDirty_WhenUninitialized_ThrowsInvalidOperationException()
        {
            // Arrange
            var mapper = new MockAtlasMapper<int>();
            var handler = new TestCullingHandler<AabbSpatialData>();

            // Act & Assert
            Assert.Throws<InvalidOperationException>(() => mapper.ForEachCullingDirty(ref handler),
                "ForEachCullingDirty should throw InvalidOperationException when the mapper is not initialized.");
        }

        #endregion

        #region ForEachRenderDirty Tests

        [Test]
        public void ForEachRenderDirty_WhenDirtySegmentsExist_ExecutesHandlerForEachSegment()
        {
            // Arrange
            var mapper = new MockAtlasMapper<int>();
            mapper.Initialize(new[] { 10 }, PowerOfTwoResolution.Res64, batchSize: 1);

            mapper.RequestTile(42, lodIndex: 0, Vector3.zero, 1f);
            mapper.FlushTileRequests();

            var handler = new TestRenderHandler();

            // Act
            mapper.ForEachRenderDirty(ref handler, merge: false);

            // Assert
            Assert.Greater(handler.Count, 0, "Render dirty handler should be executed at least once for active tiles.");
        }

        [Test]
        public void ForEachRenderDirty_WhenUninitialized_ThrowsInvalidOperationException()
        {
            // Arrange
            var mapper = new MockAtlasMapper<int>();
            var handler = new TestRenderHandler();

            // Act & Assert
            Assert.Throws<InvalidOperationException>(() => mapper.ForEachRenderDirty(ref handler),
                "ForEachRenderDirty should throw InvalidOperationException when the mapper is not initialized.");
        }

        #endregion

        #region ForEachSyncedDirty Tests

        [Test]
        public void ForEachSyncedDirty_WhenSyncedSegmentsExist_ExecutesHandlerForEachSegment()
        {
            // Arrange
            var mapper = new MockAtlasMapper<int>();
            mapper.Initialize(new[] { 10 }, PowerOfTwoResolution.Res64, batchSize: 1);

            mapper.RequestTile(42, lodIndex: 0, Vector3.zero, 1f);
            mapper.FlushTileRequests();

            var handler = new TestSyncedHandler<AabbSpatialData>();

            // Act
            mapper.ForEachSyncedDirty(ref handler, batchesPerWindow: 1);

            // Assert
            Assert.Greater(handler.Count, 0, "Synced dirty handler should be executed at least once for active tiles.");
        }

        [Test]
        public void ForEachSyncedDirty_WhenUninitialized_ThrowsInvalidOperationException()
        {
            // Arrange
            var mapper = new MockAtlasMapper<int>();
            var handler = new TestSyncedHandler<AabbSpatialData>();

            // Act & Assert
            Assert.Throws<InvalidOperationException>(() => mapper.ForEachSyncedDirty(ref handler),
                "ForEachSyncedDirty should throw InvalidOperationException when the mapper is not initialized.");
        }

        #endregion

        #region TryGetTextureMapping Tests

        [Test]
        public void TryGetTextureMapping_WhenTileIsActive_ReturnsTrueAndMapping()
        {
            // Arrange
            var mapper = new MockAtlasMapper<int>();
            mapper.Initialize(new[] { 10 }, PowerOfTwoResolution.Res64, batchSize: 1);

            int key = 42;
            mapper.RequestTile(key, lodIndex: 0, Vector3.zero, 1f);
            mapper.FlushTileRequests();

            // Act
            bool found = mapper.TryGetTextureMapping(key, out var mapping);

            // Assert
            Assert.IsTrue(found, "Should successfully retrieve texture mapping for an active tile.");
        }

        [Test]
        public void TryGetTextureMapping_WhenTileIsInactive_ReturnsFalse()
        {
            // Arrange
            var mapper = new MockAtlasMapper<int>();
            mapper.Initialize(new[] { 10 }, PowerOfTwoResolution.Res64, batchSize: 1);

            // Act
            bool found = mapper.TryGetTextureMapping(999, out var mapping);

            // Assert
            Assert.IsFalse(found, "Should return false for an inactive or non-existent tile.");
        }

        [Test]
        public void TryGetTextureMapping_WhenUninitialized_ThrowsInvalidOperationException()
        {
            // Arrange
            var mapper = new MockAtlasMapper<int>();

            // Act & Assert
            Assert.Throws<InvalidOperationException>(() => mapper.TryGetTextureMapping(42, out var mapping),
                "TryGetTextureMapping should throw InvalidOperationException when the mapper is not initialized.");
        }

        #endregion

        #region ClearBakeQueue Tests

        [Test]
        public void ClearBakeQueue_WhenBakeQueueHasItems_ClearsQueueSuccessfully()
        {
            // Arrange
            var mapper = new MockAtlasMapper<int>();
            mapper.Initialize(new[] { 10 }, PowerOfTwoResolution.Res64, batchSize: 1);

            mapper.RequestTile(key: 42, lodIndex: 0, Vector3.zero, 1f);
            mapper.FlushTileRequests();

            // Precondition: Bake queue should have items
            Assert.IsTrue(mapper.HasBakeCommands, "Bake queue should contain items after flushing requests.");

            // Act
            mapper.ClearBakeQueue();

            // Assert
            Assert.IsFalse(mapper.HasBakeCommands, "Bake queue should be empty after calling ClearBakeQueue.");

            var iterator = mapper.GetPendingBakes();
            Assert.IsFalse(iterator.MoveNext(), "Pending bakes iterator should not yield any elements after clearing.");
        }

        [Test]
        public void ClearBakeQueue_WhenAlreadyEmpty_DoesNotThrow()
        {
            // Arrange
            var mapper = new MockAtlasMapper<int>();
            mapper.Initialize(new[] { 10 }, PowerOfTwoResolution.Res64, batchSize: 1);

            // Act & Assert
            Assert.DoesNotThrow(() => mapper.ClearBakeQueue(), "Clearing an already empty bake queue should be safe and not throw.");
        }

        #endregion

        #region Full System Test

        [Test]
        public void LodAtlasMapper_FullProgramFlow_HandlesLifecycle()
        {
            // Arrange: Initialize the mapper with multi-LOD capacities that are multiples of batchSize (2)
            var mapper = new MockAtlasMapper<int>();
            mapper.Initialize(new[] { 4, 4 }, PowerOfTwoResolution.Res64, batchSize: 2);

            Assert.IsTrue(mapper.IsInitialized, "Mapper should be initialized.");
            Assert.AreEqual(0, mapper.ActiveTileCount, "Initial active tile count must be zero.");

            // Act 1: Request multiple tiles across different LOD levels
            int key1 = 101;
            int key2 = 102;
            mapper.RequestTile(key1, lodIndex: 0, new Vector3(0, 0, 0), 10f);
            mapper.RequestTile(key2, lodIndex: 1, new Vector3(10, 0, 10), 20f);

            Assert.IsTrue(mapper.HasPendingRequests, "Requests should be pending before flush.");

            // Act 2: Flush tile requests to apply allocations and generate bake commands
            mapper.FlushTileRequests();

            Assert.IsFalse(mapper.HasPendingRequests, "Pending requests should be cleared after flush.");
            Assert.AreEqual(2, mapper.ActiveTileCount, "Two tiles should be active.");
            Assert.IsTrue(mapper.HasBakeCommands, "Bake commands should be available after new allocations.");

            // Act 3: Process pending bakes using the allocation handler
            var bakeHandler = new TestBakeHandler { Collected = new List<TileMetadata<int>>() };
            mapper.ForEachPendingBake(ref bakeHandler);

            Assert.AreEqual(2, bakeHandler.Collected.Count, "All active tiles should require a bake.");

            // Act 4: Verify dirty culling and render updates
            var cullingHandler = new TestCullingHandler<AabbSpatialData>();
            mapper.ForEachCullingDirty(ref cullingHandler, merge: false);
            Assert.Greater(cullingHandler.Count, 0, "Culling dirty segments should be processed.");

            var renderHandler = new TestRenderHandler();
            mapper.ForEachRenderDirty(ref renderHandler, merge: false);
            Assert.Greater(renderHandler.Count, 0, "Render dirty segments should be processed.");

            // Act 5: Release one tile and flush again
            mapper.ReleaseTile(key1);
            mapper.FlushTileRequests();

            Assert.AreEqual(1, mapper.ActiveTileCount, "Active tile count should drop after release.");
            Assert.IsFalse(mapper.IsTileActive(key1), "Released tile should no longer be active.");
            Assert.IsTrue(mapper.IsTileActive(key2), "Remaining tile should still be active.");

            // Act 6: Clear the mapper state completely
            mapper.Clear();

            Assert.AreEqual(0, mapper.ActiveTileCount, "Active tile count should be zero after clear.");
            Assert.IsFalse(mapper.HasBakeCommands, "Bake commands should be cleared.");
        }

        #endregion
    }
}