using NUnit.Framework;
using Rayforge.Core.Collections.Abstractions;
using Rayforge.Core.Common.Rendering;
using Rayforge.Core.Environment.Abstractions;
using Rayforge.Core.Environment.Spatial.Chunks;
using Rayforge.Core.Environment.Spatial.Surfaces;
using Rayforge.Core.Execution.Abstractions;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Rayforge.Core.Environment.Spatial.Rendering.Tests
{
    [TestFixture]
    public class TextureChunkCoordinatorTests
    {
        #region Test Env

        private GameObject _viewerObject;
        private GameObject _parentObject;
        private TextureChunkCoordinator _coordinator;

        public class MockSpatialCollection : ISpatialCollection<Vector3Int>
        {
            private readonly HashSet<Vector3Int> _activeCells = new();
            private readonly HashSet<Vector3Int> _dirtyCells = new();

            public int StateCount => _activeCells.Count;
            public int CellCount => _activeCells.Count;
            public int DirtyCellCount => _dirtyCells.Count;

            public void SetCell(Vector3Int key, bool active, bool isDirty = true)
            {
                if (active)
                {
                    _activeCells.Add(key);
                }
                else
                {
                    _activeCells.Remove(key);
                }

                if (isDirty)
                {
                    _dirtyCells.Add(key);
                }
            }

            public bool IsCellActive(Vector3Int key) => _activeCells.Contains(key);

            public int GetCellStateCount(Vector3Int key) => _activeCells.Contains(key) ? 1 : 0;

            public void ClearDirtyCells() => _dirtyCells.Clear();

            public void ForEachCell<TAction>(ref TAction action) where TAction : struct, IExecutionHandler<Vector3Int>
            {
                foreach (var cell in _activeCells)
                {
                    action.Execute(cell);
                }
            }

            public void ForEachDirtyCell<TAction>(ref TAction action) where TAction : struct, IExecutionHandler<Vector3Int>
            {
                foreach (var cell in _dirtyCells)
                {
                    action.Execute(cell);
                }
            }

            public void Clear()
            {
                _activeCells.Clear();
                _dirtyCells.Clear();
            }

            public IIterator<Vector3Int> GetCellIterator() => throw new NotImplementedException();
            public IIterator<Vector3Int> GetDirtyCellIterator() => throw new NotImplementedException();
        }

        private class FaultyMockSpatialCollection : ISpatialCollection<Vector3Int>
        {
            public int StateCount => 0;
            public int CellCount => 0;
            public int DirtyCellCount => 1;

            public void Clear() { }
            public void ClearDirtyCells() { }
            public int GetCellStateCount(Vector3Int key) => 0;
            public IIterator<Vector3Int> GetCellIterator() => throw new NotImplementedException();
            public IIterator<Vector3Int> GetDirtyCellIterator() => throw new NotImplementedException();
            public bool IsCellActive(Vector3Int key) => true;

            public void ForEachCell<TAction>(ref TAction action) where TAction : struct, IExecutionHandler<Vector3Int> { }

            public void ForEachDirtyCell<TAction>(ref TAction action) where TAction : struct, IExecutionHandler<Vector3Int>
            {
                // Simulate an internal failure during iteration/update
                throw new InvalidOperationException("Simulated collection failure");
            }
        }

        [SetUp]
        public void Setup()
        {
            _viewerObject = new GameObject("TestViewer");
            _parentObject = new GameObject("TestParent");
            _coordinator = new TextureChunkCoordinator();
        }

        [TearDown]
        public void TearDown()
        {
            if (_viewerObject != null)
                UnityEngine.Object.DestroyImmediate(_viewerObject);

            if (_parentObject != null)
                UnityEngine.Object.DestroyImmediate(_parentObject);
        }

        #endregion

        #region Initialization Tests

        [Test]
        public void Initialize_WhenViewerIsNull_ThrowsArgumentNullException()
        {
            // Arrange
            float[] lodDistances = new float[] { 10f, 20f };

            // Act & Assert
            Assert.Throws<ArgumentNullException>(() =>
                _coordinator.Initialize(
                    GridSize.Size10,
                    Vector3.zero,
                    lodDistances,
                    PowerOfTwoResolution.Res256,
                    64,
                    null, // Invalid viewer
                    true,
                    _parentObject.transform
                )
            );
        }

        [Test]
        public void Initialize_WhenBatchSizeIsInvalid_ThrowsArgumentOutOfRangeException()
        {
            // Arrange
            float[] lodDistances = new float[] { 10f, 20f };

            // Act & Assert
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                _coordinator.Initialize(
                    GridSize.Size10,
                    Vector3.zero,
                    lodDistances,
                    PowerOfTwoResolution.Res256,
                    0, // Invalid batch size
                    _viewerObject.transform,
                    true,
                    _parentObject.transform
                )
            );
        }

        [Test]
        public void Initialize_WhenLodDistancesIsEmpty_ThrowsArgumentException()
        {
            // Arrange
            float[] emptyDistances = Array.Empty<float>();

            // Act & Assert
            Assert.Throws<ArgumentException>(() =>
                _coordinator.Initialize(
                    GridSize.Size10,
                    Vector3.zero,
                    emptyDistances, // Empty span
                    PowerOfTwoResolution.Res256,
                    64,
                    _viewerObject.transform,
                    true,
                    _parentObject.transform
                )
            );
        }

        [Test]
        public void Initialize_WhenLodDistanceIsNonPositive_ThrowsArgumentOutOfRangeException()
        {
            // Arrange
            float[] invalidDistances = new float[] { 10f, 0f };

            // Act & Assert
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                _coordinator.Initialize(
                    GridSize.Size10,
                    Vector3.zero,
                    invalidDistances,
                    PowerOfTwoResolution.Res256,
                    64,
                    _viewerObject.transform,
                    true,
                    _parentObject.transform
                )
            );
        }

        [Test]
        public void Initialize_WhenLodDistancesAreNotIncreasing_ThrowsArgumentException()
        {
            // Arrange
            float[] nonIncreasingDistances = new float[] { 20f, 10f };

            // Act & Assert
            Assert.Throws<ArgumentException>(() =>
                _coordinator.Initialize(
                    GridSize.Size10,
                    Vector3.zero,
                    nonIncreasingDistances,
                    PowerOfTwoResolution.Res256,
                    64,
                    _viewerObject.transform,
                    true,
                    _parentObject.transform
                )
            );
        }

        [Test]
        public void Initialize_WithValidParameters_SetsIsInitializedToTrue()
        {
            // Arrange
            float[] lodDistances = new float[] { 10f, 20f };
            var baseRes = PowerOfTwoResolution.Res256;

            // Act
            _coordinator.Initialize(
                GridSize.Size10,
                Vector3.zero,
                lodDistances,
                baseRes,
                64,
                _viewerObject.transform,
                true,
                _parentObject.transform
            );

            // Assert
            Assert.IsTrue(_coordinator.IsInitialized, "Coordinator should be initialized successfully.");
        }

        #endregion

        #region Property Tests

        [Test]
        public void IsInitialized_BeforeInitialization_ReturnsFalse()
        {
            // Assert
            Assert.IsFalse(_coordinator.IsInitialized, "Coordinator should not be initialized by default.");
        }

        [Test]
        public void IsInitialized_AfterInitialization_ReturnsTrue()
        {
            // Arrange
            _coordinator.Initialize(GridSize.Size10, Vector3.zero, new float[] { 10f }, PowerOfTwoResolution.Res64, 16, _viewerObject.transform);

            // Assert
            Assert.IsTrue(_coordinator.IsInitialized, "Coordinator should be marked as initialized.");
        }

        [Test]
        public void RequiredSliceCount_BeforeInitialization_ReturnsZero()
        {
            // Assert
            Assert.AreEqual(0, _coordinator.RequiredSliceCount, "RequiredSliceCount should be 0 before initialization.");
        }

        [Test]
        public void BaseResolution_BeforeInitialization_ReturnsDefault()
        {
            // Assert
            Assert.AreEqual(default(PowerOfTwoResolution), _coordinator.BaseResolution, "BaseResolution should be default before initialization.");
        }

        [Test]
        public void BufferCapacity_BeforeInitialization_ReturnsZero()
        {
            // Assert
            Assert.AreEqual(0, _coordinator.BufferCapacity, "BufferCapacity should be 0 before initialization.");
        }

        [Test]
        public void BatchSize_BeforeInitialization_ReturnsZero()
        {
            // Assert
            Assert.AreEqual(0, _coordinator.BatchSize, "BatchSize should be 0 before initialization.");
        }

        [Test]
        public void CullingStride_BeforeInitialization_ReturnsZero()
        {
            // Assert
            Assert.AreEqual(0, _coordinator.CullingStride, "CullingStride should be 0 before initialization.");
        }

        [Test]
        public void RenderStride_BeforeInitialization_ReturnsZero()
        {
            // Assert
            Assert.AreEqual(0, _coordinator.RenderStride, "RenderStride should be 0 before initialization.");
        }

        [Test]
        public void HighestActiveIndex_BeforeInitialization_ReturnsMinusOne()
        {
            // Assert
            Assert.AreEqual(-1, _coordinator.HighestActiveIndex, "HighestActiveIndex should be -1 before initialization.");
        }

        [Test]
        public void LodGridProvider_BeforeInitialization_IsNotNull()
        {
            // Assert
            Assert.IsNotNull(_coordinator.LodGridProvider, "LodGridProvider reference should not be null before initialization.");
        }

        [Test]
        public void BaseResolution_AfterInitialization_ReturnsCorrectValue()
        {
            // Arrange
            var expectedResolution = PowerOfTwoResolution.Res128;
            _coordinator.Initialize(GridSize.Size10, Vector3.zero, new float[] { 10f }, expectedResolution, 16, _viewerObject.transform);

            // Assert
            Assert.AreEqual(expectedResolution, _coordinator.BaseResolution, "BaseResolution width should match initialization argument.");
        }

        [Test]
        public void BatchSize_AfterInitialization_ReturnsCorrectValue()
        {
            // Arrange
            int expectedBatchSize = 32;
            _coordinator.Initialize(GridSize.Size10, Vector3.zero, new float[] { 10f }, PowerOfTwoResolution.Res64, expectedBatchSize, _viewerObject.transform);

            // Assert
            Assert.AreEqual(expectedBatchSize, _coordinator.BatchSize, "BatchSize should match initialization argument.");
        }

        [Test]
        public void RequiredSliceCount_AfterInitialization_ReturnsValidValue()
        {
            // Arrange
            _coordinator.Initialize(GridSize.Size10, Vector3.zero, new float[] { 10f }, PowerOfTwoResolution.Res64, 16, _viewerObject.transform);

            // Assert
            Assert.GreaterOrEqual(_coordinator.RequiredSliceCount, 0, "RequiredSliceCount should be a valid non-negative value after initialization.");
        }

        [Test]
        public void BufferCapacity_AfterInitialization_ReturnsValidValue()
        {
            // Arrange
            _coordinator.Initialize(GridSize.Size10, Vector3.zero, new float[] { 10f }, PowerOfTwoResolution.Res64, 16, _viewerObject.transform);

            // Assert
            Assert.GreaterOrEqual(_coordinator.BufferCapacity, 0, "BufferCapacity should be a valid non-negative value after initialization.");
        }

        [Test]
        public void CullingStride_AfterInitialization_ReturnsValidValue()
        {
            // Arrange
            _coordinator.Initialize(GridSize.Size10, Vector3.zero, new float[] { 10f }, PowerOfTwoResolution.Res64, 16, _viewerObject.transform);

            // Assert
            Assert.GreaterOrEqual(_coordinator.CullingStride, 0, "CullingStride should be a valid non-negative value after initialization.");
        }

        [Test]
        public void RenderStride_AfterInitialization_ReturnsValidValue()
        {
            // Arrange
            _coordinator.Initialize(GridSize.Size10, Vector3.zero, new float[] { 10f }, PowerOfTwoResolution.Res64, 16, _viewerObject.transform);

            // Assert
            Assert.GreaterOrEqual(_coordinator.RenderStride, 0, "RenderStride should be a valid non-negative value after initialization.");
        }

        [Test]
        public void HighestActiveIndex_AfterInitialization_ReturnsInitialMinusOne()
        {
            // Arrange
            _coordinator.Initialize(GridSize.Size10, Vector3.zero, new float[] { 10f }, PowerOfTwoResolution.Res64, 16, _viewerObject.transform);

            // Assert
            Assert.AreEqual(-1, _coordinator.HighestActiveIndex, "HighestActiveIndex should be -1 initially when no chunks are active.");
        }

        [Test]
        public void LodGridProvider_AfterInitialization_IsNotNullAndValid()
        {
            // Arrange
            _coordinator.Initialize(GridSize.Size10, Vector3.zero, new float[] { 10f }, PowerOfTwoResolution.Res64, 16, _viewerObject.transform);

            // Assert
            Assert.IsNotNull(_coordinator.LodGridProvider, "LodGridProvider should not be null after initialization.");
        }

        [Test]
        public void Viewer_WhenNotInitialized_Get_ThrowsInvalidOperationException()
        {
            // Act & Assert
            Assert.Throws<InvalidOperationException>(() => { var v = _coordinator.Viewer; });
        }

        [Test]
        public void Viewer_WhenNotInitialized_Set_ThrowsInvalidOperationException()
        {
            // Act & Assert
            Assert.Throws<InvalidOperationException>(() => _coordinator.Viewer = _viewerObject.transform);
        }

        [Test]
        public void Viewer_WhenInitialized_GetAndSet_WorksCorrectly()
        {
            // Arrange
            float[] lodDistances = new float[] { 10f };
            _coordinator.Initialize(
                GridSize.Size10,
                Vector3.zero,
                lodDistances,
                PowerOfTwoResolution.Res64,
                16,
                _viewerObject.transform
            );

            var newViewer = new GameObject("NewViewer").transform;

            try
            {
                // Act
                _coordinator.Viewer = newViewer;

                // Assert
                Assert.AreEqual(newViewer, _coordinator.Viewer, "Viewer property should return the newly set transform.");
            }
            finally
            {
                if (newViewer != null)
                    UnityEngine.Object.DestroyImmediate(newViewer.gameObject);
            }
        }

        [Test]
        public void Anchor_WhenNotInitialized_Get_ThrowsInvalidOperationException()
        {
            // Act & Assert
            Assert.Throws<InvalidOperationException>(() => { var a = _coordinator.Anchor; });
        }

        [Test]
        public void Anchor_WhenNotInitialized_Set_ThrowsInvalidOperationException()
        {
            // Act & Assert
            Assert.Throws<InvalidOperationException>(() => _coordinator.Anchor = Vector3.one);
        }

        [Test]
        public void Anchor_WhenInitialized_GetAndSet_WorksCorrectly()
        {
            // Arrange
            float[] lodDistances = new float[] { 10f };
            _coordinator.Initialize(
                GridSize.Size10,
                Vector3.zero,
                lodDistances,
                PowerOfTwoResolution.Res64,
                16,
                _viewerObject.transform
            );

            Vector3 expectedAnchor = new Vector3(5f, 10f, 15f);

            // Act
            _coordinator.Anchor = expectedAnchor;

            // Assert
            Assert.AreEqual(expectedAnchor, _coordinator.Anchor, "Anchor property should return the newly set vector.");
        }

        #endregion

        #region Clear Tests

        [Test]
        public void Clear_WhenInitialized_DoesNotThrowAndMaintainsInitializationState()
        {
            // Arrange
            float[] lodDistances = new float[] { 10f };
            _coordinator.Initialize(
                GridSize.Size10,
                Vector3.zero,
                lodDistances,
                PowerOfTwoResolution.Res64,
                16,
                _viewerObject.transform
            );

            Assert.IsTrue(_coordinator.IsInitialized, "Precondition: Coordinator should be initialized.");

            // Act & Assert
            Assert.DoesNotThrow(() => _coordinator.Clear(), "Clear should execute without throwing exceptions.");
            Assert.IsTrue(_coordinator.IsInitialized, "Coordinator should remain initialized after Clear.");
        }

        [Test]
        public void Clear_WhenNotInitialized_DoesNotThrow()
        {
            // Act & Assert
            Assert.DoesNotThrow(() => _coordinator.Clear(), "Clear on an uninitialized coordinator should safely handle null references.");
        }

        #endregion

        #region Reset Tests

        [Test]
        public void Reset_WhenInitialized_ResetsToUninitializedState()
        {
            // Arrange
            float[] lodDistances = new float[] { 10f };
            _coordinator.Initialize(
                GridSize.Size10,
                Vector3.zero,
                lodDistances,
                PowerOfTwoResolution.Res64,
                16,
                _viewerObject.transform
            );

            Assert.IsTrue(_coordinator.IsInitialized, "Precondition: Coordinator should be initialized.");

            // Act & Assert
            Assert.DoesNotThrow(() => _coordinator.Reset(), "Reset should execute without throwing exceptions.");
            Assert.IsFalse(_coordinator.IsInitialized, "Coordinator should be uninitialized after Reset.");
            Assert.AreEqual(-1, _coordinator.HighestActiveIndex, "HighestActiveIndex should be reset to -1.");
        }

        [Test]
        public void Reset_WhenNotInitialized_DoesNotThrow()
        {
            // Act & Assert
            Assert.DoesNotThrow(() => _coordinator.Reset(), "Reset on an uninitialized coordinator should safely handle null references.");
        }

        #endregion

        #region NotifyOriginShift Tests

        [Test]
        public void NotifyOriginShift_WhenNotInitialized_ThrowsInvalidOperationException()
        {
            // Act & Assert
            Assert.Throws<InvalidOperationException>(() => _coordinator.NotifyOriginShift(Vector3.one));
        }

        [Test]
        public void NotifyOriginShift_WhenInitialized_DoesNotThrow()
        {
            // Arrange
            float[] lodDistances = new float[] { 10f };
            _coordinator.Initialize(
                GridSize.Size10,
                Vector3.zero,
                lodDistances,
                PowerOfTwoResolution.Res64,
                16,
                _viewerObject.transform
            );

            Vector3 delta = new Vector3(10f, 0f, 5f);

            // Act & Assert
            Assert.DoesNotThrow(() => _coordinator.NotifyOriginShift(delta));
        }

        #endregion

        #region UpdateLODs Tests

        [Test]
        public void UpdateLODs_WhenNotInitialized_ThrowsInvalidOperationException()
        {
            // Act & Assert
            Assert.Throws<InvalidOperationException>(() => _coordinator.UpdateLODs());
        }

        [Test]
        public void UpdateLODs_WhenInitialized_DoesNotThrowAndReturnsInteger()
        {
            // Arrange
            float[] lodDistances = new float[] { 10f };
            _coordinator.Initialize(
                GridSize.Size10,
                Vector3.zero,
                lodDistances,
                PowerOfTwoResolution.Res64,
                16,
                _viewerObject.transform
            );

            int changedCount = -1;

            // Act & Assert
            Assert.DoesNotThrow(() => changedCount = _coordinator.UpdateLODs());
            Assert.GreaterOrEqual(changedCount, 0, "UpdateLODs should return a non-negative count of changed chunks.");
        }

        #endregion

        #region ForceRequeueAll Tests

        [Test]
        public void ForceRequeueAll_WhenNotInitialized_ThrowsInvalidOperationException()
        {
            // Arrange
            // _coordinator is not initialized by default

            // Act & Assert
            var exception = Assert.Throws<InvalidOperationException>(() => _coordinator.ForceRequeueAll());
            Assert.That(exception.Message, Is.EqualTo("TextureChunkCoordinator is not initialized."));
        }

        [Test]
        public void ForceRequeueAll_WhenInitializedAndEmpty_DoesNotThrow()
        {
            // Arrange
            float[] lodDistances = new float[] { 10f };
            _coordinator.Initialize(
                GridSize.Size10,
                Vector3.zero,
                lodDistances,
                PowerOfTwoResolution.Res64,
                16,
                _viewerObject.transform
            );

            // Act & Assert
            Assert.DoesNotThrow(() => _coordinator.ForceRequeueAll(),
                "ForceRequeueAll should execute safely when no chunks are registered in the registry.");
        }

        [Test]
        public void ForceRequeueAll_WithRegisteredChunks_ExecutesSuccessfully()
        {
            // Arrange
            float[] lodDistances = new float[] { 10f };
            _coordinator.Initialize(
                GridSize.Size10,
                Vector3.zero,
                lodDistances,
                PowerOfTwoResolution.Res64,
                16,
                _viewerObject.transform
            );

            var spatialCollection = new MockSpatialCollection();
            spatialCollection.SetCell(new Vector3Int(0, 0, 0), active: true, isDirty: true);
            spatialCollection.SetCell(new Vector3Int(2, 0, 2), active: true, isDirty: true);

            // Populate the registry through topology and LOD update passes
            _coordinator.UpdateTopology(spatialCollection);
            _coordinator.UpdateLODs();

            // Act & Assert
            Assert.DoesNotThrow(() => _coordinator.ForceRequeueAll(),
                "ForceRequeueAll should successfully iterate through active chunks, evaluate visibility, and flush requests.");
        }

        #endregion

        #region UpdateTopology Tests

        [Test]
        public void UpdateTopology_WhenNotInitialized_ThrowsInvalidOperationException()
        {
            // Arrange
            var spatialCollection = new MockSpatialCollection();

            // Act & Assert
            var exception = Assert.Throws<InvalidOperationException>(() => _coordinator.UpdateTopology(spatialCollection));
            Assert.That(exception.Message, Is.EqualTo("TextureChunkCoordinator is not initialized."));
        }

        [Test]
        public void UpdateTopology_WithActiveDirtyCell_RequestsTileSuccessfully()
        {
            // Arrange
            float[] lodDistances = new float[] { 10f };
            _coordinator.Initialize(
                GridSize.Size10,
                Vector3.zero,
                lodDistances,
                PowerOfTwoResolution.Res64,
                16,
                _viewerObject.transform
            );

            var spatialCollection = new MockSpatialCollection();
            var testKey = new Vector3Int(2, 0, 2);

            // Mark cell as active and dirty
            spatialCollection.SetCell(testKey, active: true, isDirty: true);

            // Act & Assert
            Assert.DoesNotThrow(() => _coordinator.UpdateTopology(spatialCollection),
                "UpdateTopology should execute cleanly with active dirty cells.");
        }

        [Test]
        public void UpdateTopology_WithInactiveDirtyCell_HandlesRemovalSuccessfully()
        {
            // Arrange
            float[] lodDistances = new float[] { 10f };
            _coordinator.Initialize(
                GridSize.Size10,
                Vector3.zero,
                lodDistances,
                PowerOfTwoResolution.Res64,
                16,
                _viewerObject.transform
            );

            var spatialCollection = new MockSpatialCollection();
            var testKey = new Vector3Int(1, 1, 1);

            // First add it as active
            spatialCollection.SetCell(testKey, active: true, isDirty: true);
            _coordinator.UpdateTopology(spatialCollection);
            spatialCollection.ClearDirtyCells();

            // Now mark it as inactive (removed) and dirty
            spatialCollection.SetCell(testKey, active: false, isDirty: true);

            // Act & Assert
            Assert.DoesNotThrow(() => _coordinator.UpdateTopology(spatialCollection),
                "UpdateTopology should cleanly process deactivated dirty cells.");
        }

        [Test]
        public void UpdateTopology_WhenCollectionThrowsException_WrapsAndRethrows()
        {
            // Arrange
            float[] lodDistances = new float[] { 10f };
            _coordinator.Initialize(
                GridSize.Size10,
                Vector3.zero,
                lodDistances,
                PowerOfTwoResolution.Res64,
                16,
                _viewerObject.transform
            );

            var faultyCollection = new FaultyMockSpatialCollection();

            // Act & Assert
            var ex = Assert.Throws<Exception>(() => _coordinator.UpdateTopology(faultyCollection));
            Assert.That(ex.Message, Does.Contain("Topology update failed"));
            Assert.IsNotNull(ex.InnerException, "Should preserve the original inner exception.");
        }

        #endregion

        #region ForEachBakeCommand Tests

        [Test]
        public void ForEachBakeCommand_WhenNotInitialized_ThrowsInvalidOperationException()
        {
            // Arrange
            var handler = new TestBakeHandler();

            // Act & Assert
            var exception = Assert.Throws<InvalidOperationException>(() => _coordinator.ForEachBakeCommand(ref handler));
            Assert.That(exception.Message, Is.EqualTo("TextureChunkCoordinator is not initialized."));
        }

        [Test]
        public void ForEachBakeCommand_WhenInitializedAndEmpty_DoesNotThrow()
        {
            // Arrange
            float[] lodDistances = new float[] { 10f };
            _coordinator.Initialize(
                GridSize.Size10,
                Vector3.zero,
                lodDistances,
                PowerOfTwoResolution.Res64,
                16,
                _viewerObject.transform
            );

            var handler = new TestBakeHandler();

            // Act & Assert
            Assert.DoesNotThrow(() => _coordinator.ForEachBakeCommand(ref handler),
                "ForEachBakeCommand should execute safely when the bake queue is empty.");
            Assert.AreEqual(0, handler.ExecutionCount, "No items should be processed if the queue is empty.");
        }

        [Test]
        public void ForEachBakeCommand_WithPendingBakes_ExecutesHandlerSuccessfully()
        {
            // Arrange
            float[] lodDistances = new float[] { 10f };
            _coordinator.Initialize(
                GridSize.Size10,
                Vector3.zero,
                lodDistances,
                PowerOfTwoResolution.Res64,
                16,
                _viewerObject.transform
            );

            var spatialCollection = new MockSpatialCollection();
            spatialCollection.SetCell(new Vector3Int(0, 0, 0), active: true, isDirty: true);

            // Populate chunks to generate tile requests and trigger bake actions
            _coordinator.UpdateTopology(spatialCollection);
            _coordinator.UpdateLODs();

            var handler = new TestBakeHandler();

            // Act & Assert
            Assert.DoesNotThrow(() => _coordinator.ForEachBakeCommand(ref handler),
                "ForEachBakeCommand should successfully process pending bakes using the execution handler.");
            Assert.AreEqual(1, handler.ExecutionCount, "Handler should have executed for each pending bake command.");
        }

        private struct TestBakeHandler : IExecutionHandler<TileMetadata<Vector3Int>>
        {
            public int ExecutionCount;

            public void Execute(TileMetadata<Vector3Int> item)
            {
                ExecutionCount++;
            }
        }

        #endregion

        #region Full Pipeline Integration Tests

        [Test]
        public void FullPipeline_WithMultipleKeys_ProcessesTopologyLODAndBakesSuccessfully()
        {
            // Arrange: Set up LOD distances and initialize the coordinator
            float[] lodDistances = new float[] { 10f, 20f };
            _coordinator.Initialize(
                GridSize.Size10,
                Vector3.zero,
                lodDistances,
                PowerOfTwoResolution.Res64,
                32, // Batch size
                _viewerObject.transform
            );

            var spatialCollection = new MockSpatialCollection();

            // Define a cluster of multiple spatial keys to test parallel processing
            var targetKeys = new[]
            {
                new Vector3Int(0, 0, 0),
                new Vector3Int(1, 0, 0),
                new Vector3Int(0, 1, 0),
                new Vector3Int(1, 1, 1)
            };

            foreach (var key in targetKeys)
            {
                spatialCollection.SetCell(key, active: true, isDirty: true);
            }

            // Act & Assert - Step 1: Feed registry changes into mapper via Topology Update
            Assert.DoesNotThrow(() => _coordinator.UpdateTopology(spatialCollection),
                "UpdateTopology should process multiple dirty cells without throwing.");

            // Act & Assert - Step 2: Update LODs to evaluate visibility and LOD levels for the cluster
            int lodChanges = -1;
            Assert.DoesNotThrow(() => lodChanges = _coordinator.UpdateLODs(),
                "UpdateLODs should execute successfully for the registered cluster.");
            Assert.GreaterOrEqual(lodChanges, 0, "UpdateLODs should return a valid non-negative count.");

            // Act & Assert - Step 3: Force requeue to push visible chunks into the mapper's bake queue
            Assert.DoesNotThrow(() => _coordinator.ForceRequeueAll(),
                "ForceRequeueAll should successfully queue all registered chunks.");

            // Act & Assert - Step 4: Execute the Bake Command Pipeline using a zero-allocation struct handler
            var pipelineHandler = new PipelineVerificationHandler();
            Assert.DoesNotThrow(() => _coordinator.ForEachBakeCommand(ref pipelineHandler),
                "ForEachBakeCommand should successfully process the pipeline loop without throwing.");

            // Verify that items successfully flowed through the entire pipeline and triggered the handler
            Assert.Greater(pipelineHandler.ProcessedCount, 0,
                "The pipeline handler should have successfully processed bake requests triggered by the multi-key pipeline flow.");
        }

        private struct PipelineVerificationHandler : IExecutionHandler<TileMetadata<Vector3Int>>
        {
            public int ProcessedCount;

            public void Execute(TileMetadata<Vector3Int> item)
            {
                ProcessedCount++;
            }
        }

        #endregion
    }
}
