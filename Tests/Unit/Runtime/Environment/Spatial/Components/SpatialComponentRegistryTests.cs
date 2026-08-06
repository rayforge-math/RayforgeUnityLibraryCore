using NUnit.Framework;
using Rayforge.Core.Environment.Spatial.Chunks;
using Rayforge.Core.Execution.Abstractions;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Rayforge.Core.Environment.Spatial.Components.Tests
{
    [TestFixture]
    public class SpatialComponentRegistryTests
    {
        #region Test Env

        private GameObject _testGameObject;
        private Transform _testComponent;
        private SpatialComponentRegistry<Vector3Int, Transform> _registry;
        private ChunkRegistry<TestChunk> _gridQuery;

        private class TestChunk : Chunk<TestChunk>
        {
            protected override void OnDispose()
            { }
        }

        private struct ComponentExecutionHandler : IExecutionHandler<Transform>
        {
            public int ExecutionCount;
            public List<Transform> ExecutedComponents;

            public void Execute(Transform component)
            {
                ExecutionCount++;
                ExecutedComponents ??= new List<Transform>();
                ExecutedComponents.Add(component);
            }
        }

        [SetUp]
        public void SetUp()
        {
            _testGameObject = new GameObject("TestObject");
            _testComponent = _testGameObject.transform;
            _registry = new SpatialComponentRegistry<Vector3Int, Transform>();
            _gridQuery = new ChunkRegistry<TestChunk>();
        }

        [TearDown]
        public void TearDown()
        {
            if (_testGameObject != null)
            {
                UnityEngine.Object.DestroyImmediate(_testGameObject);
            }
        }

        #endregion

        #region Initialize Tests

        [Test]
        public void Initialize_ValidGridProvider_SetsGridProvider()
        {
            // Act
            _registry.Initialize(_gridQuery);

            // Assert
            Assert.IsTrue(_registry.IsInitialized, "Registry should be marked as initialized when a valid grid provider is supplied.");
        }

        [Test]
        public void Initialize_NullGridProvider_ThrowsArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => _registry.Initialize(null));
        }

        #endregion

        #region Properties Tests

        [Test]
        public void StateCount_ReturnsAccurateNumberOfRegisteredComponents()
        {
            // Arrange
            _registry.Initialize(_gridQuery);
            _gridQuery.Initialize(GridSize.Size10, Vector3.zero);
            Assert.AreEqual(0, _registry.StateCount);

            var state1 = new ComponentState<Transform>
            {
                component = _testComponent,
                anchorBounds = new Bounds(Vector3.zero, Vector3.one)
            };

            var state2 = new ComponentState<Transform>
            {
                component = _testComponent,
                anchorBounds = new Bounds(Vector3.one, Vector3.one)
            };

            // Act
            _registry.TryRegister(1, state1);
            int countAfterOne = _registry.StateCount;

            _registry.TryRegister(2, state2);
            int countAfterTwo = _registry.StateCount;

            // Assert
            Assert.AreEqual(1, countAfterOne);
            Assert.AreEqual(2, countAfterTwo);
        }

        [Test]
        public void CellCount_ReflectsActiveBucketsCount()
        {
            // Arrange
            _registry.Initialize(_gridQuery);
            _gridQuery.Initialize(GridSize.Size10, Vector3.zero);
            Assert.AreEqual(0, _registry.CellCount);

            var state = new ComponentState<Transform>
            {
                component = _testComponent,
                anchorBounds = new Bounds(Vector3.zero, Vector3.one)
            };

            // Act
            _registry.TryRegister(1, state);

            // Assert
            Assert.IsTrue(_registry.CellCount > 0, "CellCount should be greater than zero after registering a component that populates buckets.");
        }

        [Test]
        public void DirtyCellCount_ReflectsModifiedBucketsAndClearsCorrectly()
        {
            // Arrange
            _registry.Initialize(_gridQuery);
            _gridQuery.Initialize(GridSize.Size10, Vector3.zero);
            Assert.AreEqual(0, _registry.DirtyCellCount);

            var state = new ComponentState<Transform>
            {
                component = _testComponent,
                anchorBounds = new Bounds(Vector3.zero, Vector3.one)
            };

            // Act: Registering marks cells as dirty
            _registry.TryRegister(1, state);
            int dirtyCount = _registry.DirtyCellCount;

            // Clear dirty cells
            _registry.ClearDirtyCells();

            // Assert
            Assert.IsTrue(dirtyCount > 0);
            Assert.AreEqual(0, _registry.DirtyCellCount);
        }

        [Test]
        public void GetCellStateCount_ExistingAndNonExistingCells_ReturnsCorrectCount()
        {
            // Arrange
            _registry.Initialize(_gridQuery);
            _gridQuery.Initialize(GridSize.Size10, Vector3.zero);

            // Act & Assert for non-existing cell
            Vector3Int nonExistentKey = new Vector3Int(999, 999, 999);
            Assert.AreEqual(0, _registry.GetCellStateCount(nonExistentKey));

            var state = new ComponentState<Transform>
            {
                component = _testComponent,
                anchorBounds = new Bounds(Vector3.zero, Vector3.one)
            };

            // Register component
            _registry.TryRegister(1, state);

            // Find an active cell key
            Vector3Int activeKey = default;
            var iterator = _registry.GetCellIterator();
            if (iterator.MoveNext())
            {
                activeKey = iterator.Current;
            }

            // Assert for existing cell
            Assert.AreEqual(1, _registry.GetCellStateCount(activeKey));
        }

        [Test]
        public void IsInitialized_ReflectsInitializationStatus()
        {
            // Assert before initialization
            Assert.IsFalse(_registry.IsInitialized);

            // Act
            _registry.Initialize(_gridQuery);

            // Assert after initialization
            Assert.IsTrue(_registry.IsInitialized);
        }

        #endregion

        #region FullRemap Tests

        [Test]
        public void FullRemap_UninitializedRegistry_ThrowsInvalidOperationException()
        {
            // Arrange
            var uninitializedRegistry = new SpatialComponentRegistry<Vector3Int, Transform>();

            // Act & Assert
            Assert.Throws<InvalidOperationException>(() => uninitializedRegistry.FullRemap());
        }

        [Test]
        public void FullRemap_InitializedRegistry_ClearsAndRebuildsBucketsAndDirtyBuckets()
        {
            // Arrange
            _registry.Initialize(_gridQuery);
            _gridQuery.Initialize(GridSize.Size10, Vector3.zero);

            var state = new ComponentState<Transform>
            {
                component = _testComponent,
                anchorBounds = new Bounds(Vector3.zero, Vector3.one)
            };

            _registry.TryRegister(1, state);
            _registry.ClearDirtyCells();

            // Act
            _registry.FullRemap();

            // Assert
            Assert.IsTrue(_registry.CellCount > 0, "Buckets should be repopulated after FullRemap.");
            Assert.IsTrue(_registry.DirtyCellCount > 0, "Buckets updated during FullRemap should be marked as dirty.");
        }

        #endregion

        #region Clear Tests

        [Test]
        public void Clear_PopulatedRegistry_ResetsAllCountsAndCollections()
        {
            // Arrange
            _registry.Initialize(_gridQuery);
            _gridQuery.Initialize(GridSize.Size10, Vector3.zero);

            var state = new ComponentState<Transform>
            {
                component = _testComponent,
                anchorBounds = new Bounds(Vector3.zero, Vector3.one)
            };

            _registry.TryRegister(1, state);

            // Verify pre-conditions
            Assert.IsTrue(_registry.StateCount > 0, "Registry should contain states before clearing.");
            Assert.IsTrue(_registry.CellCount > 0, "Registry should contain cells before clearing.");

            // Act
            _registry.Clear();

            // Assert
            Assert.AreEqual(0, _registry.StateCount, "StateCount should be zero after Clear.");
            Assert.AreEqual(0, _registry.CellCount, "CellCount should be zero after Clear.");
            Assert.AreEqual(0, _registry.DirtyCellCount, "DirtyCellCount should be zero after Clear.");
        }

        #endregion

        #region Reset Tests

        [Test]
        public void Reset_InitializedAndPopulatedRegistry_ClearsDataAndRemovesGridProvider()
        {
            // Arrange
            _registry.Initialize(_gridQuery);
            _gridQuery.Initialize(GridSize.Size10, Vector3.zero);

            var state = new ComponentState<Transform>
            {
                component = _testComponent,
                anchorBounds = new Bounds(Vector3.zero, Vector3.one)
            };

            _registry.TryRegister(1, state);

            // Verify pre-conditions
            Assert.IsTrue(_registry.IsInitialized, "Registry should be initialized before reset.");
            Assert.IsTrue(_registry.StateCount > 0, "Registry should contain states before reset.");

            // Act
            _registry.Reset();

            // Assert
            Assert.IsFalse(_registry.IsInitialized, "IsInitialized should be false after Reset.");
            Assert.AreEqual(0, _registry.StateCount, "StateCount should be zero after Reset.");
            Assert.AreEqual(0, _registry.CellCount, "CellCount should be zero after Reset.");
            Assert.AreEqual(0, _registry.DirtyCellCount, "DirtyCellCount should be zero after Reset.");
        }

        #endregion

        #region ClearDirtyCells Tests

        [Test]
        public void ClearDirtyCells_WithDirtyBuckets_ResetsDirtyCellCountToZero()
        {
            // Arrange
            _registry.Initialize(_gridQuery);
            _gridQuery.Initialize(GridSize.Size10, Vector3.zero);

            var state = new ComponentState<Transform>
            {
                component = _testComponent,
                anchorBounds = new Bounds(Vector3.zero, Vector3.one)
            };

            _registry.TryRegister(1, state);

            // Verify pre-condition
            Assert.IsTrue(_registry.DirtyCellCount > 0, "DirtyCellCount should be greater than zero after registration.");

            // Act
            _registry.ClearDirtyCells();

            // Assert
            Assert.AreEqual(0, _registry.DirtyCellCount, "DirtyCellCount should be zero after ClearDirtyCells.");
        }

        #endregion

        #region TryRegister Tests

        [Test]
        public void TryRegister_UninitializedRegistry_ThrowsInvalidOperationException()
        {
            // Arrange
            var state = new ComponentState<Transform>
            {
                component = _testComponent,
                anchorBounds = new Bounds(Vector3.zero, Vector3.one)
            };

            // Act & Assert
            Assert.Throws<InvalidOperationException>(() => _registry.TryRegister(1, state));
        }

        [Test]
        public void TryRegister_NewComponent_ReturnsTrueAndAddsState()
        {
            // Arrange
            _registry.Initialize(_gridQuery);
            _gridQuery.Initialize(GridSize.Size10, Vector3.zero);

            var state = new ComponentState<Transform>
            {
                component = _testComponent,
                anchorBounds = new Bounds(Vector3.zero, Vector3.one)
            };

            // Act
            bool result = _registry.TryRegister(1, state);

            // Assert
            Assert.IsTrue(result);
            Assert.AreEqual(1, _registry.StateCount);
            Assert.IsTrue(_registry.Contains(1));
            Assert.IsTrue(_registry.TryGetState(1, out var retrievedState));
            Assert.AreEqual(state, retrievedState);
        }

        [Test]
        public void TryRegister_ExistingComponentWithIdenticalState_ReturnsFalse()
        {
            // Arrange
            _registry.Initialize(_gridQuery);
            _gridQuery.Initialize(GridSize.Size10, Vector3.zero);

            var state = new ComponentState<Transform>
            {
                component = _testComponent,
                anchorBounds = new Bounds(Vector3.zero, Vector3.one)
            };

            _registry.TryRegister(1, state);
            _registry.ClearDirtyCells();

            // Act: Try registering the exact same state again
            bool result = _registry.TryRegister(1, state);

            // Assert
            Assert.IsFalse(result);
            Assert.AreEqual(1, _registry.StateCount);
        }

        [Test]
        public void TryRegister_ExistingComponentWithDifferentState_ReturnsTrueAndUpdatesStateAndBuckets()
        {
            // Arrange
            _registry.Initialize(_gridQuery);
            _gridQuery.Initialize(GridSize.Size10, Vector3.zero);

            var oldState = new ComponentState<Transform>
            {
                component = _testComponent,
                anchorBounds = new Bounds(Vector3.zero, Vector3.one)
            };

            var newState = new ComponentState<Transform>
            {
                component = _testComponent,
                anchorBounds = new Bounds(Vector3.one * 10f, Vector3.one) // Different bounds
            };

            _registry.TryRegister(1, oldState);
            _registry.ClearDirtyCells();

            // Act
            bool result = _registry.TryRegister(1, newState);

            // Assert
            Assert.IsTrue(result);
            Assert.AreEqual(1, _registry.StateCount);
            Assert.IsTrue(_registry.TryGetState(1, out var retrievedState));
            Assert.AreEqual(newState, retrievedState);
            Assert.IsTrue(_registry.DirtyCellCount > 0, "Buckets should be marked as dirty when updating an existing component state.");
        }

        #endregion

        #region Unregister Tests

        [Test]
        public void Unregister_ExistingComponent_ReturnsTrueAndRemovesStateAndBuckets()
        {
            // Arrange
            _registry.Initialize(_gridQuery);
            _gridQuery.Initialize(GridSize.Size10, Vector3.zero);

            var state = new ComponentState<Transform>
            {
                component = _testComponent,
                anchorBounds = new Bounds(Vector3.zero, Vector3.one)
            };

            _registry.TryRegister(1, state);

            // Verify pre-conditions
            Assert.AreEqual(1, _registry.StateCount);
            Assert.IsTrue(_registry.Contains(1));

            // Act
            bool result = _registry.Unregister(1);

            // Assert
            Assert.IsTrue(result);
            Assert.AreEqual(0, _registry.StateCount);
            Assert.IsFalse(_registry.Contains(1));
            Assert.IsFalse(_registry.TryGetState(1, out _));
        }

        [Test]
        public void Unregister_NonExistentComponent_ReturnsFalse()
        {
            // Arrange
            _registry.Initialize(_gridQuery);
            _gridQuery.Initialize(GridSize.Size10, Vector3.zero);

            // Act
            bool result = _registry.Unregister(999);

            // Assert
            Assert.IsFalse(result);
            Assert.AreEqual(0, _registry.StateCount);
        }

        #endregion

        #region Contains Tests

        [Test]
        public void Contains_ExistingId_ReturnsTrue()
        {
            // Arrange
            _registry.Initialize(_gridQuery);
            _gridQuery.Initialize(GridSize.Size10, Vector3.zero);

            var state = new ComponentState<Transform>
            {
                component = _testComponent,
                anchorBounds = new Bounds(Vector3.zero, Vector3.one)
            };

            _registry.TryRegister(42, state);

            // Act & Assert
            Assert.IsTrue(_registry.Contains(42));
        }

        [Test]
        public void Contains_NonExistentId_ReturnsFalse()
        {
            // Arrange
            _registry.Initialize(_gridQuery);
            _gridQuery.Initialize(GridSize.Size10, Vector3.zero);

            // Act & Assert
            Assert.IsFalse(_registry.Contains(999));
        }

        [Test]
        public void Contains_AfterUnregister_ReturnsFalse()
        {
            // Arrange
            _registry.Initialize(_gridQuery);
            _gridQuery.Initialize(GridSize.Size10, Vector3.zero);

            var state = new ComponentState<Transform>
            {
                component = _testComponent,
                anchorBounds = new Bounds(Vector3.zero, Vector3.one)
            };

            _registry.TryRegister(42, state);
            _registry.Unregister(42);

            // Act & Assert
            Assert.IsFalse(_registry.Contains(42));
        }

        #endregion

        #region TryGetState Tests

        [Test]
        public void TryGetState_ExistingId_ReturnsTrueAndOutputsCorrectState()
        {
            // Arrange
            _registry.Initialize(_gridQuery);
            _gridQuery.Initialize(GridSize.Size10, Vector3.zero);

            var expectedState = new ComponentState<Transform>
            {
                component = _testComponent,
                anchorBounds = new Bounds(Vector3.zero, Vector3.one)
            };

            // Register the state with ID 1
            _registry.TryRegister(1, expectedState);

            // Act
            bool result = _registry.TryGetState(1, out var retrievedState);

            // Assert
            Assert.IsTrue(result, "TryGetState should return true for an existing ID.");
            Assert.AreEqual(expectedState, retrievedState, "The retrieved state should match the registered state.");
        }

        [Test]
        public void TryGetState_NonExistentId_ReturnsFalseAndOutputsDefaultState()
        {
            // Arrange
            _registry.Initialize(_gridQuery);
            _gridQuery.Initialize(GridSize.Size10, Vector3.zero);

            // Act
            bool result = _registry.TryGetState(999, out var retrievedState);

            // Assert
            Assert.IsFalse(result, "TryGetState should return false for a non-existent ID.");
            Assert.AreEqual(default(ComponentState<Transform>), retrievedState, "The retrieved state should be the default value when ID is not found.");
        }

        #endregion

        #region ForEachCell Tests

        private struct CellExecutionHandler : IExecutionHandler<Vector3Int>
        {
            public int ExecutionCount;
            public List<Vector3Int> VisitedKeys;

            public void Execute(Vector3Int key)
            {
                ExecutionCount++;
                VisitedKeys ??= new List<Vector3Int>();
                VisitedKeys.Add(key);
            }
        }

        [Test]
        public void ForEachCell_EmptyRegistry_DoesNotExecuteAction()
        {
            // Arrange
            _registry.Initialize(_gridQuery);
            _gridQuery.Initialize(GridSize.Size10, Vector3.zero);

            var handler = new CellExecutionHandler();

            // Act
            _registry.ForEachCell(ref handler);

            // Assert
            Assert.AreEqual(0, handler.ExecutionCount, "Action should not be executed when there are no active cells.");
        }

        [Test]
        public void ForEachCell_PopulatedRegistry_ExecutesActionOnAllActiveCells()
        {
            // Arrange
            _registry.Initialize(_gridQuery);
            _gridQuery.Initialize(GridSize.Size10, Vector3.zero);

            var state = new ComponentState<Transform>
            {
                component = _testComponent,
                anchorBounds = new Bounds(Vector3.zero, Vector3.one)
            };

            _registry.TryRegister(1, state);

            int expectedCellCount = _registry.CellCount;
            Assert.IsTrue(expectedCellCount > 0, "Pre-condition: Registry must have active cells.");

            var handler = new CellExecutionHandler();

            // Act
            _registry.ForEachCell(ref handler);

            // Assert
            Assert.AreEqual(expectedCellCount, handler.ExecutionCount, "Action should be executed once for each active cell.");
            Assert.IsNotNull(handler.VisitedKeys);
            Assert.AreEqual(expectedCellCount, handler.VisitedKeys.Count);
        }

        #endregion

        #region GetCellIterator Tests

        [Test]
        public void GetCellIterator_EmptyRegistry_ReturnsEmptyIterator()
        {
            // Arrange
            _registry.Initialize(_gridQuery);
            _gridQuery.Initialize(GridSize.Size10, Vector3.zero);

            // Act
            var iterator = _registry.GetCellIterator();

            // Assert
            Assert.IsNotNull(iterator);
            Assert.IsFalse(iterator.MoveNext(), "Iterator should not have any elements when the registry is empty.");
        }

        [Test]
        public void GetCellIterator_PopulatedRegistry_IteratesAllActiveCells()
        {
            // Arrange
            _registry.Initialize(_gridQuery);
            _gridQuery.Initialize(GridSize.Size10, Vector3.zero);

            var state = new ComponentState<Transform>
            {
                component = _testComponent,
                anchorBounds = new Bounds(Vector3.zero, Vector3.one)
            };

            _registry.TryRegister(1, state);
            int expectedCellCount = _registry.CellCount;
            Assert.IsTrue(expectedCellCount > 0, "Pre-condition: Registry must have active cells.");

            // Act
            var iterator = _registry.GetCellIterator();
            int iteratedCount = 0;
            var visitedKeys = new List<Vector3Int>();

            while (iterator.MoveNext())
            {
                iteratedCount++;
                visitedKeys.Add(iterator.Current);
            }

            // Assert
            Assert.AreEqual(expectedCellCount, iteratedCount, "Iterator count should match the active cell count.");
            Assert.AreEqual(expectedCellCount, visitedKeys.Count);
        }

        #endregion

        #region IsCellActive Tests

        [Test]
        public void IsCellActive_NonExistentCell_ReturnsFalse()
        {
            // Arrange
            _registry.Initialize(_gridQuery);
            _gridQuery.Initialize(GridSize.Size10, Vector3.zero);

            var nonExistentKey = new Vector3Int(99, 99, 99);

            // Act
            bool result = _registry.IsCellActive(nonExistentKey);

            // Assert
            Assert.IsFalse(result, "IsCellActive should return false for a cell that has no components.");
        }

        [Test]
        public void IsCellActive_ActiveCell_ReturnsTrue()
        {
            // Arrange
            _registry.Initialize(_gridQuery);
            _gridQuery.Initialize(GridSize.Size10, Vector3.zero);

            var state = new ComponentState<Transform>
            {
                component = _testComponent,
                anchorBounds = new Bounds(Vector3.zero, Vector3.one)
            };

            _registry.TryRegister(1, state);

            // Find an active cell key from the registry
            var cellIterator = _registry.GetCellIterator();
            Assert.IsTrue(cellIterator.MoveNext(), "Pre-condition: Registry must have at least one active cell.");
            var activeKey = cellIterator.Current;

            // Act
            bool result = _registry.IsCellActive(activeKey);

            // Assert
            Assert.IsTrue(result, "IsCellActive should return true for a cell containing components.");
        }

        [Test]
        public void IsCellActive_AfterUnregisteringAllComponentsInCell_ReturnsFalse()
        {
            // Arrange
            _registry.Initialize(_gridQuery);
            _gridQuery.Initialize(GridSize.Size10, Vector3.zero);

            var state = new ComponentState<Transform>
            {
                component = _testComponent,
                anchorBounds = new Bounds(Vector3.zero, Vector3.one)
            };

            _registry.TryRegister(1, state);

            var cellIterator = _registry.GetCellIterator();
            Assert.IsTrue(cellIterator.MoveNext(), "Pre-condition: Registry must have at least one active cell.");
            var activeKey = cellIterator.Current;

            // Act
            _registry.Unregister(1);
            bool result = _registry.IsCellActive(activeKey);

            // Assert
            Assert.IsFalse(result, "IsCellActive should return false after all components in the cell are unregistered.");
        }

        #endregion

        #region TryForEachInCell Tests

        [Test]
        public void TryForEachInCell_NonExistentCell_ReturnsFalseAndDoesNotExecute()
        {
            // Arrange
            _registry.Initialize(_gridQuery);
            _gridQuery.Initialize(GridSize.Size10, Vector3.zero);

            var nonExistentKey = new Vector3Int(99, 99, 99);
            var handler = new ComponentExecutionHandler();

            // Act
            bool result = _registry.TryForEachInCell(nonExistentKey, ref handler);

            // Assert
            Assert.IsFalse(result, "TryForEachInCell should return false for a non-existent cell.");
            Assert.AreEqual(0, handler.ExecutionCount, "Action should not be executed for a non-existent cell.");
        }

        [Test]
        public void TryForEachInCell_ActiveCell_ReturnsTrueAndExecutesActionOnComponents()
        {
            // Arrange
            _registry.Initialize(_gridQuery);
            _gridQuery.Initialize(GridSize.Size10, Vector3.zero);

            var state = new ComponentState<Transform>
            {
                component = _testComponent,
                anchorBounds = new Bounds(Vector3.zero, Vector3.one)
            };

            _registry.TryRegister(1, state);

            var cellIterator = _registry.GetCellIterator();
            Assert.IsTrue(cellIterator.MoveNext(), "Pre-condition: Registry must have at least one active cell.");
            var activeKey = cellIterator.Current;

            var handler = new ComponentExecutionHandler();

            // Act
            bool result = _registry.TryForEachInCell(activeKey, ref handler);

            // Assert
            Assert.IsTrue(result, "TryForEachInCell should return true for an active cell.");
            Assert.IsTrue(handler.ExecutionCount > 0, "Action should be executed at least once.");
            Assert.IsNotNull(handler.ExecutedComponents);
            Assert.Contains(_testComponent, handler.ExecutedComponents, "The registered component should have been processed by the action.");
        }

        #endregion

        #region ForEachDirtyCell Tests

        [Test]
        public void ForEachDirtyCell_NoDirtyCells_DoesNotExecuteAction()
        {
            // Arrange
            _registry.Initialize(_gridQuery);
            _gridQuery.Initialize(GridSize.Size10, Vector3.zero);

            var handler = new CellExecutionHandler();

            // Act
            _registry.ForEachDirtyCell(ref handler);

            // Assert
            Assert.AreEqual(0, handler.ExecutionCount, "Action should not execute when there are no dirty cells.");
        }

        [Test]
        public void ForEachDirtyCell_WithDirtyCells_ExecutesActionOnAllDirtyCells()
        {
            // Arrange
            _registry.Initialize(_gridQuery);
            _gridQuery.Initialize(GridSize.Size10, Vector3.zero);

            var state = new ComponentState<Transform>
            {
                component = _testComponent,
                anchorBounds = new Bounds(Vector3.zero, Vector3.one)
            };

            // Registering components marks cells as dirty
            _registry.TryRegister(1, state);
            int dirtyCount = _registry.DirtyCellCount;
            Assert.IsTrue(dirtyCount > 0, "Pre-condition: Registering components must produce dirty cells.");

            var handler = new CellExecutionHandler();

            // Act
            _registry.ForEachDirtyCell(ref handler);

            // Assert
            Assert.AreEqual(dirtyCount, handler.ExecutionCount, "Action should execute once for each dirty cell.");
            Assert.IsNotNull(handler.VisitedKeys);
            Assert.AreEqual(dirtyCount, handler.VisitedKeys.Count);
        }

        [Test]
        public void ForEachDirtyCell_AfterClearDirtyCells_DoesNotExecuteAction()
        {
            // Arrange
            _registry.Initialize(_gridQuery);
            _gridQuery.Initialize(GridSize.Size10, Vector3.zero);

            var state = new ComponentState<Transform>
            {
                component = _testComponent,
                anchorBounds = new Bounds(Vector3.zero, Vector3.one)
            };

            _registry.TryRegister(1, state);

            // Clear dirty tracking
            _registry.ClearDirtyCells();
            Assert.AreEqual(0, _registry.DirtyCellCount, "Pre-condition: Dirty cell count should be 0 after clearing.");

            var handler = new CellExecutionHandler();

            // Act
            _registry.ForEachDirtyCell(ref handler);

            // Assert
            Assert.AreEqual(0, handler.ExecutionCount, "Action should not execute after dirty cells have been explicitly cleared.");
        }

        #endregion

        #region TryGetEntryIterator Tests

        [Test]
        public void TryGetEntryIterator_NonExistentCell_ReturnsFalseAndNullIterator()
        {
            // Arrange
            _registry.Initialize(_gridQuery);
            _gridQuery.Initialize(GridSize.Size10, Vector3.zero);

            var nonExistentKey = new Vector3Int(99, 99, 99);

            // Act
            bool result = _registry.TryGetEntryIterator(nonExistentKey, out var iterator);

            // Assert
            Assert.IsFalse(result, "TryGetEntryIterator should return false for a non-existent cell.");
            Assert.IsNull(iterator, "Iterator should be null when the cell is not found.");
        }

        [Test]
        public void TryGetEntryIterator_ActiveCell_ReturnsTrueAndIteratesComponents()
        {
            // Arrange
            _registry.Initialize(_gridQuery);
            _gridQuery.Initialize(GridSize.Size10, Vector3.zero);

            var state = new ComponentState<Transform>
            {
                component = _testComponent,
                anchorBounds = new Bounds(Vector3.zero, Vector3.one)
            };

            _registry.TryRegister(1, state);

            var cellIterator = _registry.GetCellIterator();
            Assert.IsTrue(cellIterator.MoveNext(), "Pre-condition: Registry must have at least one active cell.");
            var activeKey = cellIterator.Current;

            // Act
            bool result = _registry.TryGetEntryIterator(activeKey, out var iterator);

            // Assert
            Assert.IsTrue(result, "TryGetEntryIterator should return true for an active cell.");
            Assert.IsNotNull(iterator, "Iterator should not be null when an active cell is found.");

            int count = 0;
            var collectedComponents = new List<Transform>();
            while (iterator.MoveNext())
            {
                count++;
                collectedComponents.Add(iterator.Current);
            }

            Assert.IsTrue(count > 0, "Iterator should yield at least one component.");
            Assert.Contains(_testComponent, collectedComponents, "The collected components should contain the test component.");
        }

        #endregion

        #region AllIds Tests

        [Test]
        public void AllIds_EmptyRegistry_ReturnsEmptyIterator()
        {
            // Arrange
            _registry.Initialize(_gridQuery);
            _gridQuery.Initialize(GridSize.Size10, Vector3.zero);

            // Act
            var iterator = _registry.AllIds;

            // Assert
            Assert.IsNotNull(iterator);
            Assert.IsFalse(iterator.MoveNext(), "AllIds iterator should be empty when the registry has no elements.");
        }

        [Test]
        public void AllIds_PopulatedRegistry_IteratesAllIds()
        {
            // Arrange
            _registry.Initialize(_gridQuery);
            _gridQuery.Initialize(GridSize.Size10, Vector3.zero);

            var state = new ComponentState<Transform>
            {
                component = _testComponent,
                anchorBounds = new Bounds(Vector3.zero, Vector3.one)
            };

            int testId = 42;
            _registry.TryRegister(testId, state);

            // Act
            var iterator = _registry.AllIds;
            int count = 0;
            var ids = new List<int>();

            while (iterator.MoveNext())
            {
                count++;
                ids.Add(iterator.Current);
            }

            // Assert
            Assert.AreEqual(1, count);
            Assert.Contains(testId, ids);
        }

        #endregion

        #region AllKeys Tests

        [Test]
        public void AllKeys_EmptyRegistry_ReturnsEmptyIterator()
        {
            // Arrange
            _registry.Initialize(_gridQuery);
            _gridQuery.Initialize(GridSize.Size10, Vector3.zero);

            // Act
            var iterator = _registry.AllKeys;

            // Assert
            Assert.IsNotNull(iterator);
            Assert.IsFalse(iterator.MoveNext(), "AllKeys iterator should be empty when the registry has no active keys.");
        }

        [Test]
        public void AllKeys_PopulatedRegistry_IteratesAllKeys()
        {
            // Arrange
            _registry.Initialize(_gridQuery);
            _gridQuery.Initialize(GridSize.Size10, Vector3.zero);

            var state = new ComponentState<Transform>
            {
                component = _testComponent,
                anchorBounds = new Bounds(Vector3.zero, Vector3.one)
            };

            _registry.TryRegister(1, state);
            int expectedKeyCount = _registry.CellCount;
            Assert.IsTrue(expectedKeyCount > 0, "Pre-condition: Registry must have active cells.");

            // Act
            var iterator = _registry.AllKeys;
            int count = 0;

            while (iterator.MoveNext())
            {
                count++;
            }

            // Assert
            Assert.AreEqual(expectedKeyCount, count);
        }

        #endregion

        #region AllStates Tests

        [Test]
        public void AllStates_EmptyRegistry_ReturnsEmptyIterator()
        {
            // Arrange
            _registry.Initialize(_gridQuery);
            _gridQuery.Initialize(GridSize.Size10, Vector3.zero);

            // Act
            var iterator = _registry.AllStates;

            // Assert
            Assert.IsNotNull(iterator);
            Assert.IsFalse(iterator.MoveNext(), "AllStates iterator should be empty when the registry has no states.");
        }

        [Test]
        public void AllStates_PopulatedRegistry_IteratesAllStates()
        {
            // Arrange
            _registry.Initialize(_gridQuery);
            _gridQuery.Initialize(GridSize.Size10, Vector3.zero);

            var state = new ComponentState<Transform>
            {
                component = _testComponent,
                anchorBounds = new Bounds(Vector3.zero, Vector3.one)
            };

            _registry.TryRegister(1, state);

            // Act
            var iterator = _registry.AllStates;
            int count = 0;
            var states = new List<ComponentState<Transform>>();

            while (iterator.MoveNext())
            {
                count++;
                states.Add(iterator.Current);
            }

            // Assert
            Assert.AreEqual(1, count);
            Assert.AreEqual(_testComponent, states[0].component);
        }

        #endregion

        #region CellIds Tests

        [Test]
        public void CellIds_NonExistentCell_ReturnsEmptyIterator()
        {
            // Arrange
            _registry.Initialize(_gridQuery);
            _gridQuery.Initialize(GridSize.Size10, Vector3.zero);

            var nonExistentKey = new Vector3Int(99, 99, 99);

            // Act
            var iterator = _registry.CellIds(nonExistentKey);

            // Assert
            Assert.IsNotNull(iterator);
            Assert.IsFalse(iterator.MoveNext(), "CellIds iterator should be empty for a non-existent cell.");
        }

        [Test]
        public void CellIds_ActiveCell_IteratesCellIds()
        {
            // Arrange
            _registry.Initialize(_gridQuery);
            _gridQuery.Initialize(GridSize.Size10, Vector3.zero);

            var state = new ComponentState<Transform>
            {
                component = _testComponent,
                anchorBounds = new Bounds(Vector3.zero, Vector3.one)
            };

            int testId = 123;
            _registry.TryRegister(testId, state);

            var cellIterator = _registry.GetCellIterator();
            Assert.IsTrue(cellIterator.MoveNext(), "Pre-condition: Registry must have at least one active cell.");
            var activeKey = cellIterator.Current;

            // Act
            var iterator = _registry.CellIds(activeKey);
            int count = 0;
            var ids = new List<int>();

            while (iterator.MoveNext())
            {
                count++;
                ids.Add(iterator.Current);
            }

            // Assert
            Assert.IsNotNull(iterator);
            Assert.IsTrue(count > 0, "CellIds iterator should yield at least one ID.");
            Assert.Contains(testId, ids, "The collected IDs should contain the registered component ID.");
        }

        #endregion

        #region ForEachId Tests

        private struct IntExecutionHandler : IExecutionHandler<int>
        {
            public int ExecutionCount;
            public List<int> ExecutedIds;

            public void Execute(int id)
            {
                ExecutionCount++;
                ExecutedIds ??= new List<int>();
                ExecutedIds.Add(id);
            }
        }

        [Test]
        public void ForEachId_EmptyRegistry_DoesNotExecuteAction()
        {
            // Arrange
            _registry.Initialize(_gridQuery);
            _gridQuery.Initialize(GridSize.Size10, Vector3.zero);

            var handler = new IntExecutionHandler();

            // Act
            _registry.ForEachId(ref handler);

            // Assert
            Assert.AreEqual(0, handler.ExecutionCount, "Action should not execute when the registry has no IDs.");
        }

        [Test]
        public void ForEachId_PopulatedRegistry_ExecutesActionOnAllIds()
        {
            // Arrange
            _registry.Initialize(_gridQuery);
            _gridQuery.Initialize(GridSize.Size10, Vector3.zero);

            var state = new ComponentState<Transform>
            {
                component = _testComponent,
                anchorBounds = new Bounds(Vector3.zero, Vector3.one)
            };

            int testId = 77;
            _registry.TryRegister(testId, state);

            var handler = new IntExecutionHandler();

            // Act
            _registry.ForEachId(ref handler);

            // Assert
            Assert.AreEqual(1, handler.ExecutionCount, "Action should execute once for each registered ID.");
            Assert.IsNotNull(handler.ExecutedIds);
            Assert.Contains(testId, handler.ExecutedIds);
        }

        #endregion

        #region ForEachKey Tests

        private struct CellKeyExecutionHandler : IExecutionHandler<Vector3Int>
        {
            public int ExecutionCount;
            public List<Vector3Int> VisitedKeys;

            public void Execute(Vector3Int key)
            {
                ExecutionCount++;
                VisitedKeys ??= new List<Vector3Int>();
                VisitedKeys.Add(key);
            }
        }

        [Test]
        public void ForEachKey_EmptyRegistry_DoesNotExecuteAction()
        {
            // Arrange
            _registry.Initialize(_gridQuery);
            _gridQuery.Initialize(GridSize.Size10, Vector3.zero);

            var handler = new CellKeyExecutionHandler();

            // Act
            _registry.ForEachKey(ref handler);

            // Assert
            Assert.AreEqual(0, handler.ExecutionCount, "Action should not execute when the registry has no active keys.");
        }

        [Test]
        public void ForEachKey_PopulatedRegistry_ExecutesActionOnAllKeys()
        {
            // Arrange
            _registry.Initialize(_gridQuery);
            _gridQuery.Initialize(GridSize.Size10, Vector3.zero);

            var state = new ComponentState<Transform>
            {
                component = _testComponent,
                anchorBounds = new Bounds(Vector3.zero, Vector3.one)
            };

            _registry.TryRegister(1, state);
            int expectedKeyCount = _registry.CellCount;
            Assert.IsTrue(expectedKeyCount > 0, "Pre-condition: Registry must have active cells.");

            var handler = new CellKeyExecutionHandler();

            // Act
            _registry.ForEachKey(ref handler);

            // Assert
            Assert.AreEqual(expectedKeyCount, handler.ExecutionCount, "Action should execute once for each active key/bucket.");
            Assert.IsNotNull(handler.VisitedKeys);
            Assert.AreEqual(expectedKeyCount, handler.VisitedKeys.Count);
        }

        #endregion

        #region TryForEachCellId Tests

        [Test]
        public void TryForEachCellId_NonExistentCell_ReturnsFalseAndDoesNotExecute()
        {
            // Arrange
            _registry.Initialize(_gridQuery);
            _gridQuery.Initialize(GridSize.Size10, Vector3.zero);

            var nonExistentKey = new Vector3Int(99, 99, 99);
            var handler = new IntExecutionHandler();

            // Act
            bool result = _registry.TryForEachCellId(nonExistentKey, ref handler);

            // Assert
            Assert.IsFalse(result, "TryForEachCellId should return false for a non-existent cell.");
            Assert.AreEqual(0, handler.ExecutionCount, "Action should not execute for a non-existent cell.");
        }

        [Test]
        public void TryForEachCellId_ActiveCell_ReturnsTrueAndExecutesActionOnCellIds()
        {
            // Arrange
            _registry.Initialize(_gridQuery);
            _gridQuery.Initialize(GridSize.Size10, Vector3.zero);

            var state = new ComponentState<Transform>
            {
                component = _testComponent,
                anchorBounds = new Bounds(Vector3.zero, Vector3.one)
            };

            int testId = 55;
            _registry.TryRegister(testId, state);

            var cellIterator = _registry.GetCellIterator();
            Assert.IsTrue(cellIterator.MoveNext(), "Pre-condition: Registry must have at least one active cell.");
            var activeKey = cellIterator.Current;

            var handler = new IntExecutionHandler();

            // Act
            bool result = _registry.TryForEachCellId(activeKey, ref handler);

            // Assert
            Assert.IsTrue(result, "TryForEachCellId should return true for an active cell.");
            Assert.IsTrue(handler.ExecutionCount > 0, "Action should execute at least once for the cell IDs.");
            Assert.IsNotNull(handler.ExecutedIds);
            Assert.Contains(testId, handler.ExecutedIds);
        }

        #endregion

        #region ForEachState Tests

        private struct StateExecutionHandler : IExecutionHandler<ComponentState<Transform>>
        {
            public int ExecutionCount;
            public List<ComponentState<Transform>> ExecutedStates;

            public void Execute(ComponentState<Transform> state)
            {
                ExecutionCount++;
                ExecutedStates ??= new List<ComponentState<Transform>>();
                ExecutedStates.Add(state);
            }
        }

        [Test]
        public void ForEachState_EmptyRegistry_DoesNotExecuteAction()
        {
            // Arrange
            _registry.Initialize(_gridQuery);
            _gridQuery.Initialize(GridSize.Size10, Vector3.zero);

            var handler = new StateExecutionHandler();

            // Act
            _registry.ForEachState(ref handler);

            // Assert
            Assert.AreEqual(0, handler.ExecutionCount, "Action should not execute when the registry has no states.");
        }

        [Test]
        public void ForEachState_PopulatedRegistry_ExecutesActionOnAllStates()
        {
            // Arrange
            _registry.Initialize(_gridQuery);
            _gridQuery.Initialize(GridSize.Size10, Vector3.zero);

            var state = new ComponentState<Transform>
            {
                component = _testComponent,
                anchorBounds = new Bounds(Vector3.zero, Vector3.one)
            };

            _registry.TryRegister(1, state);

            var handler = new StateExecutionHandler();

            // Act
            _registry.ForEachState(ref handler);

            // Assert
            Assert.AreEqual(1, handler.ExecutionCount, "Action should execute once for each registered state.");
            Assert.IsNotNull(handler.ExecutedStates);
            Assert.AreEqual(_testComponent, handler.ExecutedStates[0].component);
        }

        #endregion
    }
}